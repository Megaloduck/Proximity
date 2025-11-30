using System;
using System.Collections.Concurrent;
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
        private const int DiscoveryPort = 9001; // UDP broadcast port
        private readonly UdpClient _udpSend;
        private readonly UdpClient _udpRecv;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, DiscoveredPeer> _peers = new ConcurrentDictionary<string, DiscoveredPeer>();


        public event Action<DiscoveredPeer> PeerDiscovered;
        public event Action<DiscoveredPeer> PeerUpdated;


        public DiscoveryService()
        {
            _udpSend = new UdpClient();
            _udpSend.EnableBroadcast = true;


            _udpRecv = new UdpClient(DiscoveryPort);
            _udpRecv.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);


            _ = ListenLoop(_cts.Token);
            _ = BroadcastLoop(_cts.Token);
        }


        private string GetLocalId() => Guid.NewGuid().ToString(); // replace with persistent id if needed


        private async Task BroadcastLoop(CancellationToken ct)
        {
            var id = GetLocalId();
            var localIp = GetLocalIPAddress();
            var payload = $"DISCOVER_CHAT|{id}|{localIp}|{9002}|{9003}"; // id|ip|chatPort|voicePort
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


    }
}
