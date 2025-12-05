using Proximity.Models;
using Proximity.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Proximity.PageModels;

public class AuditoriumPageModel : INotifyPropertyChanged
{
    private readonly AuditoriumService _auditoriumService;
    private System.Timers.Timer _durationTimer; 
    private bool _isSessionActive;
    private bool _isScreenSharing;
    private bool _hasActivePresentation;
    private bool _isMicMuted = true;
    private bool _isSpeakerMuted = false;
    private string _sessionName;
    private string _sessionDuration = "00:00:00";
    private string _presenterName;
    private string _presenterAvatar;
    private int _viewerCount;
    private ImageSource _screenSharePreview;

    public AuditoriumPageModel(AuditoriumService auditoriumService)
    {
        _auditoriumService = auditoriumService;

        InitializeCommands();
        InitializeDurationTimer();

        // Subscribe to events
        _auditoriumService.SessionStarted += OnSessionStarted;
        _auditoriumService.SessionEnded += OnSessionEnded;
        _auditoriumService.ScreenShareStarted += OnScreenShareStarted;
        _auditoriumService.ScreenShareStopped += OnScreenShareStopped;
        _auditoriumService.ParticipantJoined += OnParticipantJoined;
        _auditoriumService.ParticipantLeft += OnParticipantLeft;
    }

    private void InitializeCommands()
    {
        CreateSessionCommand = new Command(async () => await CreateSession());
        JoinSessionCommand = new Command(async () => await JoinSession());
        EndSessionCommand = new Command(async () => await EndSession());
        LeaveAuditoriumCommand = new Command(async () => await LeaveAuditorium());
        StartScreenShareCommand = new Command(async () => await StartScreenShare());
        StopScreenShareCommand = new Command(async () => await StopScreenShare());
        ToggleMicCommand = new Command(async () => await ToggleMicrophone());
        ToggleSpeakerCommand = new Command(() => ToggleSpeaker());
        ToggleChatCommand = new Command(() => { /* TODO: Implement chat panel */ });
    }

    private void InitializeDurationTimer()
    {
        _durationTimer = new System.Timers.Timer(1000); // Update every second
        _durationTimer.Elapsed += (s, e) =>
        {
            if (IsSessionActive)
            {
                var duration = _auditoriumService.GetSessionDuration();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SessionDuration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                });
            }
        };
    }

    private async Task CreateSession()
    {
        var sessionName = $"Session {DateTime.Now:HHmm}";
        await _auditoriumService.CreateSessionAsync(sessionName);
    }

    private async Task JoinSession()
    {
        // TODO: Show session picker dialog
        var sessionId = "default-session";
        await _auditoriumService.JoinSessionAsync(sessionId);

        IsSessionActive = true;
        SessionName = "Joined Session";
        _durationTimer.Start();
    }

    private async Task EndSession()
    {
        await _auditoriumService.LeaveSessionAsync();
    }

    private async Task LeaveAuditorium()
    {
        await _auditoriumService.LeaveSessionAsync();
    }

    private async Task StartScreenShare()
    {
        try
        {
            await _auditoriumService.StartScreenShareAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Screen share error: {ex.Message}");
        }
    }

    private async Task StopScreenShare()
    {
        await _auditoriumService.StopScreenShareAsync();
    }

    private async Task ToggleMicrophone()
    {
        IsMicMuted = !IsMicMuted;
        await _auditoriumService.ToggleMicrophoneAsync(!IsMicMuted);
    }

    private void ToggleSpeaker()
    {
        IsSpeakerMuted = !IsSpeakerMuted;
        // TODO: Implement speaker mute in VoiceService
    }

    // Event Handlers
    private void OnSessionStarted(object sender, string sessionName)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsSessionActive = true;
            SessionName = sessionName;
            _durationTimer.Start();
        });
    }

    private void OnSessionEnded(object sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsSessionActive = false;
            SessionName = null;
            SessionDuration = "00:00:00";
            _durationTimer.Stop();
        });
    }

    private void OnScreenShareStarted(object sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsScreenSharing = true;
            HasActivePresentation = true;
            PresenterName = Preferences.Get("username", "You");
            PresenterAvatar = Preferences.Get("user_avatar", "👤");
        });
    }

    private void OnScreenShareStopped(object sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsScreenSharing = false;
            HasActivePresentation = false;
        });
    }

    private void OnParticipantJoined(object sender, ParticipantInfo participant)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ViewerCount = Participants.Count;
        });
    }

    private void OnParticipantLeft(object sender, ParticipantInfo participant)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ViewerCount = Participants.Count;
        });
    }

    // Properties
    public ObservableCollection<ParticipantInfo> Participants => _auditoriumService.Participants;

    public int ParticipantCount => Participants.Count;

    public bool IsSessionActive
    {
        get => _isSessionActive;
        set { _isSessionActive = value; OnPropertyChanged(); }
    }

    public bool IsScreenSharing
    {
        get => _isScreenSharing;
        set { _isScreenSharing = value; OnPropertyChanged(); }
    }

    public bool HasActivePresentation
    {
        get => _hasActivePresentation;
        set { _hasActivePresentation = value; OnPropertyChanged(); }
    }

    public bool IsMicMuted
    {
        get => _isMicMuted;
        set { _isMicMuted = value; OnPropertyChanged(); }
    }

    public bool IsSpeakerMuted
    {
        get => _isSpeakerMuted;
        set { _isSpeakerMuted = value; OnPropertyChanged(); }
    }

    public string SessionName
    {
        get => _sessionName;
        set { _sessionName = value; OnPropertyChanged(); }
    }

    public string SessionDuration
    {
        get => _sessionDuration;
        set { _sessionDuration = value; OnPropertyChanged(); }
    }

    public string PresenterName
    {
        get => _presenterName;
        set { _presenterName = value; OnPropertyChanged(); }
    }

    public string PresenterAvatar
    {
        get => _presenterAvatar;
        set { _presenterAvatar = value; OnPropertyChanged(); }
    }

    public int ViewerCount
    {
        get => _viewerCount;
        set { _viewerCount = value; OnPropertyChanged(); }
    }

    public ImageSource ScreenSharePreview
    {
        get => _screenSharePreview;
        set { _screenSharePreview = value; OnPropertyChanged(); }
    }

    // Commands
    public ICommand CreateSessionCommand { get; private set; }
    public ICommand JoinSessionCommand { get; private set; }
    public ICommand EndSessionCommand { get; private set; }
    public ICommand LeaveAuditoriumCommand { get; private set; }
    public ICommand StartScreenShareCommand { get; private set; }
    public ICommand StopScreenShareCommand { get; private set; }
    public ICommand ToggleMicCommand { get; private set; }
    public ICommand ToggleSpeakerCommand { get; private set; }
    public ICommand ToggleChatCommand { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}   