using Proximity.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Proximity.Services;

public class BroadcastService : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly DiscoveryService _discoveryService;
    private readonly ObservableCollection<BroadcastMessage> _broadcastHistory = new();
    private const int BROADCAST_PORT = 9004;
    private bool _isListening;
    private int _broadcastsSent;

    public ObservableCollection<BroadcastMessage> BroadcastHistory => _broadcastHistory;

    public event EventHandler<BroadcastMessage> BroadcastReceived;
    public event EventHandler<BroadcastMessage> BroadcastSent;

    public int BroadcastsSent => _broadcastsSent;

    public BroadcastService(DiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;

        try
        {
            _udpClient = new UdpClient(BROADCAST_PORT);
            _udpClient.EnableBroadcast = true;
            StartListening();
            System.Diagnostics.Debug.WriteLine($"BroadcastService: Started on port {BROADCAST_PORT}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BroadcastService init error: {ex.Message}");
        }
    }

    private async void StartListening()
    {
        _isListening = true;

        try
        {
            while (_isListening)
            {
                var result = await _udpClient.ReceiveAsync();
                var json = Encoding.UTF8.GetString(result.Buffer);

                try
                {
                    var broadcast = JsonSerializer.Deserialize<BroadcastMessage>(json);

                    if (broadcast != null && broadcast.SenderId != _discoveryService.LocalPeerId)
                    {
                        broadcast.IsSentByMe = false;

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _broadcastHistory.Insert(0, broadcast);

                            // Keep history manageable
                            if (_broadcastHistory.Count > 100)
                            {
                                _broadcastHistory.RemoveAt(_broadcastHistory.Count - 1);
                            }

                            BroadcastReceived?.Invoke(this, broadcast);
                        });

                        System.Diagnostics.Debug.WriteLine($"BroadcastService: Received from {broadcast.SenderName}");
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"BroadcastService JSON parse error: {ex.Message}");
                }
            }
        }
        catch (SocketException ex)
        {
            System.Diagnostics.Debug.WriteLine($"BroadcastService socket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (_isListening) // Only log if we're still supposed to be listening
            {
                System.Diagnostics.Debug.WriteLine($"BroadcastService listening error: {ex.Message}");
            }
        }
    }

    public async Task<bool> SendBroadcastAsync(string message, BroadcastPriority priority = BroadcastPriority.Normal)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        try
        {
            var broadcast = new BroadcastMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                SenderId = _discoveryService.LocalPeerId,
                SenderName = _discoveryService.LocalName,
                SenderAvatar = GetUserAvatar(),
                Message = message,
                Timestamp = DateTime.Now,
                Priority = priority,
                IsSentByMe = true,
                DeliveredCount = 0
            };

            var json = JsonSerializer.Serialize(broadcast);
            var data = Encoding.UTF8.GetBytes(json);

            int deliveredCount = 0;

            // Send to all discovered online peers
            foreach (var peer in _discoveryService.DiscoveredPeers)
            {
                if (peer.IsOnline)
                {
                    try
                    {
                        var endpoint = new IPEndPoint(IPAddress.Parse(peer.IpAddress), BROADCAST_PORT);
                        await _udpClient.SendAsync(data, data.Length, endpoint);
                        deliveredCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"BroadcastService send to {peer.Name} failed: {ex.Message}");
                    }
                }
            }

            // Also broadcast to subnet (for discovery of new peers)
            try
            {
                var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, BROADCAST_PORT);
                await _udpClient.SendAsync(data, data.Length, broadcastEndpoint);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BroadcastService subnet broadcast failed: {ex.Message}");
            }

            broadcast.DeliveredCount = deliveredCount;
            _broadcastsSent++;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _broadcastHistory.Insert(0, broadcast);

                // Keep history manageable
                if (_broadcastHistory.Count > 100)
                {
                    _broadcastHistory.RemoveAt(_broadcastHistory.Count - 1);
                }

                BroadcastSent?.Invoke(this, broadcast);
            });

            System.Diagnostics.Debug.WriteLine($"BroadcastService: Sent to {deliveredCount} peers");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BroadcastService send error: {ex.Message}");
            return false;
        }
    }

    public void ClearHistory()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _broadcastHistory.Clear();
        });
    }

    private string GetUserAvatar()
    {
        return Preferences.Get("user_avatar", "📢");
    }

    public void Dispose()
    {
        _isListening = false;

        try
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
        }
        catch { }

        System.Diagnostics.Debug.WriteLine("BroadcastService: Disposed");
    }
}