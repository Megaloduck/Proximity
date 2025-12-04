using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Proximity.Models;

public class BroadcastMessage : INotifyPropertyChanged
{
    private string _messageId;
    private string _senderName;
    private string _senderAvatar;
    private string _senderId;
    private string _message;
    private DateTime _timestamp;
    private bool _isSentByMe;
    private BroadcastPriority _priority;
    private int _deliveredCount;
    private bool _isUrgent;

    public BroadcastMessage()
    {
        MessageId = Guid.NewGuid().ToString();
        Timestamp = DateTime.Now;
        Priority = BroadcastPriority.Normal;
        IsSentByMe = false;
        DeliveredCount = 0;
    }

    public string MessageId
    {
        get => _messageId;
        set { _messageId = value; OnPropertyChanged(); }
    }

    public string SenderName
    {
        get => _senderName;
        set { _senderName = value; OnPropertyChanged(); }
    }

    public string SenderAvatar
    {
        get => _senderAvatar;
        set { _senderAvatar = value; OnPropertyChanged(); }
    }

    public string SenderId
    {
        get => _senderId;
        set { _senderId = value; OnPropertyChanged(); }
    }

    public string Message
    {
        get => _message;
        set { _message = value; OnPropertyChanged(); }
    }

    public DateTime Timestamp
    {
        get => _timestamp;
        set { _timestamp = value; OnPropertyChanged(); }
    }

    public bool IsSentByMe
    {
        get => _isSentByMe;
        set { _isSentByMe = value; OnPropertyChanged(); }
    }

    public BroadcastPriority Priority
    {
        get => _priority;
        set
        {
            _priority = value;
            OnPropertyChanged();
            IsUrgent = (value == BroadcastPriority.High);
        }
    }

    public int DeliveredCount
    {
        get => _deliveredCount;
        set { _deliveredCount = value; OnPropertyChanged(); }
    }

    public bool IsUrgent
    {
        get => _isUrgent;
        set { _isUrgent = value; OnPropertyChanged(); }
    }

    // Computed properties for display
    public string TimeAgo
    {
        get
        {
            var timeSpan = DateTime.Now - Timestamp;
            if (timeSpan.TotalSeconds < 60)
                return "just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            return $"{(int)timeSpan.TotalDays}d ago";
        }
    }

    public string PriorityText
    {
        get
        {
            return Priority switch
            {
                BroadcastPriority.High => "🔴 High Priority",
                BroadcastPriority.Normal => "Normal",
                _ => "Normal"
            };
        }
    }

    public string DeliveryStatusText
    {
        get
        {
            if (IsSentByMe)
            {
                return $"Delivered to {DeliveredCount} peer{(DeliveredCount != 1 ? "s" : "")}";
            }
            return string.Empty;
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum BroadcastPriority
{
    Normal = 0,
    High = 1
}