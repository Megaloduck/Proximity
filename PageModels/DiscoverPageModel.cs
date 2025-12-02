using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Proximity.Models;
using Proximity.Services;

namespace Proximity.PageModels
{
    public class DiscoverPageModel : BasePageModel
    {
        private readonly DiscoveryService _discoveryService;
        private readonly ChatService _chatService;
        private string _localName;

        public ObservableCollection<PeerInfo> DiscoveredPeers { get; } = new ObservableCollection<PeerInfo>();

        public string LocalName
        {
            get => _localName;
            set => SetProperty(ref _localName, value);
        }

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand RefreshCommand { get; }

        // ONLY Constructor - receives DI services
        public DiscoverPageModel(DiscoveryService discoveryService, ChatService chatService)
        {
            _discoveryService = discoveryService;
            _chatService = chatService;
            _localName = Preferences.Get("UserName", "User_" + Random.Shared.Next(1000, 9999));

            ConnectCommand = new Command<PeerInfo>(async (peer) => await ConnectToPeer(peer));
            DisconnectCommand = new Command<PeerInfo>(DisconnectFromPeer);
            RefreshCommand = new Command(RefreshPeers);

            HookDiscoveryEvents();
            RefreshPeers();
        }

        // REST OF THE CODE STAYS THE SAME...
        private void HookDiscoveryEvents()
        {
            _discoveryService.PeerDiscovered += OnPeerDiscovered;
            _discoveryService.PeerUpdated += OnPeerUpdated;
            _discoveryService.PeerLost += OnPeerLost;

            _chatService.ClientConnected += OnClientConnected;
            _chatService.ClientDisconnected += OnClientDisconnected;
        }

        private void OnPeerDiscovered(DiscoveredPeer peer)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!DiscoveredPeers.Any(p => p.Id == peer.Id))
                {
                    DiscoveredPeers.Add(new PeerInfo
                    {
                        Id = peer.Id,
                        Name = string.IsNullOrEmpty(peer.DisplayName)
                            ? $"Peer_{peer.Id.Substring(0, 8)}"
                            : $"{peer.Emoji} {peer.DisplayName}",
                        IpAddress = peer.Address.ToString(),
                        IsOnline = true,
                        IsConnected = false
                    });
                }
            });
        }

        private void OnPeerUpdated(DiscoveredPeer peer)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = DiscoveredPeers.FirstOrDefault(p => p.Id == peer.Id);
                if (existing != null)
                {
                    existing.IpAddress = peer.Address.ToString();
                    existing.IsOnline = true;
                }
            });
        }

        private void OnPeerLost(string peerId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var peer = DiscoveredPeers.FirstOrDefault(p => p.Id == peerId);
                if (peer != null)
                {
                    peer.IsOnline = false;
                    peer.IsConnected = false;
                }
            });
        }

        private void OnClientConnected(string clientId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var peer = DiscoveredPeers.FirstOrDefault(p => p.Id == clientId);
                if (peer != null)
                {
                    peer.IsConnected = true;
                }
            });
        }

        private void OnClientDisconnected(string clientId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var peer = DiscoveredPeers.FirstOrDefault(p => p.Id == clientId);
                if (peer != null)
                {
                    peer.IsConnected = false;
                }
            });
        }

        private async System.Threading.Tasks.Task ConnectToPeer(PeerInfo? peer)
        {
            if (peer == null || peer.IsConnected) return;

            try
            {
                var discovered = _discoveryService.GetDiscoveredPeers()
                    .FirstOrDefault(p => p.Id == peer.Id);

                if (discovered != null)
                {
                    await _chatService.ConnectToPeer(discovered.Address, peer.Id, discovered.ChatPort);
                    peer.IsConnected = true;
                }
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Connection Error",
                    $"Failed to connect: {ex.Message}",
                    "OK"
                );
            }
        }

        private void DisconnectFromPeer(PeerInfo? peer)
        {
            if (peer == null) return;
            peer.IsConnected = false;
        }

        private void RefreshPeers()
        {
            var discovered = _discoveryService.GetDiscoveredPeers();

            foreach (var peer in discovered)
            {
                var existing = DiscoveredPeers.FirstOrDefault(p => p.Id == peer.Id);
                if (existing == null)
                {
                    OnPeerDiscovered(peer);
                }
                else
                {
                    existing.IsOnline = true;
                }
            }
        }

        public void SaveUserName()
        {
            Preferences.Set("UserName", _localName);
        }
    }
}