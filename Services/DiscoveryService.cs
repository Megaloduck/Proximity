using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Proximity.Models;

namespace Proximity.Services
{
    public class DiscoveryService : IDisposable
    {
        private const int DISCOVERY_PORT = 8888;
        private const int BROADCAST_INTERVAL_MS = 3000; // 3 seconds
        private const int CLEANUP_INTERVAL_MS = 5000; // 5 seconds
        private const int PEER_TIMEOUT_SECONDS = 15;

        private UdpClient _udpSender;
        private UdpClient _udpListener;
        private CancellationTokenSource _cts;
        private Task _broadcastTask;
        private Task _listenTask;
        private Task _cleanupTask;

        public string MyDeviceId { get; private set; }
        public string MyDeviceName { get; private set; }
        public int MyPort { get; private set; }
        public ObservableCollection<PeerInfo> DiscoveredPeers { get; }

        private bool _isRunning = false;

        public event Action<PeerInfo> OnPeerDiscovered;
        public event Action<PeerInfo> OnPeerLost;

        public DiscoveryService(string deviceName, int port = 9001)
        {
            MyDeviceId = Guid.NewGuid().ToString("N").Substring(0, 8);
            MyDeviceName = deviceName;
            MyPort = port;
            DiscoveredPeers = new ObservableCollection<PeerInfo>();
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;

            try
            {
                _cts = new CancellationTokenSource();

                // Setup UDP sender for broadcasts
                _udpSender = new UdpClient();
                _udpSender.EnableBroadcast = true;

                // Setup UDP listener for receiving broadcasts
                _udpListener = new UdpClient();
                _udpListener.ExclusiveAddressUse = false;
                _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, DISCOVERY_PORT));

                // Start background tasks
                _broadcastTask = Task.Run(() => BroadcastPresenceLoopAsync(_cts.Token));
                _listenTask = Task.Run(() => ListenForPeersLoopAsync(_cts.Token));
                _cleanupTask = Task.Run(() => CleanupInactivePeersLoopAsync(_cts.Token));

                _isRunning = true;
                Debug.WriteLine($"[Discovery] Started - DeviceId: {MyDeviceId}, Name: {MyDeviceName}, Port: {MyPort}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Discovery] Start failed: {ex.Message}");
                throw;
            }
        }
        public async Task StopAsync()
        {
            if (!_isRunning) return;

            try
            {
                _cts?.Cancel();

                await Task.WhenAll(
                    _broadcastTask ?? Task.CompletedTask,
                    _listenTask ?? Task.CompletedTask,
                    _cleanupTask ?? Task.CompletedTask
                ).ConfigureAwait(false);

                _udpSender?.Close();
                _udpListener?.Close();

                DiscoveredPeers.Clear();
                _isRunning = false;

                Debug.WriteLine("[Discovery] Stopped");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Discovery] Stop error: {ex.Message}");
            }
        }
        private async Task BroadcastPresenceLoopAsync(CancellationToken token)
        {
            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, DISCOVERY_PORT);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Format: "DISCOVER|DeviceId|DeviceName|Port"
                    var message = $"DISCOVER|{MyDeviceId}|{MyDeviceName}|{MyPort}";
                    var bytes = Encoding.UTF8.GetBytes(message);

                    await _udpSender.SendAsync(bytes, bytes.Length, broadcastEndpoint);
                    Debug.WriteLine($"[Discovery] Broadcast sent: {message}");

                    await Task.Delay(BROADCAST_INTERVAL_MS, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Discovery] Broadcast error: {ex.Message}");
                    await Task.Delay(1000, token);
                }
            }
        }
        private async Task ListenForPeersLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync();
                    var message = Encoding.UTF8.GetString(result.Buffer);
                    var senderIp = result.RemoteEndPoint.Address.ToString();

                    // Ignore our own broadcasts
                    if (IsLocalIpAddress(senderIp))
                        continue;

                    Debug.WriteLine($"[Discovery] Received from {senderIp}: {message}");

                    // Parse: "DISCOVER|DeviceId|DeviceName|Port"
                    var parts = message.Split('|');
                    if (parts.Length == 4 && parts[0] == "DISCOVER")
                    {
                        var deviceId = parts[1];
                        var deviceName = parts[2];
                        var port = int.Parse(parts[3]);

                        // Ignore our own device
                        if (deviceId == MyDeviceId)
                            continue;

                        await UpdatePeerAsync(deviceId, deviceName, senderIp, port);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Discovery] Listen error: {ex.Message}");
                    await Task.Delay(100, token);
                }
            }
        }
        private async Task CleanupInactivePeersLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(CLEANUP_INTERVAL_MS, token);

                    var now = DateTime.UtcNow;
                    var peersToRemove = new List<PeerInfo>();

                    foreach (var peer in DiscoveredPeers)
                    {
                        if ((now - peer.LastSeen).TotalSeconds > PEER_TIMEOUT_SECONDS)
                        {
                            peersToRemove.Add(peer);
                        }
                    }

                    foreach (var peer in peersToRemove)
                    {
                        await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            DiscoveredPeers.Remove(peer);
                        });

                        OnPeerLost?.Invoke(peer);
                        Debug.WriteLine($"[Discovery] Peer lost: {peer.DeviceName} ({peer.IpAddress})");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Discovery] Cleanup error: {ex.Message}");
                }
            }
        }
        private async Task UpdatePeerAsync(string deviceId, string deviceName, string ipAddress, int port)
        {
            var existingPeer = DiscoveredPeers.FirstOrDefault(p => p.DeviceId == deviceId);

            if (existingPeer != null)
            {
                // Update existing peer
                existingPeer.LastSeen = DateTime.UtcNow;
                existingPeer.DeviceName = deviceName;
                existingPeer.IpAddress = ipAddress;
                existingPeer.Port = port;
            }
            else
            {
                // Add new peer
                var newPeer = new PeerInfo
                {
                    DeviceId = deviceId,
                    DeviceName = deviceName,
                    IpAddress = ipAddress,
                    Port = port,
                    LastSeen = DateTime.UtcNow
                };

                await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(() =>
                {
                    DiscoveredPeers.Add(newPeer);
                });

                OnPeerDiscovered?.Invoke(newPeer);
                Debug.WriteLine($"[Discovery] New peer: {deviceName} ({ipAddress}:{port})");
            }
        }
        private bool IsLocalIpAddress(string ipAddress)
        {
            try
            {
                var hostName = Dns.GetHostName();
                var localIPs = Dns.GetHostAddresses(hostName);
                return localIPs.Any(ip => ip.ToString() == ipAddress);
            }
            catch
            {
                return false;
            }
        }
        public void Dispose()
        {
            StopAsync().Wait();
            _cts?.Dispose();
            _udpSender?.Dispose();
            _udpListener?.Dispose();
        }
    }
}