using Proximity.PageModels;
using Proximity.Services;
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

    private void OnCreateRoomClicked(object sender, EventArgs e)
    {
        // Reset form
        _viewModel.NewRoomName = string.Empty;
        _viewModel.NewRoomDescription = string.Empty;
        _viewModel.NewRoomIcon = "🏠";
        _viewModel.NewRoomHasVoice = true;
        _viewModel.NewRoomHasWhiteboard = true;
        _viewModel.NewRoomIsPrivate = false;

        // Set initial icon selection
        SetInitialIconSelection();

        // Show dialog
        DialogOverlay.IsVisible = true;

        SystemDiagnostics.Debug.WriteLine("RoomsPage: Dialog shown");
    }

    private void SetInitialIconSelection()
    {
        foreach (var child in RoomIconSelector.Children)
        {
            if (child is RadioButton radio && radio.Value?.ToString() == _viewModel.NewRoomIcon)
            {
                radio.IsChecked = true;
                break;
            }
        }
    }

    private void OnRoomIconChecked(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radio)
        {
            var icon = radio.Value?.ToString();
            if (!string.IsNullOrEmpty(icon))
            {
                _viewModel.NewRoomIcon = icon;
            }
        }
    }

    private async void OnDialogCreateClicked(object sender, EventArgs e)
    {
        await _viewModel.ConfirmCreateRoom();
        DialogOverlay.IsVisible = false;
    }

    private void OnDialogCancelClicked(object sender, EventArgs e)
    {
        DialogOverlay.IsVisible = false;
    }
}