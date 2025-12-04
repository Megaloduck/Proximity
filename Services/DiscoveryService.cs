using Proximity.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Proximity.Services;

public class DiscoveryService : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly ObservableCollection<PeerInfo> _discoveredPeers = new();
    private bool _isRunning;
    private const int DISCOVERY_PORT = 9001;
    private System.Timers.Timer _advertisingTimer;
    private System.Timers.Timer _cleanupTimer;

    // Properties
    public string LocalPeerId { get; private set; }
    public string LocalName { get; set; }
    public string MyDeviceId => LocalPeerId; // Alias for compatibility
    public string MyDeviceName { get; set; } // Device description
    public bool IsRunning => _isRunning;
    public ObservableCollection<PeerInfo> DiscoveredPeers => _discoveredPeers;

    // Events
    public event EventHandler<PeerInfo> PeerDiscovered;
    public event EventHandler<PeerInfo> PeerUpdated;
    public event EventHandler<string> PeerLost;

    public DiscoveryService()
    {
        // Initialize local peer info
        LocalPeerId = Preferences.Get("peer_id", Guid.NewGuid().ToString());
        Preferences.Set("peer_id", LocalPeerId);

        LocalName = Preferences.Get("username", Environment.UserName ?? "Anonymous");
        MyDeviceName = Preferences.Get("device_name", GetDefaultDeviceName());

        try
        {
            _udpClient = new UdpClient(DISCOVERY_PORT);
            _udpClient.EnableBroadcast = true;
            StartListening();
            StartAdvertising();
            StartCleanupTimer();
            _isRunning = true;

            System.Diagnostics.Debug.WriteLine($"DiscoveryService: Started with ID {LocalPeerId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DiscoveryService initialization error: {ex.Message}");
        }
    }

    private string GetDefaultDeviceName()
    {
        try
        {
            return DeviceInfo.Current.Name ?? Environment.MachineName ?? "Unknown Device";
        }
        catch
        {
            return "Unknown Device";
        }
    }

    public void StartAdvertising() // Made public for DiscoverPageModel
    {
        if (_advertisingTimer != null)
            return;

        _advertisingTimer = new System.Timers.Timer(5000); // Advertise every 5 seconds
        _advertisingTimer.Elapsed += async (s, e) => await BroadcastPresenceAsync();
        _advertisingTimer.Start();

        // Send initial broadcast immediately
        _ = BroadcastPresenceAsync();
    }

    private void StartCleanupTimer()
    {
        _cleanupTimer = new System.Timers.Timer(10000); // Check every 10 seconds
        _cleanupTimer.Elapsed += (s, e) => CleanupOldPeers();
        _cleanupTimer.Start();
    }

    private async void StartListening()
    {
        try
        {
            while (_isRunning)
            {
                var result = await _udpClient.ReceiveAsync();
                var json = Encoding.UTF8.GetString(result.Buffer);

                try
                {
                    var discoveryMessage = JsonSerializer.Deserialize<DiscoveryMessage>(json);

                    if (discoveryMessage != null && discoveryMessage.PeerId != LocalPeerId)
                    {
                        var ipAddress = result.RemoteEndPoint.Address.ToString();
                        HandleDiscoveredPeer(discoveryMessage, ipAddress);
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"DiscoveryService JSON error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                System.Diagnostics.Debug.WriteLine($"DiscoveryService listening error: {ex.Message}");
            }
        }
    }

    private void HandleDiscoveredPeer(DiscoveryMessage message, string ipAddress)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existingPeer = _discoveredPeers.FirstOrDefault(p => p.PeerId == message.PeerId);

            if (existingPeer != null)
            {
                // Update existing peer
                existingPeer.Name = message.Name;
                existingPeer.IpAddress = ipAddress;
                existingPeer.IsOnline = true;
                existingPeer.LastSeen = DateTime.Now;
                existingPeer.Avatar = message.Avatar;
                existingPeer.StatusMessage = message.StatusMessage;

                PeerUpdated?.Invoke(this, existingPeer);
            }
            else
            {
                // Add new peer
                var newPeer = new PeerInfo
                {
                    PeerId = message.PeerId,
                    Name = message.Name,
                    IpAddress = ipAddress,
                    Port = DISCOVERY_PORT,
                    IsOnline = true,
                    LastSeen = DateTime.Now,
                    Avatar = message.Avatar,
                    StatusMessage = message.StatusMessage
                };

                _discoveredPeers.Add(newPeer);
                PeerDiscovered?.Invoke(this, newPeer);

                System.Diagnostics.Debug.WriteLine($"DiscoveryService: Discovered {newPeer.Name} at {ipAddress}");
            }
        });
    }

    private async Task BroadcastPresenceAsync()
    {
        try
        {
            var message = new DiscoveryMessage
            {
                PeerId = LocalPeerId,
                Name = LocalName,
                Avatar = Preferences.Get("user_avatar", "👤"),
                StatusMessage = Preferences.Get("user_status", ""),
                Timestamp = DateTime.Now
            };

            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json);

            var endpoint = new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT);
            await _udpClient.SendAsync(data, data.Length, endpoint);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DiscoveryService broadcast error: {ex.Message}");
        }
    }

    private void CleanupOldPeers()
    {
        var threshold = DateTime.Now.AddSeconds(-30); // Consider peers offline after 30 seconds

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var peersToRemove = _discoveredPeers.Where(p => p.LastSeen < threshold).ToList();

            foreach (var peer in peersToRemove)
            {
                peer.IsOnline = false;
                PeerLost?.Invoke(this, peer.PeerId);

                // Optionally remove completely after being offline for a while
                if (peer.LastSeen < DateTime.Now.AddMinutes(-5))
                {
                    _discoveredPeers.Remove(peer);
                }
            }
        });
    }

    public void UpdateLocalName(string name)
    {
        LocalName = name;
        Preferences.Set("username", name);
        _ = BroadcastPresenceAsync(); // Broadcast immediately with new name
    }

    public void UpdateDeviceName(string name)
    {
        MyDeviceName = name;
        Preferences.Set("device_name", name);
    }

    public void Stop()
    {
        _isRunning = false;
        _advertisingTimer?.Stop();
        _cleanupTimer?.Stop();
    }

    public void Dispose()
    {
        Stop();

        try
        {
            _advertisingTimer?.Dispose();
            _cleanupTimer?.Dispose();
            _udpClient?.Close();
            _udpClient?.Dispose();
        }
        catch { }

        System.Diagnostics.Debug.WriteLine("DiscoveryService: Disposed");
    }
}