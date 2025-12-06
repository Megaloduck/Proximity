using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.System;

public partial class SettingsPage : ContentPage
{
    private SettingsPageModel _viewModel;

    public SettingsPage(DiscoveryService discoveryService, VoiceService voiceService)
    {
        InitializeComponent();
        _viewModel = new SettingsPageModel(discoveryService, voiceService);
        BindingContext = _viewModel;
    }

    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Cleanup when navigating away
        _viewModel?.Cleanup();        
    }
}