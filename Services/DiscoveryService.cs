using System;
using System.Collections.Concurrent;
using System.Linq;
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
        private const int DiscoveryPort = 9001;
        private readonly UdpClient _udpSend;
        private readonly UdpClient _udpRecv;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, DiscoveredPeer> _peers = new ConcurrentDictionary<string, DiscoveredPeer>();
        private readonly string _localId;

        public event Action<DiscoveredPeer>? PeerDiscovered;
        public event Action<DiscoveredPeer>? PeerUpdated;
        public event Action<string>? PeerLost;

        public DiscoveryService()
        {
            _localId = GetOrCreateLocalId();

            _udpSend = new UdpClient();
            _udpSend.EnableBroadcast = true;

            _udpRecv = new UdpClient(DiscoveryPort);
            _udpRecv.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _ = ListenLoop(_cts.Token);
            _ = BroadcastLoop(_cts.Token);
            _ = MonitorPeers(_cts.Token);
        }

        private string GetOrCreateLocalId()
        {
            var id = Preferences.Get("LocalPeerId", string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
                Preferences.Set("LocalPeerId", id);
            }
            return id;
        }

        public string GetLocalId() => _localId;

        private async Task BroadcastLoop(CancellationToken ct)
        {
            var localIp = GetLocalIPAddress();
            var displayName = Preferences.Get("ProfileDisplayName", Preferences.Get("UserName", "User"));
            var status = Preferences.Get("ProfileStatus", "");
            var emoji = Preferences.Get("ProfileEmoji", "😀");

            // Updated protocol to include profile info
            var payload = $"DISCOVER_CHAT|{_localId}|{localIp}|9002|9003|{displayName}|{status}|{emoji}";
            var data = Encoding.UTF8.GetBytes(payload);
            var endpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _udpSend.SendAsync(data, data.Length, endpoint);
                }
                catch { }

                await Task.Delay(2000, ct);
            }
        }

        private async Task ListenLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpRecv.ReceiveAsync();
                    var message = Encoding.UTF8.GetString(result.Buffer);

                    if (message.StartsWith("DISCOVER_CHAT|"))
                    {
                        var parts = message.Split('|');
                        if (parts.Length >= 5)
                        {
                            var id = parts[1];
                            var ipStr = parts[2];
                            var chatPort = int.Parse(parts[3]);
                            var voicePort = int.Parse(parts[4]);

                            // Optional profile fields
                            var displayName = parts.Length > 5 ? parts[5] : $"Peer_{id.Substring(0, 8)}";
                            var status = parts.Length > 6 ? parts[6] : "";
                            var emoji = parts.Length > 7 ? parts[7] : "😀";

                            if (id == _localId) continue;

                            var peer = new DiscoveredPeer
                            {
                                Id = id,
                                Address = IPAddress.Parse(ipStr),
                                ChatPort = chatPort,
                                VoicePort = voicePort,
                                LastSeen = DateTime.Now,
                                DisplayName = displayName,
                                StatusMessage = status,
                                Emoji = emoji
                            };

                            if (_peers.TryGetValue(id, out var existing))
                            {
                                _peers[id] = peer;
                                PeerUpdated?.Invoke(peer);
                            }
                            else
                            {
                                _peers.TryAdd(id, peer);
                                PeerDiscovered?.Invoke(peer);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private async Task MonitorPeers(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, ct);

                    var stale = _peers.Where(kv => (DateTime.Now - kv.Value.LastSeen).TotalSeconds > 10)
                                      .Select(kv => kv.Key)
                                      .ToList();

                    foreach (var key in stale)
                    {
                        if (_peers.TryRemove(key, out _))
                        {
                            PeerLost?.Invoke(key);
                        }
                    }
                }
                catch { }
            }
        }

        private string GetLocalIPAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        public DiscoveredPeer[] GetDiscoveredPeers()
        {
            return _peers.Values.ToArray();
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _udpSend?.Close();
            _udpRecv?.Close();
        }
    }
}