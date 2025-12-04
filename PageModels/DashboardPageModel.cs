using Proximity.Models;
using Proximity.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Proximity.PageModels;

public class DashboardPageModel : INotifyPropertyChanged
{
    private readonly DiscoveryService _discoveryService;
    private readonly ChatService _chatService;
    private readonly VoiceService _voiceService;
    private System.Timers.Timer _refreshTimer; // Changed from readonly to allow assignment
    private readonly ObservableCollection<ActivityInfo> _recentActivities = new();

    private string _welcomeMessage;
    private int _connectedPeersCount;
    private int _discoveredPeersCount;
    private int _messagesSentCount;
    private string _voiceStatus;
    private string _localPeerId;
    private string _username;
    private string _connectionStatus;
    private bool _isConnected;
    private string _lastActivityTime;
    private string _uptime;
    private DateTime _startTime;

    public DashboardPageModel(DiscoveryService discoveryService, ChatService chatService, VoiceService voiceService)
    {
        _discoveryService = discoveryService;
        _chatService = chatService;
        _voiceService = voiceService;
        _startTime = DateTime.Now;

        InitializeCommands();
        LoadData();
        StartAutoRefresh();

        // Subscribe to service events
        _discoveryService.PeerDiscovered += OnPeerDiscovered;
        _chatService.MessageSent += OnMessageSent;
    }

    private void InitializeCommands()
    {
        NavigateToDashboardCommand = new Command(async () => { });
        NavigateToDiscoverCommand = new Command(async () => { });
        NavigateToContactsCommand = new Command(async () => { });
        NavigateToRoomsCommand = new Command(async () => { });
        RefreshCommand = new Command(LoadData);
    }

    private void LoadData()
    {
        Username = _discoveryService.LocalName;
        LocalPeerId = _discoveryService.LocalPeerId;
        WelcomeMessage = $"Hello, {Username}! Your network is ready.";

        ConnectedPeersCount = _discoveryService.DiscoveredPeers.Count(p => p.IsConnected);
        DiscoveredPeersCount = _discoveryService.DiscoveredPeers.Count;
        MessagesSentCount = _chatService.GetTotalMessagesSent();

        VoiceStatus = _voiceService.IsInCall ? "Active" : "Inactive";
        ConnectionStatus = _discoveryService.IsRunning ? "🟢 Online" : "🔴 Offline";
        IsConnected = _discoveryService.IsRunning;

        LastActivityTime = "Last activity: " + DateTime.Now.ToString("HH:mm:ss");
        UpdateUptime();
    }

    private void StartAutoRefresh()
    {
        _refreshTimer = new System.Timers.Timer(5000); // Refresh every 5 seconds
        _refreshTimer.Elapsed += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadData();
            });
        };
        _refreshTimer.Start();
    }

    private void UpdateUptime()
    {
        var uptime = DateTime.Now - _startTime;
        Uptime = $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
    }

    private void OnPeerDiscovered(object sender, Models.PeerInfo peer)
    {
        AddActivity("🔍", $"Discovered {peer.Name}", DateTime.Now);
        LoadData();
    }

    private void OnMessageSent(object sender, Models.ChatMessage message)
    {
        AddActivity("💬", $"Message sent to {message.ReceiverName}", DateTime.Now);
        LoadData();
    }

    private void AddActivity(string icon, string message, DateTime timestamp)
    {
        var activity = new ActivityInfo
        {
            Icon = icon,
            Message = message,
            Timestamp = timestamp,
            TimeAgo = GetTimeAgo(timestamp)
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _recentActivities.Insert(0, activity);
            if (_recentActivities.Count > 10)
            {
                _recentActivities.RemoveAt(_recentActivities.Count - 1);
            }
        });
    }

    private string GetTimeAgo(DateTime timestamp)
    {
        var span = DateTime.Now - timestamp;
        if (span.TotalSeconds < 60)
            return "just now";
        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours}h ago";
        return $"{(int)span.TotalDays}d ago";
    }

    // Properties
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set { _welcomeMessage = value; OnPropertyChanged(); }
    }

    public int ConnectedPeersCount
    {
        get => _connectedPeersCount;
        set { _connectedPeersCount = value; OnPropertyChanged(); }
    }

    public int DiscoveredPeersCount
    {
        get => _discoveredPeersCount;
        set { _discoveredPeersCount = value; OnPropertyChanged(); }
    }

    public int MessagesSentCount
    {
        get => _messagesSentCount;
        set { _messagesSentCount = value; OnPropertyChanged(); }
    }

    public string VoiceStatus
    {
        get => _voiceStatus;
        set { _voiceStatus = value; OnPropertyChanged(); }
    }

    public string LocalPeerId
    {
        get => _localPeerId;
        set { _localPeerId = value; OnPropertyChanged(); }
    }

    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set { _connectionStatus = value; OnPropertyChanged(); }
    }

    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); }
    }

    public string LastActivityTime
    {
        get => _lastActivityTime;
        set { _lastActivityTime = value; OnPropertyChanged(); }
    }

    public string Uptime
    {
        get => _uptime;
        set { _uptime = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ActivityInfo> RecentActivities => _recentActivities;

    // Commands
    public ICommand NavigateToDashboardCommand { get; private set; }
    public ICommand NavigateToDiscoverCommand { get; private set; }
    public ICommand NavigateToContactsCommand { get; private set; }
    public ICommand NavigateToRoomsCommand { get; private set; }
    public ICommand RefreshCommand { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}