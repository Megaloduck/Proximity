using Proximity.PageModels;

namespace Proximity.Pages.Tools;

public partial class CreateRoomDialog : ContentPage
{
    private readonly RoomsPageModel _viewModel;

    public CreateRoomDialog(RoomsPageModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Set initial icon selection
        SetInitialIconSelection();
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

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        await _viewModel.ConfirmCreateRoom();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}