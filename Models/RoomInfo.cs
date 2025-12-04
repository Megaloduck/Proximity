using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Proximity.Models;

public class RoomInfo : INotifyPropertyChanged
{
    private string _roomId;
    private string _roomName;
    private string _description;
    private string _roomIcon;
    private string _createdBy;
    private int _memberCount;
    private bool _hasVoice;
    private bool _hasWhiteboard;
    private bool _isPrivate;
    private bool _isJoined;
    private DateTime _createdAt;
    private List<string> _members;

    public string RoomId
    {
        get => _roomId;
        set { _roomId = value; OnPropertyChanged(); }
    }

    public string RoomName
    {
        get => _roomName;
        set { _roomName = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public string RoomIcon
    {
        get => _roomIcon;
        set { _roomIcon = value; OnPropertyChanged(); }
    }

    public string CreatedBy
    {
        get => _createdBy;
        set { _createdBy = value; OnPropertyChanged(); }
    }

    public int MemberCount
    {
        get => _memberCount;
        set { _memberCount = value; OnPropertyChanged(); }
    }

    public bool HasVoice
    {
        get => _hasVoice;
        set { _hasVoice = value; OnPropertyChanged(); }
    }

    public bool HasWhiteboard
    {
        get => _hasWhiteboard;
        set { _hasWhiteboard = value; OnPropertyChanged(); }
    }

    public bool IsPrivate
    {
        get => _isPrivate;
        set { _isPrivate = value; OnPropertyChanged(); }
    }

    public bool IsJoined
    {
        get => _isJoined;
        set { _isJoined = value; OnPropertyChanged(); }
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set { _createdAt = value; OnPropertyChanged(); }
    }

    public List<string> Members
    {
        get => _members ??= new List<string>();
        set { _members = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}