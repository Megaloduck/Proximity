using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Windows.Input;
using Proximity.Services;

namespace Proximity.PageModels
{
    public class ProfilePageModel : BasePageModel
    {
        private readonly DiscoveryService _discoveryService;

        private string _displayName = string.Empty;
        private string _statusMessage = string.Empty;
        private string _selectedEmoji = "😀";
        private string _localPeerId = string.Empty;
        private string _accountCreated = string.Empty;

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SelectedEmoji
        {
            get => _selectedEmoji;
            set => SetProperty(ref _selectedEmoji, value);
        }

        public string LocalPeerId
        {
            get => _localPeerId;
            set => SetProperty(ref _localPeerId, value);
        }

        public string AccountCreated
        {
            get => _accountCreated;
            set => SetProperty(ref _accountCreated, value);
        }

        public ICommand SelectEmojiCommand { get; }
        public ICommand SaveProfileCommand { get; }

        public ProfilePageModel(DiscoveryService discoveryService)
        {
            _discoveryService = discoveryService;

            SelectEmojiCommand = new Command<string>(SelectEmoji);
            SaveProfileCommand = new Command(SaveProfile);

            LoadProfile();
        }

        private void LoadProfile()
        {
            DisplayName = Preferences.Get("ProfileDisplayName", Preferences.Get("UserName", "User"));
            StatusMessage = Preferences.Get("ProfileStatus", "Available to chat");
            SelectedEmoji = Preferences.Get("ProfileEmoji", "😀");
            LocalPeerId = _discoveryService.GetLocalId();

            // Get or set account creation date
            var createdDate = Preferences.Get("AccountCreatedDate", "");
            if (string.IsNullOrEmpty(createdDate))
            {
                createdDate = DateTime.Now.ToString("yyyy-MM-dd");
                Preferences.Set("AccountCreatedDate", createdDate);
            }
            AccountCreated = DateTime.Parse(createdDate).ToString("MMMM dd, yyyy");
        }

        private void SelectEmoji(string emoji)
        {
            SelectedEmoji = emoji;
        }

        private async void SaveProfile()
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                await Application.Current!.MainPage!.DisplayAlert(
                    "Invalid Name",
                    "Please enter a display name",
                    "OK"
                );
                return;
            }

            Preferences.Set("ProfileDisplayName", DisplayName);
            Preferences.Set("ProfileStatus", StatusMessage);
            Preferences.Set("ProfileEmoji", SelectedEmoji);

            await Application.Current!.MainPage!.DisplayAlert(
                "Success",
                "Profile saved! Other users will see your updated card when they discover you.",
                "OK"
            );
        }
    }
}
