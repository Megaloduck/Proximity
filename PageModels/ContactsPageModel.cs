using Microsoft.Maui.Controls;
using Proximity.Models;
using Proximity.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Proximity.PageModels
{
    public class ChatMessageViewModel
    {
        public string SenderName { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsFromMe { get; set; }
    }

    public class ContactsPageModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly PeerInfo _peer;
        private readonly ChatService _chatService;
        private readonly VoiceService _voiceService;

        // Properties
        public string PeerName => _peer?.DeviceName ?? "Unknown";

        private string _messageText;
        public string MessageText
        {
            get => _messageText;
            set { _messageText = value; OnPropertyChanged(); }
        }

        private bool _isVoiceActive = false;
        public bool IsVoiceActive
        {
            get => _isVoiceActive;
            set { _isVoiceActive = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ChatMessageViewModel> Messages { get; }

        // Commands
        public ICommand SendCommand { get; }
        public ICommand ToggleVoiceCommand { get; }

        public ContactsPageModel(PeerInfo peer, ChatService chatService)
        {
            _peer = peer ?? throw new ArgumentNullException(nameof(peer));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _voiceService = new VoiceService();

            Messages = new ObservableCollection<ChatMessageViewModel>();

            // Subscribe to message events
            _chatService.OnMessageReceived += OnMessageReceived;

            // Subscribe to voice events
            _voiceService.OnCallStarted += () => IsVoiceActive = true;
            _voiceService.OnCallEnded += () => IsVoiceActive = false;

            // Initialize commands
            SendCommand = new Command(async () => await SendMessageAsync(), () => !string.IsNullOrWhiteSpace(MessageText));
            ToggleVoiceCommand = new Command(async () => await ToggleVoiceAsync());

            // Update send button state when message text changes
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MessageText))
                {
                    ((Command)SendCommand).ChangeCanExecute();
                }
            };
        }

        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(MessageText) || _peer == null)
                return;

            try
            {
                var success = await _chatService.SendMessageAsync(_peer, MessageText);

                if (success)
                {
                    // Add to local messages
                    Messages.Add(new ChatMessageViewModel
                    {
                        SenderName = "You",
                        Content = MessageText,
                        Timestamp = DateTime.Now,
                        IsFromMe = true
                    });

                    MessageText = string.Empty;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Send Failed",
                        "Could not send message. Check connection.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task ToggleVoiceAsync()
        {
            try
            {
                if (IsVoiceActive)
                {
                    await _voiceService.EndCallAsync();
                }
                else
                {
                    await _voiceService.StartCallAsync(_peer);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Voice Error", ex.Message, "OK");
            }
        }

        private void OnMessageReceived(ChatMessage message)
        {
            // Only show messages from this peer
            if (message.FromDeviceId == _peer.DeviceId)
            {
                Application.Current.MainPage.Dispatcher.Dispatch(() =>
                {
                    Messages.Add(new ChatMessageViewModel
                    {
                        SenderName = message.FromDeviceName,
                        Content = message.Text,
                        Timestamp = message.Timestamp,
                        IsFromMe = false
                    });
                });
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            _chatService.OnMessageReceived -= OnMessageReceived;
            _voiceService?.Dispose();
        }
    }
}