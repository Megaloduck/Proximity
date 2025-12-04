using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Proximity.Models;

public class PeerInfo : INotifyPropertyChanged
{
    private string _peerId;
    private string _name;
    private string _ipAddress;
    private int _port;
    private bool _isOnline;
    private bool _isConnected;
    private DateTime _lastSeen;
    private string _avatar;
    private string _statusMessage;
    private long _ping;
    private bool _hasPing;

    public string PeerId
    {
        get => _peerId;
        set { _peerId = value; OnPropertyChanged(); }
    }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string IpAddress
    {
        get => _ipAddress;
        set { _ipAddress = value; OnPropertyChanged(); }
    }

    public int Port
    {
        get => _port;
        set { _port = value; OnPropertyChanged(); }
    }

    public bool IsOnline
    {
        get => _isOnline;
        set { _isOnline = value; OnPropertyChanged(); }
    }

    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); }
    }

    public DateTime LastSeen
    {
        get => _lastSeen;
        set { _lastSeen = value; OnPropertyChanged(); }
    }

    public string Avatar
    {
        get => _avatar;
        set { _avatar = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    // New Ping Properties
    public long Ping
    {
        get => _ping;
        set { _ping = value; OnPropertyChanged(); }
    }

    public bool HasPing
    {
        get => _hasPing;
        set { _hasPing = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}