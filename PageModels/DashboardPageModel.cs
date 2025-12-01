using System;
using System.Linq;
using System.Windows.Input;
using Proximity.Services;

namespace Proximity.PageModels
{
    public class DashboardPageModel : BasePageModel
    {
        private readonly DiscoveryService _discoveryService;
        private readonly ChatService _chatService;
        private readonly VoiceService _voiceService;
        private System.Threading.Timer? _refreshTimer;

        private string _welcomeMessage = string.Empty;
        private int _connectedPeersCount;
        private int _discoveredPeersCount;
        private int _messagesSentCount;
        private string _voiceStatus = "Ready";
        private string _localPeerId = string.Empty;
        private string _username = string.Empty;
        private string _connectionStatus = "Online";

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public int ConnectedPeersCount
        {
            get => _connectedPeersCount;
            set => SetProperty(ref _connectedPeersCount, value);
        }

        public int DiscoveredPeersCount
        {
            get => _discoveredPeersCount;
            set => SetProperty(ref _discoveredPeersCount, value);
        }

        public int MessagesSentCount
        {
            get => _messagesSentCount;
            set => SetProperty(ref _messagesSentCount, value);
        }

        public string VoiceStatus
        {
            get => _voiceStatus;
            set => SetProperty(ref _voiceStatus, value);
        }

        public string LocalPeerId
        {
            get => _localPeerId;
            set => SetProperty(ref _localPeerId, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand NavigateToDiscoverCommand { get; }
        public ICommand NavigateToContactsCommand { get; }

        public DashboardPageModel(DiscoveryService discoveryService, ChatService chatService, VoiceService voiceService)
        {
            _discoveryService = discoveryService;
            _chatService = chatService;
            _voiceService = voiceService;

            RefreshCommand = new Command(RefreshStats);
            NavigateToDiscoverCommand = new Command(NavigateToDiscover);
            NavigateToContactsCommand = new Command(NavigateToContacts);

            InitializeDashboard();
            StartAutoRefresh();
        }

        private void InitializeDashboard()
        {
            Username = Preferences.Get("UserName", "User");
            LocalPeerId = _discoveryService.GetLocalId().Substring(0, 8) + "...";

            var hour = DateTime.Now.Hour;
            var greeting = hour < 12 ? "Good Morning" : hour < 18 ? "Good Afternoon" : "Good Evening";
            WelcomeMessage = $"{greeting}, {Username}! 👋";

            RefreshStats();
        }

        private void RefreshStats()
        {
            DiscoveredPeersCount = _discoveryService.GetDiscoveredPeers().Length;
            ConnectedPeersCount = _chatService.GetConnectedPeerIds().Length;
            MessagesSentCount = _chatService.GetMessageHistory().Count(m => m.SenderId == _discoveryService.GetLocalId());
            VoiceStatus = _voiceService != null ? "Ready" : "Unavailable";
            ConnectionStatus = DiscoveredPeersCount > 0 ? "🟢 Online" : "🔴 Searching...";
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new System.Threading.Timer(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() => RefreshStats());
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
        }

        private void NavigateToDiscover()
        {
            Application.Current?.MainPage?.Navigation.PushAsync(
                new Pages.MainMenu.DiscoverPage(_discoveryService, _chatService));
        }

        private void NavigateToContacts()
        {
            Application.Current?.MainPage?.Navigation.PushAsync(
                new Pages.Tools.ContactsPage(null, _chatService));
        }
    }
}