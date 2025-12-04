using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.System;

public partial class SettingsPage : ContentPage
{
    // Updated constructor with correct service types
    public SettingsPage(DiscoveryService discoveryService, VoiceService voiceService)
    {
        InitializeComponent();
        BindingContext = new SettingsPageModel(discoveryService, voiceService);
    }

    // Keep parameterless constructor for XAML designer
    public SettingsPage()
    {
        InitializeComponent();
    }
}