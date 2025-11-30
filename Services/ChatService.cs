using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proximity.Services
{
    public class ChatService : IDisposable
    {
        private readonly int _port;
        private TcpListener _listener;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, TcpClient> _clients = new ConcurrentDictionary<string, TcpClient>();


        public event Action<string, string> MessageReceived; // fromId, message


        public ChatService(int port = 9002)
        {
            _port = port;
        }


        public void StartHost()
        {
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
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClient(client, ct);
                }
                catch { }
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken ct)
        {
            var ep = client.Client.RemoteEndPoint.ToString();
            _clients.TryAdd(ep, client);


            using var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);


            while (!ct.IsCancellationRequested && client.Connected)
            {
                try
                { var line = await reader.ReadLineAsync();
                    if (line == null) break;
                    // simple protocol: CHAT|senderId|message
                    if (line.StartsWith("CHAT|"))
                    {
                        var parts = line.Split('|', 3);
                        if (parts.Length >= 3)
                        {
                            var from = parts[1];
                            var msg = parts[2];
                            MessageReceived?.Invoke(from, msg);
                        }
                    }
                }
                catch { break; }
            }
            _clients.TryRemove(ep, out _);
        }
        public async Task ConnectToHost(IPAddress host, int port = 9002)
        {
            var client = new TcpClient();
            await client.ConnectAsync(host, port);
            var ep = client.Client.RemoteEndPoint.ToString();
            _clients.TryAdd(ep, client);
            _ = HandleClient(client, _cts.Token);
        }
        public async Task SendMessageToAll(string senderId, string message)
        {
            var line = $"CHAT|{senderId}|{message}\n";
            var bytes = Encoding.UTF8.GetBytes(line);


            foreach (var kv in _clients)
            {
                try
                { var stream = kv.Value.GetStream();
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                    await stream.FlushAsync();
                }
                catch { }
            }
        }
        public void Dispose()
        {
            _cts.Cancel();
            try { _listener?.Stop(); } catch { }
            foreach (var kv in _clients) kv.Value?.Close();
        }
    }
}

