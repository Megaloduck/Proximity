using System;
using System.Windows.Input;
using Proximity.Services;

namespace Proximity.PageModels
{
    public class SettingsPageModel : BasePageModel
    {
        private readonly DiscoveryService _discoveryService;
        private readonly VoiceService? _voiceService;
        private readonly ThemeService _themeService;

        private string _username = string.Empty;
        private bool _isDarkMode;
        private bool _isPushToTalk = true;
        private string _localPeerId = string.Empty;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    _themeService.IsDarkMode = value;
                }
            }
        }

        public bool IsPushToTalk
        {
            get => _isPushToTalk;
            set
            {
                if (SetProperty(ref _isPushToTalk, value) && _voiceService != null)
                {
                    _voiceService.IsPushToTalk = value;
                }
            }
        }

        public string LocalPeerId
        {
            get => _localPeerId;
            set => SetProperty(ref _localPeerId, value);
        }

        public ICommand SaveUsernameCommand { get; }

        public SettingsPageModel(DiscoveryService discoveryService, VoiceService? voiceService)
        {
            _discoveryService = discoveryService;
            _voiceService = voiceService;
            _themeService = ThemeService.Instance;

            SaveUsernameCommand = new Command(SaveUsername);

            LoadSettings();
        }

        private void LoadSettings()
        {
            Username = Preferences.Get("UserName", "User");
            IsDarkMode = _themeService.IsDarkMode;
            IsPushToTalk = _voiceService?.IsPushToTalk ?? true;
            LocalPeerId = _discoveryService.GetLocalId();
        }

        private async void SaveUsername()
        {
            Preferences.Set("UserName", Username);

            await Application.Current!.MainPage!.DisplayAlert(
                "Success",
                "Username saved! Restart the app for changes to take full effect.",
                "OK"
            );
        }
    }
}