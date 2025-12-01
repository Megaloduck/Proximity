using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Proximity.Models;
using Proximity.Services;

namespace Proximity.PageModels
{
    public class ContactsPageModel : BasePageModel
    {
        private readonly ChatService _chatService;
        private readonly VoiceService _voiceService;
        private readonly PeerInfo? _targetPeer;
        private string _messageText = string.Empty;
        private bool _isVoiceActive;

        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();

        public string MessageText
        {
            get => _messageText;
            set => SetProperty(ref _messageText, value);
        }

        public bool IsVoiceActive
        {
            get => _isVoiceActive;
            set => SetProperty(ref _isVoiceActive, value);
        }

        public string PeerName => _targetPeer?.Name ?? "Broadcast";

        public ICommand SendCommand { get; }
        public ICommand ToggleVoiceCommand { get; }

        public ContactsPageModel(PeerInfo? peer, ChatService chatService)
        {
            _targetPeer = peer;
            _chatService = chatService;

            // Get VoiceService from DI
            var app = Application.Current;
            var services = app?.Handler?.MauiContext?.Services;
            _voiceService = services?.GetService(typeof(VoiceService)) as VoiceService;

            SendCommand = new Command(SendMessage, () => !string.IsNullOrWhiteSpace(MessageText));
            ToggleVoiceCommand = new Command(ToggleVoice);

            _chatService.MessageReceived += OnMessageReceived;

            // Load message history
            LoadMessageHistory();
        }

        private void LoadMessageHistory()
        {
            var history = _chatService.GetMessageHistory(_targetPeer?.Id);
            foreach (var msg in history)
            {
                Messages.Add(msg);
            }
        }

        private void OnMessageReceived(ChatMessage message)
        {
            // Only show messages relevant to this chat
            bool isRelevant = _targetPeer == null || // broadcast chat
                             message.SenderId == _targetPeer.Id ||
                             (message.RecipientId == _targetPeer.Id);

            if (isRelevant)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Messages.Add(message);
                });
            }
        }

        private async void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(MessageText)) return;

            try
            {
                await _chatService.SendMessage(MessageText, _targetPeer?.Id);
                MessageText = string.Empty;
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Error",
                    $"Failed to send message: {ex.Message}",
                    "OK"
                );
            }
        }

        private void ToggleVoice()
        {
            if (_voiceService == null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current!.MainPage!.DisplayAlert(
                        "Voice Not Available",
                        "Voice service is not initialized. Please restart the app.",
                        "OK"
                    );
                });
                return;
            }

            IsVoiceActive = !IsVoiceActive;

            try
            {
                if (IsVoiceActive)
                {
                    // Start voice transmission
                    if (_voiceService.IsPushToTalk)
                    {
                        _voiceService.StartPushToTalk();
                    }
                    else
                    {
                        _voiceService.StartCapture();
                    }

                    System.Diagnostics.Debug.WriteLine("Voice chat started");
                }
                else
                {
                    // Stop voice transmission
                    if (_voiceService.IsPushToTalk)
                    {
                        _voiceService.StopPushToTalk();
                    }
                    else
                    {
                        _voiceService.StopCapture();
                    }

                    System.Diagnostics.Debug.WriteLine("Voice chat stopped");
                }
            }
            catch (Exception ex)
            {
                IsVoiceActive = false;
                System.Diagnostics.Debug.WriteLine($"Voice error: {ex.Message}");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Application.Current!.MainPage!.DisplayAlert(
                        "Voice Error",
                        $"Failed to toggle voice: {ex.Message}",
                        "OK"
                    );
                });
            }
        }
    }
}