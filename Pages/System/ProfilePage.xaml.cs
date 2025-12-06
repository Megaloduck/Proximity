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

        SystemDiagnostics.Debug.WriteLine("ProfilePage: Constructor - ViewModel created and bound");

        // Subscribe to preview update events
        _viewModel.PreviewUpdateRequested += OnPreviewUpdateRequested;

        // Subscribe to Entry text changes DIRECTLY
        DisplayNameEntry.TextChanged += OnDisplayNameTextChanged;
        StatusMessageEntry.TextChanged += OnStatusMessageTextChanged;

        // Force initial preview update
        UpdatePreview();

        // Set initial RadioButton selection
        SetInitialEmojiSelection();
    }

    public ProfilePage()
    {
        InitializeComponent();
    }

    private void SetInitialEmojiSelection()
    {
        if (_viewModel == null) return;

        foreach (var child in EmojiSelector.Children)
        {
            if (child is RadioButton radio && radio.Value?.ToString() == _viewModel.SelectedEmoji)
            {
                radio.IsChecked = true;
                SystemDiagnostics.Debug.WriteLine($"ProfilePage: Set initial emoji selection to {_viewModel.SelectedEmoji}");
                break;
            }
        }
    }

    private void OnEmojiChecked(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value && sender is RadioButton radio)
        {
            var emoji = radio.Value?.ToString();
            SystemDiagnostics.Debug.WriteLine($"ProfilePage: RadioButton checked - Emoji: {emoji}");

            if (_viewModel != null && !string.IsNullOrEmpty(emoji))
            {
                _viewModel.SelectedEmoji = emoji;
                SystemDiagnostics.Debug.WriteLine($"ProfilePage: Set ViewModel.SelectedEmoji to {emoji}");
            }
        }
    }

    private void OnDisplayNameTextChanged(object sender, TextChangedEventArgs e)
    {
        SystemDiagnostics.Debug.WriteLine($"ProfilePage: DisplayNameEntry.TextChanged - '{e.OldTextValue}' -> '{e.NewTextValue}'");

        // FORCE update the ViewModel
        if (_viewModel != null)
        {
            _viewModel.DisplayName = e.NewTextValue ?? string.Empty;
            SystemDiagnostics.Debug.WriteLine($"ProfilePage: FORCED ViewModel.DisplayName = '{_viewModel.DisplayName}'");
        }

        UpdatePreview();
    }

    private void OnStatusMessageTextChanged(object sender, TextChangedEventArgs e)
    {
        SystemDiagnostics.Debug.WriteLine($"ProfilePage: StatusMessageEntry.TextChanged - '{e.OldTextValue}' -> '{e.NewTextValue}'");

        // FORCE update the ViewModel
        if (_viewModel != null)
        {
            _viewModel.StatusMessage = e.NewTextValue ?? string.Empty;
            SystemDiagnostics.Debug.WriteLine($"ProfilePage: FORCED ViewModel.StatusMessage = '{_viewModel.StatusMessage}'");
        }

        UpdatePreview();
    }

    private void OnPreviewUpdateRequested(object sender, EventArgs e)
    {
        SystemDiagnostics.Debug.WriteLine("ProfilePage: OnPreviewUpdateRequested event fired");
        UpdatePreview();
    }

    private void OnProfileChanged(object sender, TextChangedEventArgs e)
    {
        SystemDiagnostics.Debug.WriteLine($"ProfilePage: OnProfileChanged - Old: '{e.OldTextValue}', New: '{e.NewTextValue}'");
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (_viewModel == null)
        {
            SystemDiagnostics.Debug.WriteLine("ProfilePage: UpdatePreview - ViewModel is NULL!");
            return;
        }

        SystemDiagnostics.Debug.WriteLine("=== UpdatePreview START ===");
        SystemDiagnostics.Debug.WriteLine($"  ViewModel.DisplayName: '{_viewModel.DisplayName}' (Length: {_viewModel.DisplayName?.Length ?? 0})");
        SystemDiagnostics.Debug.WriteLine($"  ViewModel.StatusMessage: '{_viewModel.StatusMessage}' (Length: {_viewModel.StatusMessage?.Length ?? 0})");
        SystemDiagnostics.Debug.WriteLine($"  ViewModel.SelectedEmoji: '{_viewModel.SelectedEmoji}'");

        // Update preview with proper placeholders when empty
        var newNameText = string.IsNullOrWhiteSpace(_viewModel.DisplayName)
            ? "Your Name"
            : _viewModel.DisplayName;

        var newStatusText = string.IsNullOrWhiteSpace(_viewModel.StatusMessage)
            ? "Your status message..."
            : _viewModel.StatusMessage;

        var newEmojiText = _viewModel.SelectedEmoji ?? "😀";

        SystemDiagnostics.Debug.WriteLine($"  Preview will show - Name: '{newNameText}', Status: '{newStatusText}', Emoji: '{newEmojiText}'");

        PreviewName.Text = newNameText;
        PreviewStatus.Text = newStatusText;
        PreviewEmoji.Text = newEmojiText;

        // Make placeholder text look different (lighter)
        PreviewName.Opacity = string.IsNullOrWhiteSpace(_viewModel.DisplayName) ? 0.5 : 1.0;
        PreviewStatus.Opacity = string.IsNullOrWhiteSpace(_viewModel.StatusMessage) ? 0.5 : 1.0;

        SystemDiagnostics.Debug.WriteLine($"  Preview UI Updated - Name.Text: '{PreviewName.Text}', Status.Text: '{PreviewStatus.Text}', Emoji.Text: '{PreviewEmoji.Text}'");
        SystemDiagnostics.Debug.WriteLine("=== UpdatePreview END ===");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe to prevent memory leaks
        if (_viewModel != null)
        {
            _viewModel.PreviewUpdateRequested -= OnPreviewUpdateRequested;
        }

        DisplayNameEntry.TextChanged -= OnDisplayNameTextChanged;
        StatusMessageEntry.TextChanged -= OnStatusMessageTextChanged;
    }
}

