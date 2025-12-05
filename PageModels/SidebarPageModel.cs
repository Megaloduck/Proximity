using Proximity.Pages.MainMenu;
using Proximity.Pages.System;
using Proximity.Pages.Tools;
using Proximity.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Proximity.PageModels;

public class SidebarPageModel : INotifyPropertyChanged
{
    private bool _isDarkMode;
    public Action<Page> NavigateAction { get; set; }

    public SidebarPageModel()
    {
        // Load dark mode preference
        _isDarkMode = Preferences.Get("dark_mode", false);
        ApplyTheme();

        BuildNavigationCommands();
    }

    private void BuildNavigationCommands()
    {
        // Main Menu
        NavigateToDashboardCommand = new Command(() =>
        {
            try
            {
                var discoveryService = GetService<DiscoveryService>();
                var chatService = GetService<ChatService>();
                var voiceService = GetService<VoiceService>();

                if (discoveryService != null && chatService != null && voiceService != null)
                {
                    var page = new DashboardPage(discoveryService, chatService, voiceService);
                    NavigateAction?.Invoke(page);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SidebarPageModel: Services not available for Dashboard");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SidebarPageModel NavigateToDashboard error: {ex.Message}");
            }
        });

        NavigateToDiscoverCommand = new Command(() =>
        {
            try
            {
                var discoveryService = GetService<DiscoveryService>();
                var chatService = GetService<ChatService>();

                if (discoveryService != null && chatService != null)
                {
                    var page = new DiscoverPage(discoveryService, chatService);
                    NavigateAction?.Invoke(page);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SidebarPageModel: Services not available for Discover");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SidebarPageModel NavigateToDiscover error: {ex.Message}");
            }
        });

        // Features
        NavigateToContactsCommand = new Command(() =>
        {
            try
            {
                // Contacts page requires a peer selection
                // Navigate to Discover page instead where users can select a peer to chat with
                var discoveryService = GetService<DiscoveryService>();
                var chatService = GetService<ChatService>();

                if (discoveryService != null && chatService != null)
                {
                    var page = new DiscoverPage(discoveryService, chatService);
                    NavigateAction?.Invoke(page);

                    // Show a message to the user
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Application.Current?.MainPage?.DisplayAlert(
                            "Select a Peer",
                            "Please select a peer from the Discover page to start chatting.",
                            "OK"
                        );
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SidebarPageModel: Services not available for Contacts");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SidebarPageModel NavigateToContacts error: {ex.Message}");
            }
        });

        NavigateToRoomsCommand = new Command(() =>
        {
            try
            {
                var roomService = GetService<RoomService>();
                var discoveryService = GetService<DiscoveryService>();

                if (roomService != null && discoveryService != null)
                {
                    var page = new RoomsPage(roomService, discoveryService);
                    NavigateAction?.Invoke(page);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SidebarPageModel: Services not available for Rooms");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SidebarPageModel NavigateToRooms error: {ex.Message}");
            }
        });

        NavigateToBroadcastsCommand = new Command(() =>
        {
            try
            {
                var broadcastService = GetService<BroadcastService>();
                var discoveryService = GetService<DiscoveryService>();

                if (broadcastService != null && discoveryService != null)
                {
                    var page = new BroadcastsPage(broadcastService, discoveryService);
                    NavigateAction?.Invoke(page);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SidebarPageModel: Services not available for Broadcasts");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SidebarPageModel NavigateToBroadcasts error: {ex.Message}");
            }
        });

        NavigateToAuditoriumCommand = new Command(() =>
        {
            try
            {
                var auditoriumService = GetService<AuditoriumService>();

                if (auditoriumService != null)
                {
                    var page = new AuditoriumPage(auditoriumService);
                    NavigateAction?.Invoke(page);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SidebarPageModel: Services not available for Auditorium");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SidebarPageModel NavigateToAuditorium error: {ex.Message}");
            }
        });

        // System
        NavigateToProfileCommand = new Command(() =>
        {
            try
            {
                var discoveryService = GetService<DiscoveryService>();

                if (discoveryService != null)
                {
                    var page = new ProfilePage(discoveryService);
                    NavigateAction?.Invoke(page);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SidebarPageModel: Services not available for Profile");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SidebarPageModel NavigateToProfile error: {ex.Message}");
            }
        });

        NavigateToSettingsCommand = new Command(() =>
        {
            try
            {
                var discoveryService = GetService<DiscoveryService>();
                var voiceService = GetService<VoiceService>();

                if (discoveryService != null && voiceService != null)
                {
                    var page = new SettingsPage(discoveryService, voiceService);
                    NavigateAction?.Invoke(page);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SidebarPageModel: Services not available for Settings");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SidebarPageModel NavigateToSettings error: {ex.Message}");
            }
        });
    }

    private T GetService<T>() where T : class
    {
        try
        {
            return Application.Current?.Handler?.MauiContext?.Services?.GetService<T>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SidebarPageModel GetService<{typeof(T).Name}> error: {ex.Message}");
            return null;
        }
    }

    private void ApplyTheme()
    {
        Application.Current.UserAppTheme = _isDarkMode ? AppTheme.Dark : AppTheme.Light;
    }

    // Properties
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            _isDarkMode = value;
            OnPropertyChanged();
            Preferences.Set("dark_mode", value);
            ApplyTheme();
        }
    }

    // Commands
    public ICommand NavigateToDashboardCommand { get; private set; }
    public ICommand NavigateToDiscoverCommand { get; private set; }
    public ICommand NavigateToContactsCommand { get; private set; }
    public ICommand NavigateToRoomsCommand { get; private set; }
    public ICommand NavigateToBroadcastsCommand { get; private set; }
    public ICommand NavigateToAuditoriumCommand { get; private set; }
    public ICommand NavigateToProfileCommand { get; private set; }
    public ICommand NavigateToSettingsCommand { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}