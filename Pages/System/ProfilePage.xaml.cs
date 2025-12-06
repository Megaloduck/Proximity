using Proximity.PageModels;
using Proximity.Services;
using SystemDiagnostics = System.Diagnostics;

namespace Proximity.Pages.System;

public partial class ProfilePage : ContentPage
{
    private ProfilePageModel _viewModel;

    public ProfilePage(DiscoveryService discoveryService)
    {
        InitializeComponent();
        _viewModel = new ProfilePageModel(discoveryService);
        BindingContext = _viewModel;

        // Subscribe to preview update events
        _viewModel.PreviewUpdateRequested += OnPreviewUpdateRequested;

        // Force initial preview update
        UpdatePreview();
    }

    public ProfilePage()
    {
        InitializeComponent();
    }

    private void OnPreviewUpdateRequested(object sender, EventArgs e)
    {
        // Update preview whenever the model requests it
        UpdatePreview();
    }

    // Event handler for TextChanged events
    private void OnProfileChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (_viewModel == null) return;

        // Update preview with proper placeholders when empty
        PreviewName.Text = string.IsNullOrWhiteSpace(_viewModel.DisplayName)
            ? "Your Name"
            : _viewModel.DisplayName;

        PreviewStatus.Text = string.IsNullOrWhiteSpace(_viewModel.StatusMessage)
            ? "Your status message..."
            : _viewModel.StatusMessage;

        PreviewEmoji.Text = _viewModel.SelectedEmoji ?? "😀";

        // Make placeholder text look different (lighter)
        PreviewName.Opacity = string.IsNullOrWhiteSpace(_viewModel.DisplayName) ? 0.5 : 1.0;
        PreviewStatus.Opacity = string.IsNullOrWhiteSpace(_viewModel.StatusMessage) ? 0.5 : 1.0;

        SystemDiagnostics.Debug.WriteLine($"ProfilePage: Preview updated - Name='{PreviewName.Text}', Status='{PreviewStatus.Text}', Emoji='{PreviewEmoji.Text}'");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe to prevent memory leaks
        if (_viewModel != null)
        {
            _viewModel.PreviewUpdateRequested -= OnPreviewUpdateRequested;
        }
    }
}