using Proximity.Models;
using Proximity.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Proximity.PageModels;

public class DiscoverPageModel : INotifyPropertyChanged
{
    private readonly DiscoveryService _discoveryService;
    private readonly ChatService _chatService;
    private readonly PingService _pingService;
    private string _localName;
    private string _discoveryStatusText;
    private string _lastScanTime;

    public DiscoverPageModel(DiscoveryService discoveryService, ChatService chatService)
    {
        _discoveryService = discoveryService;
        _chatService = chatService;
        _pingService = new PingService();

        LocalName = _discoveryService.LocalName;
        UpdateDiscoveryStatus();

        InitializeCommands();

        // Subscribe to discovery events
        _discoveryService.PeerDiscovered += OnPeerDiscovered;
    }

    private void InitializeCommands()
    {
        ConnectCommand = new Command<PeerInfo>(async (peer) => await ConnectToPeer(peer));
        DisconnectCommand = new Command<PeerInfo>(async (peer) => await DisconnectFromPeer(peer));
        PingCommand = new Command<PeerInfo>(async (peer) => await PingPeer(peer));
        RefreshCommand = new Command(async () => await RefreshPeers());
    }

    private void OnPeerDiscovered(object sender, PeerInfo peer)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateDiscoveryStatus();
        });
    }

    private void UpdateDiscoveryStatus()
    {
        var onlineCount = DiscoveredPeers.Count(p => p.IsOnline);
        DiscoveryStatusText = $"📡 {onlineCount} peers online • Scanning...";
        LastScanTime = $"Last scan: {DateTime.Now:HH:mm:ss}";
    }

    private async Task RefreshPeers()
    {
        _discoveryService.StartAdvertising();
        UpdateDiscoveryStatus();
        await Task.Delay(100);
    }

    private async Task ConnectToPeer(PeerInfo peer)
    {
        if (peer == null || peer.IsConnected) return;

        try
        {
            await _chatService.ConnectToPeerAsync(peer);
            peer.IsConnected = true;

            // Auto-ping after connect
            await PingPeer(peer);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Connect error: {ex.Message}");
        }
    }

    private async Task DisconnectFromPeer(PeerInfo peer)
    {
        if (peer == null || !peer.IsConnected) return;

        try
        {
            _chatService.DisconnectFromPeer(peer);
            peer.IsConnected = false;
            peer.Ping = 0;
            peer.HasPing = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Disconnect error: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task PingPeer(PeerInfo peer)
    {
        if (peer == null || string.IsNullOrEmpty(peer.IpAddress)) return;

        try
        {
            var latency = await _pingService.PingAsync(peer.IpAddress);

            if (latency > 0)
            {
                peer.Ping = latency;
                peer.HasPing = true;
            }
            else
            {
                peer.Ping = 0;
                peer.HasPing = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ping error: {ex.Message}");
            peer.HasPing = false;
        }
    }

    public void SaveUserName()
    {
        if (!string.IsNullOrWhiteSpace(LocalName))
        {
            _discoveryService.LocalName = LocalName;
            Preferences.Set("username", LocalName);
        }
    }

    // Properties
    public ObservableCollection<PeerInfo> DiscoveredPeers => _discoveryService.DiscoveredPeers;

    public string LocalName
    {
        get => _localName;
        set { _localName = value; OnPropertyChanged(); }
    }

    public string DiscoveryStatusText
    {
        get => _discoveryStatusText;
        set { _discoveryStatusText = value; OnPropertyChanged(); }
    }

    public string LastScanTime
    {
        get => _lastScanTime;
        set { _lastScanTime = value; OnPropertyChanged(); }
    }

    // Commands
    public ICommand ConnectCommand { get; private set; }
    public ICommand DisconnectCommand { get; private set; }
    public ICommand PingCommand { get; private set; }
    public ICommand RefreshCommand { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}