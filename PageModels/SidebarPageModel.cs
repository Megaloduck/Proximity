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
        public ICommand NavigateToOverviewCommand { get; private set; }

        public ICommand NavigateToTools1Command { get; private set; }
        public ICommand NavigateToTools2Command { get; private set; }
        public ICommand NavigateToTools3Command { get; private set; }
        public ICommand NavigateToTools4Command { get; private set; }
        public ICommand NavigateToTools5Command { get; private set; }
        public ICommand NavigateToTools6Command { get; private set; }
        public ICommand NavigateToTools7Command { get; private set; }

        public ICommand NavigateToUsersCommand { get; private set; }
        public ICommand NavigateToSettingsCommand { get; private set; }

        // -------------------------------
        // BUILD COMMANDS
        // -------------------------------
        private void BuildNavigationCommands()
        {
            // Main Menu
            NavigateToDashboardCommand = new Command(() => NavigateAction?.Invoke(new DashboardPage()));
            NavigateToOverviewCommand = new Command(() => NavigateAction?.Invoke(new OverviewPage()));

            // Tools
            NavigateToTools1Command = new Command(() => NavigateAction?.Invoke(new Tools1Page()));
            NavigateToTools2Command = new Command(() => NavigateAction?.Invoke(new Tools2Page()));
            NavigateToTools3Command = new Command(() => NavigateAction?.Invoke(new Tools3Page()));
            NavigateToTools4Command = new Command(() => NavigateAction?.Invoke(new Tools4Page()));
            NavigateToTools5Command = new Command(() => NavigateAction?.Invoke(new Tools5Page()));
            NavigateToTools6Command = new Command(() => NavigateAction?.Invoke(new Tools6Page()));
            NavigateToTools7Command = new Command(() => NavigateAction?.Invoke(new Tools7Page()));

            // System
            NavigateToUsersCommand = new Command(() => NavigateAction?.Invoke(new UsersPage()));
            NavigateToSettingsCommand = new Command(() => NavigateAction?.Invoke(new SettingsPage()));
        }
    }
}