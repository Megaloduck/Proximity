using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Proximity.Models;

public class ChatMessage : INotifyPropertyChanged
{
    private string _messageId;
    private string _senderId;
    private string _senderName;
    private string _receiverId;
    private string _receiverName;
    private string _content;
    private DateTime _timestamp;
    private bool _isSentByMe;
    private bool _isDelivered;
    private string _deliveryStatus;

    public ChatMessage()
    {
        MessageId = Guid.NewGuid().ToString();
        Timestamp = DateTime.Now;
        IsDelivered = false;
        UpdateDeliveryStatus();
    }

    public string MessageId
    {
        get => _messageId;
        set { _messageId = value; OnPropertyChanged(); }
    }

    public string SenderId
    {
        get => _senderId;
        set { _senderId = value; OnPropertyChanged(); }
    }

    public string SenderName
    {
        get => _senderName;
        set { _senderName = value; OnPropertyChanged(); }
    }

    public string ReceiverId
    {
        get => _receiverId;
        set { _receiverId = value; OnPropertyChanged(); }
    }

    public string ReceiverName
    {
        get => _receiverName;
        set { _receiverName = value; OnPropertyChanged(); }
    }

    public string Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(); }
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

    public bool IsDelivered
    {
        get => _isDelivered;
        set
        {
            _isDelivered = value;
            OnPropertyChanged();
            UpdateDeliveryStatus();
        }
    }

    public string DeliveryStatus
    {
        get => _deliveryStatus;
        set { _deliveryStatus = value; OnPropertyChanged(); }
    }

    private void UpdateDeliveryStatus()
    {
        DeliveryStatus = IsDelivered ? "✓✓" : "✓";
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}