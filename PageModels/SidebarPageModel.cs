using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Proximity.Services;
using Proximity.Pages.MainMenu;
using Proximity.Pages.Tools;
using Proximity.Pages.System;

namespace Proximity.PageModels
{
    public class SidebarPageModel : BasePageModel
    {
        private readonly ThemeService _themeService;

        // Used by SidebarPage.xaml.cs to navigate content
        public Action<Page> NavigateAction { get; set; }

        public SidebarPageModel()
        {
            _themeService = ThemeService.Instance;

            BuildNavigationCommands();
            HookThemeUpdates();
        }

        // -------------------------------
        // THEME BINDING
        // -------------------------------
        public bool IsDarkMode
        {
            get => _themeService.IsDarkMode;
            set
            {
                if (_themeService.IsDarkMode != value)
                {
                    _themeService.IsDarkMode = value;
                    OnPropertyChanged();
                }
            }
        }

        private void HookThemeUpdates()
        {
            _themeService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ThemeService.IsDarkMode))
                    OnPropertyChanged(nameof(IsDarkMode));
            };
        }

        // -------------------------------
        // COMMANDS
        // -------------------------------
        public ICommand NavigateToDashboardCommand { get; private set; }
        public ICommand NavigateToDiscoverCommand { get; private set; }

        public ICommand NavigateToContactsCommand { get; private set; }
        public ICommand NavigateToRoomsCommand { get; private set; }
        public ICommand NavigateToAuditoriumCommand { get; private set; }

        public ICommand NavigateToProfileCommand { get; private set; }
        public ICommand NavigateToSettingsCommand { get; private set; }

        // -------------------------------
        // BUILD COMMANDS - FIXED TO USE DI
        // -------------------------------
        private void BuildNavigationCommands()
        {
            // Main Menu
            NavigateToDashboardCommand = new Command(() =>
            {
                var app = Application.Current as App;
                var services = app?.Handler?.MauiContext?.Services;

                if (services != null)
                {
                    var discoveryService = services.GetService(typeof(DiscoveryService)) as DiscoveryService;
                    var chatService = services.GetService(typeof(ChatService)) as ChatService;
                    var voiceService = services.GetService(typeof(VoiceService)) as VoiceService;

                    if (discoveryService != null && chatService != null && voiceService != null)
                    {
                        NavigateAction?.Invoke(new DashboardPage(discoveryService, chatService, voiceService));
                    }
                }
            });

            NavigateToDiscoverCommand = new Command(() =>
            {
                var app = Application.Current as App;
                var services = app?.Handler?.MauiContext?.Services;

                if (services != null)
                {
                    var discoveryService = services.GetService(typeof(DiscoveryService)) as DiscoveryService;
                    var chatService = services.GetService(typeof(ChatService)) as ChatService;

                    if (discoveryService != null && chatService != null)
                    {
                        NavigateAction?.Invoke(new DiscoverPage(discoveryService, chatService));
                    }
                }
            });

            // Tools
            NavigateToContactsCommand = new Command(() =>
            {
                var app = Application.Current as App;
                var services = app?.Handler?.MauiContext?.Services;

                if (services != null)
                {
                    var chatService = services.GetService(typeof(ChatService)) as ChatService;
                    if (chatService != null)
                    {
                        NavigateAction?.Invoke(new ContactsPage(null, chatService));
                    }
                }
            });

            NavigateToRoomsCommand = new Command(() => NavigateAction?.Invoke(new RoomsPage()));
            NavigateToAuditoriumCommand = new Command(() => NavigateAction?.Invoke(new AuditoriumPage()));

            // System
            NavigateToProfileCommand = new Command(() =>
            {
                var app = Application.Current as App;
                var services = app?.Handler?.MauiContext?.Services; if (services != null)
                {
                    var discoveryService = services.GetService(typeof(DiscoveryService)) as DiscoveryService;
                    if (discoveryService != null)
                    {
                        NavigateAction?.Invoke(new ProfilePage(discoveryService));
                    }
                }
            });

            NavigateToSettingsCommand = new Command(() =>
            {
                var app = Application.Current as App;
                var services = app?.Handler?.MauiContext?.Services;

                if (services != null)
                {
                    var discoveryService = services.GetService(typeof(DiscoveryService)) as DiscoveryService;
                    var voiceService = services.GetService(typeof(VoiceService)) as VoiceService;

                    if (discoveryService != null)
                    {
                        NavigateAction?.Invoke(new SettingsPage(discoveryService, voiceService));
                    }
                }
            });
        }
    }
}