using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Proximity.Services;

namespace Proximity.PageModels
{
    public class DashboardPageModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly DiscoveryService _discoveryService;
        private readonly ChatService _chatService;
        private readonly VoiceService _voiceService;

        // Welcome
        private string _welcomeMessage = "Stay connected with your local network";
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set { _welcomeMessage = value; OnPropertyChanged(); }
        }

        // Statistics
        public int ConnectedPeersCount => _discoveryService?.DiscoveredPeers?.Count(p => p.IsOnline) ?? 0;
        public int DiscoveredPeersCount => _discoveryService?.DiscoveredPeers?.Count ?? 0;

        private int _messagesSentCount = 0;
        public int MessagesSentCount
        {
            get => _messagesSentCount;
            set { _messagesSentCount = value; OnPropertyChanged(); }
        }

        public string VoiceStatus => _voiceService?.IsInCall == true ? "In Call" : "Ready";

        // Network Info
        public string LocalPeerId => _discoveryService?.MyDeviceId ?? "Not Set";
        public string Username => _discoveryService?.MyDeviceName ?? "Guest";
        public string ConnectionStatus => "Online";

        // Commands
        public ICommand NavigateToDiscoverCommand { get; }
        public ICommand NavigateToContactsCommand { get; }
        public ICommand RefreshCommand { get; }

        public DashboardPageModel(DiscoveryService discoveryService, ChatService chatService, VoiceService voiceService)
        {
            _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _voiceService = voiceService ?? throw new ArgumentNullException(nameof(voiceService));

            // Subscribe to events
            if (_discoveryService.DiscoveredPeers != null)
            {
                _discoveryService.DiscoveredPeers.CollectionChanged += (s, e) =>
                {
                    OnPropertyChanged(nameof(ConnectedPeersCount));
                    OnPropertyChanged(nameof(DiscoveredPeersCount));
                };
            }

            _chatService.OnMessageReceived += (msg) =>
            {
                MessagesSentCount++;
            };

            _voiceService.OnCallStarted += () => OnPropertyChanged(nameof(VoiceStatus));
            _voiceService.OnCallEnded += () => OnPropertyChanged(nameof(VoiceStatus));

            // Initialize commands
            NavigateToDiscoverCommand = new Command(async () => await NavigateToDiscoverAsync());
            NavigateToContactsCommand = new Command(async () => await NavigateToContactsAsync());
            RefreshCommand = new Command(() => RefreshStatistics());

            // Set personalized welcome message
            UpdateWelcomeMessage();
        }

        private void UpdateWelcomeMessage()
        {
            var hour = DateTime.Now.Hour;
            var greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";
            WelcomeMessage = $"{greeting}, {Username}! Ready to connect?";
        }

        private async Task NavigateToDiscoverAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//DiscoverPage");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Navigation Error", ex.Message, "OK");
            }
        }

        private async Task NavigateToContactsAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//ContactsPage");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Navigation Error", ex.Message, "OK");
            }
        }

        private void RefreshStatistics()
        {
            OnPropertyChanged(nameof(ConnectedPeersCount));
            OnPropertyChanged(nameof(DiscoveredPeersCount));
            OnPropertyChanged(nameof(VoiceStatus));
            OnPropertyChanged(nameof(ConnectionStatus));
            UpdateWelcomeMessage();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}