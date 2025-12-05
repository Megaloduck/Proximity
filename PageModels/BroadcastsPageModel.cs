using Proximity.Models;
using Proximity.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Proximity.PageModels;

public class BroadcastsPageModel : INotifyPropertyChanged
{
    private readonly BroadcastService _broadcastService;
    private readonly DiscoveryService _discoveryService;
    private string _broadcastMessage;
    private string _selectedPriority = "Normal";
    private bool _hasMessage;

    public BroadcastsPageModel(BroadcastService broadcastService, DiscoveryService discoveryService)
    {
        _broadcastService = broadcastService;
        _discoveryService = discoveryService;

        InitializeCommands();

        // Subscribe to events
        _broadcastService.BroadcastReceived += OnBroadcastReceived;
        _broadcastService.BroadcastSent += OnBroadcastSent;
    }

    private void InitializeCommands()
    {
        SendBroadcastCommand = new Command(async () => await SendBroadcast(), () => HasMessage);
        ClearMessageCommand = new Command(() => BroadcastMessage = string.Empty);
        ClearHistoryCommand = new Command(() => _broadcastService.ClearHistory());
    }

    private async Task SendBroadcast()
    {
        if (string.IsNullOrWhiteSpace(BroadcastMessage))
            return;

        try
        {
            var priority = SelectedPriority == "High" ? BroadcastPriority.High : BroadcastPriority.Normal;
            var success = await _broadcastService.SendBroadcastAsync(BroadcastMessage, priority);

            if (success)
            {
                BroadcastMessage = string.Empty;
            }
            else
            {
                await Application.Current?.MainPage?.DisplayAlert("Error", "Failed to send broadcast", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BroadcastsPageModel SendBroadcast error: {ex.Message}");
            await Application.Current?.MainPage?.DisplayAlert("Error", $"Failed to send broadcast: {ex.Message}", "OK");
        }
    }

    private void OnBroadcastReceived(object sender, BroadcastMessage message)
    {
        // Already handled by the service adding to history
    }

    private void OnBroadcastSent(object sender, BroadcastMessage message)
    {
        // Already handled by the service adding to history
    }

    // Properties
    public ObservableCollection<BroadcastMessage> BroadcastHistory => _broadcastService.BroadcastHistory;

    public int ConnectedPeersCount => _discoveryService.DiscoveredPeers.Count(p => p.IsOnline);

    public int BroadcastsSent => _broadcastService.BroadcastsSent;

    public string BroadcastMessage
    {
        get => _broadcastMessage;
        set
        {
            _broadcastMessage = value;
            OnPropertyChanged();
            HasMessage = !string.IsNullOrWhiteSpace(value);
        }
    }

    public bool HasMessage
    {
        get => _hasMessage;
        set
        {
            _hasMessage = value;
            OnPropertyChanged();
            ((Command)SendBroadcastCommand).ChangeCanExecute();
        }
    }

    public string SelectedPriority
    {
        get => _selectedPriority;
        set { _selectedPriority = value; OnPropertyChanged(); }
    }

    public List<string> PriorityOptions => new() { "Normal", "High" };

    // Commands
    public ICommand SendBroadcastCommand { get; private set; }
    public ICommand ClearMessageCommand { get; private set; }
    public ICommand ClearHistoryCommand { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}