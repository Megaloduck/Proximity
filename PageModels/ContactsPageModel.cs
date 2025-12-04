using Proximity.Models;
using Proximity.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Proximity.PageModels;

public class ContactsPageModel : INotifyPropertyChanged
{
    private readonly PeerInfo _peer;
    private readonly ChatService _chatService;
    private VoiceService _voiceService;
    private readonly ObservableCollection<ChatMessage> _messages = new();
    private System.Timers.Timer _callDurationTimer; // Changed from readonly

    private string _messageText;
    private bool _isVoiceActive;
    private bool _isTyping;
    private bool _hasMessageText;
    private string _callDuration = "00:00";
    private DateTime _callStartTime;

    public ContactsPageModel(PeerInfo peer, ChatService chatService)
    {
        _peer = peer;
        _chatService = chatService;

        InitializeCommands();
        LoadMessages();

        // Initialize call duration timer
        _callDurationTimer = new System.Timers.Timer(1000);
        _callDurationTimer.Elapsed += UpdateCallDuration;

        // Subscribe to events
        _chatService.MessageReceived += OnMessageReceived;
    }

    private void InitializeCommands()
    {
        SendCommand = new Command(async () => await SendMessage(), () => HasMessageText);
        ToggleVoiceCommand = new Command(async () => await ToggleVoice());
        ShowEmojiPickerCommand = new Command(() => { /* TODO: Show emoji picker */ });
    }

    private void LoadMessages()
    {
        var history = _chatService.GetChatHistory(_peer.PeerId);
        foreach (var msg in history)
        {
            _messages.Add(msg);
        }
    }

    private void OnMessageReceived(object sender, ChatMessage message)
    {
        if (message.SenderId == _peer.PeerId || message.ReceiverId == _peer.PeerId)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _messages.Add(message);
            });
        }
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText)) return;

        var message = new ChatMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            SenderId = _chatService.LocalPeerId,
            SenderName = Preferences.Get("username", "Me"),
            ReceiverId = _peer.PeerId,
            ReceiverName = _peer.Name,
            Content = MessageText,
            Timestamp = DateTime.Now,
            IsSentByMe = true,
            IsDelivered = false
        };

        _messages.Add(message);
        await _chatService.SendMessageAsync(_peer, MessageText);

        MessageText = string.Empty;

        // Simulate delivery confirmation
        await Task.Delay(500);
        message.IsDelivered = true;
    }

    private async Task ToggleVoice()
    {
        // Get VoiceService from DI if not already set
        if (_voiceService == null)
        {
            try
            {
                _voiceService = Application.Current?.Handler?.MauiContext?.Services?.GetService<VoiceService>();
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("ContactsPageModel: Could not get VoiceService");
                return;
            }
        }

        IsVoiceActive = !IsVoiceActive;

        if (IsVoiceActive)
        {
            _callStartTime = DateTime.Now;
            _callDurationTimer.Start();

            if (_voiceService != null)
            {
                await _voiceService.StartTransmittingAsync();
            }
        }
        else
        {
            _callDurationTimer.Stop();
            CallDuration = "00:00";

            if (_voiceService != null)
            {
                _voiceService.StopTransmitting();
            }
        }
    }

    private void UpdateCallDuration(object sender, System.Timers.ElapsedEventArgs e)
    {
        var duration = DateTime.Now - _callStartTime;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CallDuration = $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        });
    }

    // Properties
    public ObservableCollection<ChatMessage> Messages => _messages;

    public string PeerName => _peer.Name;
    public string PeerAvatar => _peer.Avatar ?? "👤";
    public string PeerStatus => _peer.IsOnline ? "🟢 Online" : "🔴 Offline";
    public string PeerIpAddress => _peer.IpAddress;

    public string MessageText
    {
        get => _messageText;
        set
        {
            _messageText = value;
            OnPropertyChanged();
            HasMessageText = !string.IsNullOrWhiteSpace(value);
        }
    }

    public bool HasMessageText
    {
        get => _hasMessageText;
        set
        {
            _hasMessageText = value;
            OnPropertyChanged();
            ((Command)SendCommand).ChangeCanExecute();
        }
    }

    public bool IsVoiceActive
    {
        get => _isVoiceActive;
        set { _isVoiceActive = value; OnPropertyChanged(); }
    }

    public bool IsTyping
    {
        get => _isTyping;
        set { _isTyping = value; OnPropertyChanged(); }
    }

    public string CallDuration
    {
        get => _callDuration;
        set { _callDuration = value; OnPropertyChanged(); }
    }

    // Commands
    public ICommand SendCommand { get; private set; }
    public ICommand ToggleVoiceCommand { get; private set; }
    public ICommand ShowEmojiPickerCommand { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}   