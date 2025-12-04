using Proximity.Models;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Proximity.Services;

public class ChatService : IDisposable
{
    private readonly ConcurrentDictionary<string, TcpClient> _connectedPeers = new();
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _chatHistory = new();
    private readonly ConcurrentDictionary<string, NetworkStream> _peerStreams = new();
    private TcpListener _listener;
    private bool _isListening;
    private int _totalMessagesSent;
    private const int CHAT_PORT = 9002;

    public string LocalPeerId { get; private set; }

    public event EventHandler<ChatMessage> MessageReceived;
    public event EventHandler<ChatMessage> MessageSent;
    public event EventHandler<string> PeerConnected;
    public event EventHandler<string> PeerDisconnected;

    public ChatService()
    {
        LocalPeerId = Preferences.Get("peer_id", Guid.NewGuid().ToString());
        Preferences.Set("peer_id", LocalPeerId);
        StartListening();
    }

    private async void StartListening()
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, CHAT_PORT);
            _listener.Start();
            _isListening = true;

            System.Diagnostics.Debug.WriteLine($"ChatService: Listening on port {CHAT_PORT}");

            while (_isListening)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClient(client));
            }
        }
        catch (SocketException ex)
        {
            System.Diagnostics.Debug.WriteLine($"ChatService socket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ChatService listening error: {ex.Message}");
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        NetworkStream stream = null;
        string peerId = null;

        try
        {
            stream = client.GetStream();
            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var message = JsonSerializer.Deserialize<ChatMessage>(json);

                if (message != null)
                {
                    peerId = message.SenderId;

                    // Store peer connection if first message
                    if (!_connectedPeers.ContainsKey(peerId))
                    {
                        _connectedPeers[peerId] = client;
                        _peerStreams[peerId] = stream;
                        PeerConnected?.Invoke(this, peerId);
                        System.Diagnostics.Debug.WriteLine($"ChatService: Peer {message.SenderName} connected");
                    }

                    message.IsSentByMe = false;
                    message.IsDelivered = true;

                    // Store in history
                    AddToHistory(message.SenderId, message);

                    // Notify listeners
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MessageReceived?.Invoke(this, message);
                    });

                    System.Diagnostics.Debug.WriteLine($"ChatService: Received message from {message.SenderName}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ChatService handle client error: {ex.Message}");
        }
        finally
        {
            if (peerId != null)
            {
                _connectedPeers.TryRemove(peerId, out _);
                _peerStreams.TryRemove(peerId, out _);
                PeerDisconnected?.Invoke(this, peerId);
            }

            stream?.Dispose();
            client?.Close();
        }
    }

    public async Task ConnectToPeerAsync(PeerInfo peer)
    {
        if (_connectedPeers.ContainsKey(peer.PeerId))
        {
            System.Diagnostics.Debug.WriteLine($"ChatService: Already connected to {peer.Name}");
            return;
        }

        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(peer.IpAddress, CHAT_PORT);

            _connectedPeers[peer.PeerId] = client;
            _peerStreams[peer.PeerId] = client.GetStream();

            peer.IsConnected = true;
            PeerConnected?.Invoke(this, peer.PeerId);

            // Start listening for messages from this peer
            _ = Task.Run(() => HandleClient(client));

            System.Diagnostics.Debug.WriteLine($"ChatService: Connected to {peer.Name} at {peer.IpAddress}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ChatService connect error: {ex.Message}");
            throw new InvalidOperationException($"Failed to connect to {peer.Name}: {ex.Message}");
        }
    }

    public void DisconnectFromPeer(PeerInfo peer)
    {
        if (_connectedPeers.TryRemove(peer.PeerId, out var client))
        {
            _peerStreams.TryRemove(peer.PeerId, out _);

            try
            {
                client?.Close();
                client?.Dispose();
            }
            catch { }

            peer.IsConnected = false;
            PeerDisconnected?.Invoke(this, peer.PeerId);

            System.Diagnostics.Debug.WriteLine($"ChatService: Disconnected from {peer.Name}");
        }
    }

    public async Task SendMessageAsync(PeerInfo peer, string content)
    {
        if (!_peerStreams.TryGetValue(peer.PeerId, out var stream))
        {
            throw new InvalidOperationException("Not connected to peer. Connect first.");
        }

        var message = new ChatMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            SenderId = LocalPeerId,
            SenderName = Preferences.Get("username", "Me"),
            ReceiverId = peer.PeerId,
            ReceiverName = peer.Name,
            Content = content,
            Timestamp = DateTime.Now,
            IsSentByMe = true,
            IsDelivered = false
        };

        try
        {
            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);

            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();

            // Store in history
            AddToHistory(peer.PeerId, message);

            _totalMessagesSent++;

            // Mark as delivered after successful send
            message.IsDelivered = true;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MessageSent?.Invoke(this, message);
            });

            System.Diagnostics.Debug.WriteLine($"ChatService: Sent message to {peer.Name}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ChatService send error: {ex.Message}");
            throw new InvalidOperationException($"Failed to send message: {ex.Message}");
        }
    }

    private void AddToHistory(string peerId, ChatMessage message)
    {
        if (!_chatHistory.ContainsKey(peerId))
        {
            _chatHistory[peerId] = new List<ChatMessage>();
        }
        _chatHistory[peerId].Add(message);
    }

    public List<ChatMessage> GetChatHistory(string peerId)
    {
        return _chatHistory.TryGetValue(peerId, out var history)
            ? new List<ChatMessage>(history)
            : new List<ChatMessage>();
    }

    public void ClearChatHistory(string peerId)
    {
        _chatHistory.TryRemove(peerId, out _);
    }

    public void ClearAllHistory()
    {
        _chatHistory.Clear();
    }

    public int GetTotalMessagesSent()
    {
        return _totalMessagesSent;
    }

    public int GetTotalMessagesReceived()
    {
        return _chatHistory.Values.Sum(history => history.Count(m => !m.IsSentByMe));
    }

    public bool IsConnectedToPeer(string peerId)
    {
        return _connectedPeers.ContainsKey(peerId);
    }

    public List<string> GetConnectedPeerIds()
    {
        return _connectedPeers.Keys.ToList();
    }

    public void Dispose()
    {
        _isListening = false;

        try
        {
            _listener?.Stop();
        }
        catch { }

        foreach (var client in _connectedPeers.Values)
        {
            try
            {
                client?.Close();
                client?.Dispose();
            }
            catch { }
        }

        _connectedPeers.Clear();
        _peerStreams.Clear();

        System.Diagnostics.Debug.WriteLine("ChatService: Disposed");
    }
}