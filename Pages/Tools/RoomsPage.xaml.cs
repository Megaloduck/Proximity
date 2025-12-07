using Proximity.PageModels;
using Proximity.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SystemDiagnostics = System.Diagnostics;

namespace Proximity.Pages.Tools;

public partial class RoomsPage : ContentPage
{
    private RoomsPageModel _viewModel;

    public RoomsPage(RoomService roomService, DiscoveryService discoveryService)
    {
        InitializeComponent();
        _viewModel = new RoomsPageModel(roomService, discoveryService);
        BindingContext = _viewModel;
    }

    public RoomsPage()
    {
        InitializeComponent();
    }  

    private void OnRoomNameTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.NewRoomName = e.NewTextValue ?? string.Empty;
            SystemDiagnostics.Debug.WriteLine($"RoomsPage: Room name updated to '{_viewModel.NewRoomName}'");
        }
    }

    private void SetInitialIconSelection()
    {
        try
        {
            foreach (var child in RoomIconSelector.Children)
            {
                if (child is RadioButton radio && radio.Value?.ToString() == _viewModel.NewRoomIcon)
                {
                    radio.IsChecked = true;
                    SystemDiagnostics.Debug.WriteLine($"RoomsPage: Set initial icon to {_viewModel.NewRoomIcon}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            SystemDiagnostics.Debug.WriteLine($"RoomsPage: SetInitialIconSelection error: {ex.Message}");
        }
    }

    private void OnRoomIconChecked(object sender, CheckedChangedEventArgs e)
    {
        try
        {
            if (e.Value && sender is RadioButton radio)
            {
                var icon = radio.Value?.ToString();
                if (!string.IsNullOrEmpty(icon))
                {
                    _viewModel.NewRoomIcon = icon;
                    SystemDiagnostics.Debug.WriteLine($"RoomsPage: Icon changed to {icon}");
                }
            }
        }
        catch (Exception ex)
        {
            SystemDiagnostics.Debug.WriteLine($"RoomsPage: OnRoomIconChecked error: {ex.Message}");
        }
    }
    private void OnRoomDescriptionTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.NewRoomDescription = e.NewTextValue ?? string.Empty;
            SystemDiagnostics.Debug.WriteLine($"RoomsPage: Room description updated to '{_viewModel.NewRoomDescription}'");
        }
    }
    private void OnCreateRoomClicked(object sender, EventArgs e)
    {
        try
        {
            SystemDiagnostics.Debug.WriteLine("RoomsPage: OnCreateRoomClicked");

            // Reset form
            _viewModel.NewRoomName = string.Empty;
            _viewModel.NewRoomDescription = string.Empty;
            _viewModel.NewRoomIcon = "🏠";
            _viewModel.NewRoomHasVoice = true;
            _viewModel.NewRoomHasWhiteboard = true;
            _viewModel.NewRoomIsPrivate = false;

            // Clear the entry fields
            RoomNameEntry.Text = string.Empty;
            RoomDescriptionEditor.Text = string.Empty;

            // Set initial icon selection
            SetInitialIconSelection();

            // Subscribe to text changed events
            RoomNameEntry.TextChanged += OnRoomNameTextChanged;
            RoomDescriptionEditor.TextChanged += OnRoomDescriptionTextChanged;

            // Show dialog
            DialogOverlay.IsVisible = true;

            SystemDiagnostics.Debug.WriteLine("RoomsPage: Dialog shown");
        }
        catch (Exception ex)
        {
            SystemDiagnostics.Debug.WriteLine($"RoomsPage: OnCreateRoomClicked error: {ex.Message}");
        }
    }
    private async void OnDialogCreateClicked(object sender, EventArgs e)
    {
        try
        {
            SystemDiagnostics.Debug.WriteLine($"RoomsPage: OnDialogCreateClicked - Room name: '{_viewModel.NewRoomName}'");
            SystemDiagnostics.Debug.WriteLine($"RoomsPage: Description: '{_viewModel.NewRoomDescription}'");
            SystemDiagnostics.Debug.WriteLine($"RoomsPage: Icon: '{_viewModel.NewRoomIcon}'");

            // Call the ViewModel method
            await _viewModel.ConfirmCreateRoom();

            SystemDiagnostics.Debug.WriteLine("RoomsPage: Room creation completed, closing dialog");

            // Unsubscribe from events
            RoomNameEntry.TextChanged -= OnRoomNameTextChanged;
            RoomDescriptionEditor.TextChanged -= OnRoomDescriptionTextChanged;

            // Close dialog after successful creation
            DialogOverlay.IsVisible = false;

            SystemDiagnostics.Debug.WriteLine("RoomsPage: Dialog closed");
        }
        catch (Exception ex)
        {
            SystemDiagnostics.Debug.WriteLine($"RoomsPage: OnDialogCreateClicked error: {ex.Message}");

            // Unsubscribe from events
            RoomNameEntry.TextChanged -= OnRoomNameTextChanged;
            RoomDescriptionEditor.TextChanged -= OnRoomDescriptionTextChanged;

            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            DialogOverlay.IsVisible = false;
        }
    }

    private void OnDialogCancelClicked(object sender, EventArgs e)
    {
        try
        {
            SystemDiagnostics.Debug.WriteLine("RoomsPage: Dialog cancelled");

            // Unsubscribe from events
            RoomNameEntry.TextChanged -= OnRoomNameTextChanged;
            RoomDescriptionEditor.TextChanged -= OnRoomDescriptionTextChanged;

            DialogOverlay.IsVisible = false;
        }
        catch (Exception ex)
        {
            SystemDiagnostics.Debug.WriteLine($"RoomsPage: OnDialogCancelClicked error: {ex.Message}");
        }
    }
}