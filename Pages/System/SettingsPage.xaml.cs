using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.System;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(DiscoveryService discoveryService, VoiceService voiceService)
    {
        InitializeComponent();
        BindingContext = new SettingsPageModel(discoveryService, voiceService);
    }

    public SettingsPage()
    {
        InitializeComponent();
    }
}