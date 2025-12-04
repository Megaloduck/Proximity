using Proximity.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Proximity.Services;

public class AuditoriumService : IDisposable
{
    private readonly ObservableCollection<ParticipantInfo> _participants = new();
    private readonly VoiceService _voiceService;
    private readonly UdpClient _udpClient;
    private string _currentSessionId;
    private bool _isSessionActive;
    private bool _isScreenSharing;
    private DateTime _sessionStartTime;
    private bool _isListening;
    private const int AUDITORIUM_PORT = 9005;

    public ObservableCollection<ParticipantInfo> Participants => _participants;

    public bool IsSessionActive => _isSessionActive;
    public bool IsScreenSharing => _isScreenSharing;
    public string CurrentSessionId => _currentSessionId;
    public string SessionName { get; private set; }

    public event EventHandler<ParticipantInfo> ParticipantJoined;
    public event EventHandler<ParticipantInfo> ParticipantLeft;
    public event EventHandler<string> SessionStarted;
    public event EventHandler SessionEnded;
    public event EventHandler ScreenShareStarted;
    public event EventHandler ScreenShareStopped;
    public event EventHandler<ParticipantInfo> ParticipantSpeaking;

    public AuditoriumService(VoiceService voiceService)
    {
        _voiceService = voiceService;

        try
        {
            _udpClient = new UdpClient(AUDITORIUM_PORT);
            StartListening();
            System.Diagnostics.Debug.WriteLine($"AuditoriumService: Started on port {AUDITORIUM_PORT}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuditoriumService init error: {ex.Message}");
        }
    }

    private async void StartListening()
    {
        _isListening = true;

        try
        {
            while (_isListening)
            {
                var result = await _udpClient.ReceiveAsync();
                var json = Encoding.UTF8.GetString(result.Buffer);

                try
                {
                    var message = JsonSerializer.Deserialize<AuditoriumMessage>(json);

                    if (message != null)
                    {
                        await HandleAuditoriumMessage(message);
                    }
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"AuditoriumService JSON parse error: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            if (_isListening)
            {
                System.Diagnostics.Debug.WriteLine($"AuditoriumService listening error: {ex.Message}");
            }
        }
    }

    private async Task HandleAuditoriumMessage(AuditoriumMessage message)
    {
        switch (message.Type)
        {
            case "participant_join":
                var participant = JsonSerializer.Deserialize<ParticipantInfo>(message.Data);
                if (participant != null)
                {
                    AddParticipant(participant);
                }
                break;

            case "participant_leave":
                RemoveParticipant(message.SenderId);
                break;

            case "speaking_status":
                UpdateSpeakingStatus(message.SenderId, bool.Parse(message.Data));
                break;

            case "screen_share_start":
                // Handle screen share start
                break;

            case "screen_share_stop":
                // Handle screen share stop
                break;
        }

        await Task.CompletedTask;
    }

    public async Task<string> CreateSessionAsync(string sessionName)
    {
        _currentSessionId = Guid.NewGuid().ToString();
        _isSessionActive = true;
        _sessionStartTime = DateTime.Now;
        SessionName = sessionName;

        // Add self as first participant and presenter
        var self = new ParticipantInfo
        {
            ParticipantId = Preferences.Get("peer_id", "unknown"),
            Name = Preferences.Get("username", "You"),
            Avatar = Preferences.Get("user_avatar", "👤"),
            IsMicOn = false,
            IsSpeaking = false,
            IsPresenter = true,
            IsScreenSharing = false
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _participants.Add(self);
        });

        SessionStarted?.Invoke(this, sessionName);

        // Broadcast session creation
        await BroadcastSessionMessage("session_create", sessionName);

        System.Diagnostics.Debug.WriteLine($"AuditoriumService: Session '{sessionName}' created");
        return _currentSessionId;
    }

    public async Task JoinSessionAsync(string sessionId)
    {
        _currentSessionId = sessionId;
        _isSessionActive = true;
        _sessionStartTime = DateTime.Now;

        var self = new ParticipantInfo
        {
            ParticipantId = Preferences.Get("peer_id", "unknown"),
            Name = Preferences.Get("username", "You"),
            Avatar = Preferences.Get("user_avatar", "👤"),
            IsMicOn = false,
            IsSpeaking = false,
            IsPresenter = false,
            IsScreenSharing = false
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _participants.Add(self);
        });

        // Broadcast join
        await BroadcastParticipantMessage("participant_join", self);

        System.Diagnostics.Debug.WriteLine($"AuditoriumService: Joined session {sessionId}");
    }

    public async Task LeaveSessionAsync()
    {
        if (_isScreenSharing)
        {
            await StopScreenShareAsync();
        }

        // Broadcast leave
        await BroadcastSessionMessage("participant_leave", _currentSessionId);

        _isSessionActive = false;
        _currentSessionId = null;
        SessionName = null;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _participants.Clear();
        });

        SessionEnded?.Invoke(this, EventArgs.Empty);

        System.Diagnostics.Debug.WriteLine("AuditoriumService: Left session");
    }

    public async Task StartScreenShareAsync()
    {
        if (!_isSessionActive)
        {
            throw new InvalidOperationException("No active session");
        }

        _isScreenSharing = true;

        // Set self as presenter and screen sharing
        var self = _participants.FirstOrDefault(p => p.ParticipantId == Preferences.Get("peer_id", "unknown"));
        if (self != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                self.IsPresenter = true;
                self.IsScreenSharing = true;
            });
        }

        ScreenShareStarted?.Invoke(this, EventArgs.Empty);

        // Broadcast screen share start
        await BroadcastSessionMessage("screen_share_start", "");

        System.Diagnostics.Debug.WriteLine("AuditoriumService: Screen share started");

        // TODO: Implement actual screen capture
        await Task.CompletedTask;
    }

    public async Task StopScreenShareAsync()
    {
        _isScreenSharing = false;

        var self = _participants.FirstOrDefault(p => p.ParticipantId == Preferences.Get("peer_id", "unknown"));
        if (self != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                self.IsScreenSharing = false;
            });
        }

        ScreenShareStopped?.Invoke(this, EventArgs.Empty);

        // Broadcast screen share stop
        await BroadcastSessionMessage("screen_share_stop", "");

        System.Diagnostics.Debug.WriteLine("AuditoriumService: Screen share stopped");
    }

    public async Task ToggleMicrophoneAsync(bool isOn)
    {
        var self = _participants.FirstOrDefault(p => p.ParticipantId == Preferences.Get("peer_id", "unknown"));
        if (self != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                self.IsMicOn = isOn;
            });

            if (isOn)
            {
                await _voiceService.StartTransmittingAsync();
            }
            else
            {
                _voiceService.StopTransmitting();
            }

            // Broadcast mic status
            await BroadcastSessionMessage("mic_status", isOn.ToString());
        }
    }

    public void UpdateSpeakingStatus(string participantId, bool isSpeaking)
    {
        var participant = _participants.FirstOrDefault(p => p.ParticipantId == participantId);
        if (participant != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                participant.IsSpeaking = isSpeaking;
            });

            ParticipantSpeaking?.Invoke(this, participant);
        }
    }

    public TimeSpan GetSessionDuration()
    {
        if (!_isSessionActive)
            return TimeSpan.Zero;

        return DateTime.Now - _sessionStartTime;
    }

    public void AddParticipant(ParticipantInfo participant)
    {
        if (!_participants.Any(p => p.ParticipantId == participant.ParticipantId))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _participants.Add(participant);
            });

            ParticipantJoined?.Invoke(this, participant);
            System.Diagnostics.Debug.WriteLine($"AuditoriumService: {participant.Name} joined");
        }
    }

    public void RemoveParticipant(string participantId)
    {
        var participant = _participants.FirstOrDefault(p => p.ParticipantId == participantId);
        if (participant != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _participants.Remove(participant);
            });

            ParticipantLeft?.Invoke(this, participant);
            System.Diagnostics.Debug.WriteLine($"AuditoriumService: {participant.Name} left");
        }
    }

    private async Task BroadcastSessionMessage(string messageType, string data)
    {
        try
        {
            var message = new AuditoriumMessage
            {
                Type = messageType,
                SenderId = Preferences.Get("peer_id", "unknown"),
                SessionId = _currentSessionId,
                Data = data,
                Timestamp = DateTime.Now
            };

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);

            var endpoint = new IPEndPoint(IPAddress.Broadcast, AUDITORIUM_PORT);
            await _udpClient.SendAsync(bytes, bytes.Length, endpoint);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AuditoriumService broadcast error: {ex.Message}");
        }
    }

    private async Task BroadcastParticipantMessage(string messageType, ParticipantInfo participant)
    {
        var participantJson = JsonSerializer.Serialize(participant);
        await BroadcastSessionMessage(messageType, participantJson);
    }

    public void Dispose()
    {
        _isListening = false;

        try
        {
            _udpClient?.Close();
            _udpClient?.Dispose();
        }
        catch { }

        System.Diagnostics.Debug.WriteLine("AuditoriumService: Disposed");
    }
}

// Helper class for auditorium messages
public class AuditoriumMessage
{
    public string Type { get; set; }
    public string SenderId { get; set; }
    public string SessionId { get; set; }
    public string Data { get; set; }
    public DateTime Timestamp { get; set; }
}