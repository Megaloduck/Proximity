using Proximity.Models;
using Proximity.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace Proximity.PageModels;

public class RoomsPageModel : INotifyPropertyChanged
{
    private readonly RoomService _roomService;
    private readonly DiscoveryService _discoveryService;
    private string _selectedTab = "Available";
    private ObservableCollection<RoomInfo> _displayedRooms;

    // Room creation properties
    private string _newRoomName = string.Empty;
    private string _newRoomDescription = string.Empty;
    private string _newRoomIcon = "🏠";
    private bool _newRoomHasVoice = true;
    private bool _newRoomHasWhiteboard = true;
    private bool _newRoomIsPrivate = false;

    public RoomsPageModel(RoomService roomService, DiscoveryService discoveryService)
    {
        _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _displayedRooms = new ObservableCollection<RoomInfo>(_roomService.AvailableRooms);

        InitializeCommands();
    }

    private void InitializeCommands()
    {
        CreateRoomCommand = new Command(async () => await CreateRoom());
        JoinRoomCommand = new Command<RoomInfo>(async (room) => await JoinRoom(room));
        LeaveRoomCommand = new Command<RoomInfo>(async (room) => await LeaveRoom(room));
        OpenRoomCommand = new Command<RoomInfo>(async (room) => await OpenRoom(room));
        SelectTabCommand = new Command<string>((tab) => SelectTab(tab));
    }

    private void SelectTab(string tab)
    {
        SelectedTab = tab;

        if (tab == "Available")
        {
            DisplayedRooms = new ObservableCollection<RoomInfo>(_roomService.AvailableRooms);
        }
        else if (tab == "MyRooms")
        {
            DisplayedRooms = new ObservableCollection<RoomInfo>(_roomService.MyRooms);
        }

        System.Diagnostics.Debug.WriteLine($"RoomsPageModel: Selected tab '{tab}', showing {DisplayedRooms.Count} rooms");
    }

    private async Task CreateRoom()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("RoomsPageModel: CreateRoom called");

            // Reset form values
            NewRoomName = string.Empty;
            NewRoomDescription = string.Empty;
            NewRoomIcon = "🏠";
            NewRoomHasVoice = true;
            NewRoomHasWhiteboard = true;
            NewRoomIsPrivate = false;

            // Show create room dialog (implemented in code-behind)
            var createRoomPage = new Pages.Tools.CreateRoomDialog(this);
            await Application.Current.MainPage.Navigation.PushModalAsync(createRoomPage);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RoomsPageModel: Create room error: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to open create room dialog: {ex.Message}", "OK");
        }
    }

    public async Task ConfirmCreateRoom()
    {
        try
        {
            // Validate
            if (string.IsNullOrWhiteSpace(NewRoomName))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Validation Error",
                    "Room name cannot be empty",
                    "OK");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"RoomsPageModel: Creating room '{NewRoomName}'");

            var room = await _roomService.CreateRoomAsync(
                NewRoomName,
                NewRoomDescription ?? "No description",
                NewRoomIcon,
                hasVoice: NewRoomHasVoice,
                hasWhiteboard: NewRoomHasWhiteboard,
                isPrivate: NewRoomIsPrivate
            );

            System.Diagnostics.Debug.WriteLine($"RoomsPageModel: Room created successfully - {room.RoomName}");

            // Refresh the current tab
            SelectTab(SelectedTab);

            // Close the dialog
            await Application.Current.MainPage.Navigation.PopModalAsync();

            await Application.Current.MainPage.DisplayAlert(
                "Success",
                $"Room '{NewRoomName}' created successfully!",
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RoomsPageModel: Create room error: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to create room: {ex.Message}", "OK");
        }
    }

    private async Task JoinRoom(RoomInfo room)
    {
        if (room == null) return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"RoomsPageModel: Joining room '{room.RoomName}'");
            await _roomService.JoinRoomAsync(room);
            SelectTab(SelectedTab); // Refresh view

            await Application.Current.MainPage.DisplayAlert(
                "Success",
                $"Joined room '{room.RoomName}'",
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RoomsPageModel: Join room error: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to join room: {ex.Message}", "OK");
        }
    }

    private async Task LeaveRoom(RoomInfo room)
    {
        if (room == null) return;

        try
        {
            System.Diagnostics.Debug.WriteLine($"RoomsPageModel: Leaving room '{room.RoomName}'");
            await _roomService.LeaveRoomAsync(room);
            SelectTab(SelectedTab); // Refresh view

            await Application.Current.MainPage.DisplayAlert(
                "Success",
                $"Left room '{room.RoomName}'",
                "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RoomsPageModel: Leave room error: {ex.Message}");
            await Application.Current.MainPage.DisplayAlert("Error", $"Failed to leave room: {ex.Message}", "OK");
        }
    }

    private async Task OpenRoom(RoomInfo room)
    {
        if (room == null || !room.IsJoined) return;

        System.Diagnostics.Debug.WriteLine($"RoomsPageModel: Opening room '{room.RoomName}'");
        // Navigate to room chat page
        // TODO: Implement room chat navigation
        await Application.Current.MainPage.DisplayAlert(
            "Room",
            $"Opening room '{room.RoomName}'...\n\nRoom chat page coming soon!",
            "OK");
    }

    // Properties
    public string SelectedTab
    {
        get => _selectedTab;
        set { _selectedTab = value; OnPropertyChanged(); }
    }

    public ObservableCollection<RoomInfo> DisplayedRooms
    {
        get => _displayedRooms;
        set { _displayedRooms = value; OnPropertyChanged(); }
    }

    // Room creation properties
    public string NewRoomName
    {
        get => _newRoomName;
        set { _newRoomName = value; OnPropertyChanged(); }
    }

    public string NewRoomDescription
    {
        get => _newRoomDescription;
        set { _newRoomDescription = value; OnPropertyChanged(); }
    }

    public string NewRoomIcon
    {
        get => _newRoomIcon;
        set { _newRoomIcon = value; OnPropertyChanged(); }
    }

    public bool NewRoomHasVoice
    {
        get => _newRoomHasVoice;
        set { _newRoomHasVoice = value; OnPropertyChanged(); }
    }

    public bool NewRoomHasWhiteboard
    {
        get => _newRoomHasWhiteboard;
        set { _newRoomHasWhiteboard = value; OnPropertyChanged(); }
    }

    public bool NewRoomIsPrivate
    {
        get => _newRoomIsPrivate;
        set { _newRoomIsPrivate = value; OnPropertyChanged(); }
    }

    // Commands
    public ICommand CreateRoomCommand { get; private set; }
    public ICommand JoinRoomCommand { get; private set; }
    public ICommand LeaveRoomCommand { get; private set; }
    public ICommand OpenRoomCommand { get; private set; }
    public ICommand SelectTabCommand { get; private set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}