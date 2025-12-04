using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Proximity.Models;

public class ParticipantInfo : INotifyPropertyChanged
{
    private string _participantId;
    private string _name;
    private string _avatar;
    private bool _isSpeaking;
    private bool _isMicOn;
    private bool _isPresenter;
    private bool _isScreenSharing;
    private DateTime _joinedAt;
    private string _ipAddress;

    public ParticipantInfo()
    {
        ParticipantId = Guid.NewGuid().ToString();
        JoinedAt = DateTime.Now;
        IsSpeaking = false;
        IsMicOn = false;
        IsPresenter = false;
        IsScreenSharing = false;
    }

    public string ParticipantId
    {
        get => _participantId;
        set { _participantId = value; OnPropertyChanged(); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string Avatar
    {
        get => _avatar;
        set { _avatar = value; OnPropertyChanged(); }
    }

    public bool IsSpeaking
    {
        get => _isSpeaking;
        set { _isSpeaking = value; OnPropertyChanged(); }
    }

    public bool IsMicOn
    {
        get => _isMicOn;
        set { _isMicOn = value; OnPropertyChanged(); }
    }

    public bool IsPresenter
    {
        get => _isPresenter;
        set { _isPresenter = value; OnPropertyChanged(); }
    }

    public bool IsScreenSharing
    {
        get => _isScreenSharing;
        set { _isScreenSharing = value; OnPropertyChanged(); }
    }

    public DateTime JoinedAt
    {
        get => _joinedAt;
        set { _joinedAt = value; OnPropertyChanged(); }
    }

    public string IpAddress
    {
        get => _ipAddress;
        set { _ipAddress = value; OnPropertyChanged(); }
    }

    // Computed property for display
    public string JoinedTimeAgo
    {
        get
        {
            var timeSpan = DateTime.Now - JoinedAt;
            if (timeSpan.TotalMinutes < 1)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            return $"{(int)timeSpan.TotalDays}d ago";
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}