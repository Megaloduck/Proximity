using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Proximity.Models;
using Proximity.Services;

namespace Proximity.PageModels
{
    public class DiscoverPageModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly DiscoveryService _discoveryService;
        private readonly ChatService _chatService;

        // Local Name
        private string _localName;
        public string LocalName
        {
            get => _localName;
            set { _localName = value; OnPropertyChanged(); }
        }

        // Discovered Peers
        public ObservableCollection<PeerInfo> DiscoveredPeers => _discoveryService.DiscoveredPeers;

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        public DiscoverPageModel(DiscoveryService discoveryService, ChatService chatService)
        {
            _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));

            LocalName = _discoveryService.MyDeviceName;

            // Initialize commands
            RefreshCommand = new Command(async () => await RefreshPeersAsync());
            ConnectCommand = new Command<PeerInfo>(async (peer) => await ConnectToPeerAsync(peer));
            DisconnectCommand = new Command<PeerInfo>(async (peer) => await DisconnectFromPeerAsync(peer));

            // Start discovery automatically
            _ = StartDiscoveryAsync();
        }

        private async Task StartDiscoveryAsync()
        {
            try
            {
                if (!_discoveryService.DiscoveredPeers.Any())
                {
                    await _discoveryService.StartAsync();
                    await _chatService.StartAsync();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Discovery Error", ex.Message, "OK");
            }
        }

        private async Task RefreshPeersAsync()
        {
            try
            {
                // Restart discovery to force refresh
                await _discoveryService.StopAsync();
                await Task.Delay(500);
                await _discoveryService.StartAsync();

                await Application.Current.MainPage.DisplayAlert("Success", "Peer list refreshed", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Refresh Error", ex.Message, "OK");
            }
        }

        private async Task ConnectToPeerAsync(PeerInfo peer)
        {
            if (peer == null) return;

            try
            {
                // Test connection by sending a hello message
                var success = await _chatService.SendMessageAsync(peer, "Hello! Connected via Proximity.");

                if (success)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Connected",
                        $"Successfully connected to {peer.DeviceName}",
                        "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Connection Failed",
                        $"Could not connect to {peer.DeviceName}",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Connection Error", ex.Message, "OK");
            }
        }

        private async Task DisconnectFromPeerAsync(PeerInfo peer)
        {
            if (peer == null) return;

            try
            {
                // Send goodbye message
                await _chatService.SendMessageAsync(peer, "Disconnected from Proximity.");

                await Application.Current.MainPage.DisplayAlert(
                    "Disconnected",
                    $"Disconnected from {peer.DeviceName}",
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Disconnect Error", ex.Message, "OK");
            }
        }

        public void SaveUserName()
        {
            if (!string.IsNullOrWhiteSpace(LocalName))
            {
                // TODO: Persist to preferences
                Preferences.Set("UserName", LocalName);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}