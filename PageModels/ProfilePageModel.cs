using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Proximity.Services;

namespace Proximity.PageModels
{
    public class ProfilePageModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly DiscoveryService _discoveryService;

        // Profile Properties with proper change notification
        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value && (value == null || value.Length <= 30))
                {
                    _displayName = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayNameLength));
                }
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value && (value == null || value.Length <= 60))
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusMessageLength));
                }
            }
        }

        private string _selectedEmoji = "😀";
        public string SelectedEmoji
        {
            get => _selectedEmoji;
            set
            {
                _selectedEmoji = value;
                OnPropertyChanged();
            }
        }

        // Computed properties for character counts
        public int DisplayNameLength => DisplayName?.Length ?? 0;
        public int StatusMessageLength => StatusMessage?.Length ?? 0;

        // User Info
        public string LocalPeerId => _discoveryService?.MyDeviceId ?? "Unknown";
        public string AccountCreated => Preferences.Get("AccountCreatedDate", DateTime.Now.ToString("yyyy-MM-dd"));

        // Commands
        public ICommand SelectEmojiCommand { get; }
        public ICommand SaveProfileCommand { get; }

        public ProfilePageModel(DiscoveryService discoveryService)
        {
            _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));

            // Load saved profile
            LoadProfile();

            // Initialize commands
            SelectEmojiCommand = new Command<string>((emoji) =>
            {
                SelectedEmoji = emoji;
                System.Diagnostics.Debug.WriteLine($"ProfilePageModel: Emoji selected: {emoji}");
            });

            SaveProfileCommand = new Command(async () => await SaveProfileAsync());
        }

        private void LoadProfile()
        {
            DisplayName = Preferences.Get("ProfileDisplayName", _discoveryService?.MyDeviceName ?? "User");
            StatusMessage = Preferences.Get("ProfileStatusMessage", "Available");
            SelectedEmoji = Preferences.Get("ProfileEmoji", "😀");

            // Ensure account created date exists
            if (!Preferences.ContainsKey("AccountCreatedDate"))
            {
                Preferences.Set("AccountCreatedDate", DateTime.Now.ToString("yyyy-MM-dd"));
            }

            System.Diagnostics.Debug.WriteLine($"ProfilePageModel: Loaded profile - Name: {DisplayName}, Emoji: {SelectedEmoji}");
        }

        private async Task SaveProfileAsync()
        {
            try
            {
                // Validate
                if (string.IsNullOrWhiteSpace(DisplayName))
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Validation Error",
                        "Display name cannot be empty",
                        "OK");
                    return;
                }

                // Save to preferences
                Preferences.Set("ProfileDisplayName", DisplayName);
                Preferences.Set("ProfileStatusMessage", StatusMessage ?? "");
                Preferences.Set("ProfileEmoji", SelectedEmoji);

                // Update discovery service name to broadcast new name
                if (_discoveryService != null)
                {
                    _discoveryService.LocalName = DisplayName;
                    // Update preferences that DiscoveryService uses
                    Preferences.Set("username", DisplayName);
                    Preferences.Set("user_avatar", SelectedEmoji);
                    Preferences.Set("user_status", StatusMessage ?? "");
                }

                System.Diagnostics.Debug.WriteLine($"ProfilePageModel: Saved profile - Name: {DisplayName}, Emoji: {SelectedEmoji}");

                await Application.Current.MainPage.DisplayAlert(
                    "Success",
                    "Profile saved successfully! Other users will now see your updated information.",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProfilePageModel: Save error - {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "Save Error",
                    ex.Message,
                    "OK");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            System.Diagnostics.Debug.WriteLine($"ProfilePageModel: Property changed - {name}");
        }
    }
}