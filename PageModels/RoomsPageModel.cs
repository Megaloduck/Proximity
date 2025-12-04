using Proximity.Models;
using Proximity.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Proximity.PageModels;

public class RoomsPageModel : INotifyPropertyChanged
{
    private readonly RoomService _roomService;
    private readonly DiscoveryService _discoveryService;
    private string _selectedTab = "Available";
    private ObservableCollection<RoomInfo> _displayedRooms;

    public RoomsPageModel(RoomService roomService, DiscoveryService discoveryService)
    {
        _roomService = roomService;
        _discoveryService = discoveryService;
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
    }

    private async Task CreateRoom()
    {
        // In a real app, show a dialog to get room details
        var roomName = "New Room " + DateTime.Now.ToString("HHmm");
        var description = "Created via Proximity";
        var icon = "🏠";

        try
        {
            var room = await _roomService.CreateRoomAsync(
                roomName,
                description,
                icon,
                hasVoice: true,
                hasWhiteboard: true,
                isPrivate: false
            );

            // Refresh the current tab
            SelectTab(SelectedTab);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Create room error: {ex.Message}");
        }
    }

    private async Task JoinRoom(RoomInfo room)
    {
        if (room == null) return;

        try
        {
            await _roomService.JoinRoomAsync(room);
            SelectTab(SelectedTab); // Refresh view
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Join room error: {ex.Message}");
        }
    }

    private async Task LeaveRoom(RoomInfo room)
    {
        if (room == null) return;

        try
        {
            await _roomService.LeaveRoomAsync(room);
            SelectTab(SelectedTab); // Refresh view
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Leave room error: {ex.Message}");
        }
    }

    private async Task OpenRoom(RoomInfo room)
    {
        if (room == null || !room.IsJoined) return;

        // Navigate to room chat page
        // TODO: Implement room chat navigation
        await Task.CompletedTask;
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