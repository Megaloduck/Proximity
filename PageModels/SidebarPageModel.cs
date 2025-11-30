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
                    _themeService.IsDarkMode = value;   // ThemeService applies the actual theme
                    OnPropertyChanged();                // Update UI binding
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


        public ICommand NavigateToUsersCommand { get; private set; }
        public ICommand NavigateToSettingsCommand { get; private set; }

        // -------------------------------
        // BUILD COMMANDS
        // -------------------------------
        private void BuildNavigationCommands()
        {
            // Main Menu
            NavigateToDashboardCommand = new Command(() => NavigateAction?.Invoke(new DashboardPage()));
            NavigateToDiscoverCommand = new Command(() => NavigateAction?.Invoke(new DiscoverPage()));

            // Tools
            NavigateToContactsCommand = new Command(() => NavigateAction?.Invoke(new ContactsPage()));
            NavigateToRoomsCommand = new Command(() => NavigateAction?.Invoke(new RoomsPage()));
            NavigateToAuditoriumCommand = new Command(() => NavigateAction?.Invoke(new AuditoriumPage()));

            // System
            NavigateToUsersCommand = new Command(() => NavigateAction?.Invoke(new UsersPage()));
            NavigateToSettingsCommand = new Command(() => NavigateAction?.Invoke(new SettingsPage()));
        }
    }
}