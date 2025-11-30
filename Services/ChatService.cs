using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Models;

namespace Proximity.Services
{    
    public class ChatService : IDisposable
    {
        private readonly int _port;
        private TcpListener? _listener;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, TcpClient> _connections = new ConcurrentDictionary<string, TcpClient>();
        private readonly List<ChatMessage> _messageHistory = new List<ChatMessage>();
        private readonly string _localId;
        private readonly string _localName;

        public event Action<ChatMessage>? MessageReceived;
        public event Action<string>? ClientConnected;
        public event Action<string>? ClientDisconnected;

        public ChatService(string localId, string localName, int port = 9002)
        {
            _port = port;
            _localId = localId;
            _localName = localName;
        }

        public void StartListening()
        {
            if (_listener != null) return;

            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            _ = AcceptLoop(_cts.Token);
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync();
                    _ = HandleClient(client, ct);
                }
                catch when (ct.IsCancellationRequested) { }
                catch { }
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken ct)
        {
            string? clientId = null;
            try
            {
                using var stream = client.GetStream();
                var reader = new StreamReader(stream, Encoding.UTF8);
                var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                // First message should be HELLO with client ID
                var hello = await reader.ReadLineAsync();
                if (hello != null && hello.StartsWith("HELLO|"))
                {
                    var parts = hello.Split('|');
                    if (parts.Length >= 2)
                    {
                        clientId = parts[1];
                        _connections.TryAdd(clientId, client);
                        ClientConnected?.Invoke(clientId);

                        // Send acknowledgment
                        await writer.WriteLineAsync($"WELCOME|{_localId}|{_localName}");
                    }
                }

                while (!ct.IsCancellationRequested && client.Connected)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    ProcessMessage(line);
                }
            }
            catch { }
            finally
            {
                if (clientId != null)
                {
                    _connections.TryRemove(clientId, out _);
                    ClientDisconnected?.Invoke(clientId);
                }
                client?.Close();
            }
        }

        public async Task ConnectToPeer(IPAddress address, string peerId, int port = 9002)
        {
            try
            {
                var client = new TcpClient();
                await client.ConnectAsync(address, port);

                var stream = client.GetStream();
                var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                var reader = new StreamReader(stream, Encoding.UTF8);

                // Send HELLO
                await writer.WriteLineAsync($"HELLO|{_localId}|{_localName}");

                // Wait for WELCOME
                var welcome = await reader.ReadLineAsync();
                if (welcome != null && welcome.StartsWith("WELCOME|"))
                {
                    _connections.TryAdd(peerId, client);
                    ClientConnected?.Invoke(peerId);
                    _ = HandleClient(client, _cts.Token);
                }
                else
                {
                    client.Close();
                }
            }
            catch { }
        }

        private void ProcessMessage(string line)
        {
            try
            {
                // Protocol: MSG|senderId|senderName|content|isPrivate|recipientId
                if (line.StartsWith("MSG|"))
                {
                    var parts = line.Split('|');
                    if (parts.Length >= 5)
                    {
                        var message = new ChatMessage
                        {
                            SenderId = parts[1],
                            SenderName = parts[2],
                            Content = parts[3],
                            IsPrivate = parts[4] == "1",
                            RecipientId = parts.Length > 5 ? parts[5] : null,
                            Timestamp = DateTime.Now
                        };

                        // Only process if it's for us (broadcast or private to us)
                        if (!message.IsPrivate || message.RecipientId == _localId)
                        {
                            _messageHistory.Add(message);
                            MessageReceived?.Invoke(message);
                        }
                    }
                }
            }
            catch { }
        }

        public async Task SendMessage(string content, string? recipientId = null)
        {
            var isPrivate = recipientId != null;
            var line = $"MSG|{_localId}|{_localName}|{content}|{(isPrivate ? "1" : "0")}|{recipientId ?? ""}";
            var bytes = Encoding.UTF8.GetBytes(line + "\n");

            var message = new ChatMessage
            {
                SenderId = _localId,
                SenderName = _localName,
                Content = content,
                IsPrivate = isPrivate,
                RecipientId = recipientId
            };

            _messageHistory.Add(message);

            // Send to specific recipient or broadcast to all
            var targets = isPrivate && recipientId != null
                ? _connections.Where(kv => kv.Key == recipientId)
                : _connections;

            foreach (var kv in targets)
            {
                try
                {
                    var stream = kv.Value.GetStream();
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                    await stream.FlushAsync();
                }
                catch
                {
                    _connections.TryRemove(kv.Key, out _);
                }
            }
        }

        public ChatMessage[] GetMessageHistory(string? withPeerId = null)
        {
            if (withPeerId == null)
                return _messageHistory.ToArray();

            return _messageHistory
                .Where(m => !m.IsPrivate ||
                           (m.SenderId == withPeerId && m.RecipientId == _localId) ||
                           (m.SenderId == _localId && m.RecipientId == withPeerId))
                .ToArray();
        }

        public string[] GetConnectedPeerIds()
        {
            return _connections.Keys.ToArray();
        }

        public void Dispose()
        {
            _cts?.Cancel();
            try { _listener?.Stop(); } catch { }
            foreach (var kv in _connections)
            {
                try { kv.Value?.Close(); } catch { }
            }
            _connections.Clear();
        }
    }
}