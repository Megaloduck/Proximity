using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.System;

public partial class ProfilePage : ContentPage
{
    private ProfilePageModel _viewModel;

    public ProfilePage(DiscoveryService discoveryService)
    {
        InitializeComponent();
        _viewModel = new ProfilePageModel(discoveryService);
        BindingContext = _viewModel;
    }

    public ProfilePage()
    {
        InitializeComponent();
    }

    // Event handler to refresh preview when text changes
    private void OnProfileChanged(object sender, TextChangedEventArgs e)
    {
        // Force binding update by triggering property changed
        if (_viewModel != null)
        {
            // The bindings will automatically update the preview
            // This handler is just here to ensure real-time updates
        }
    }
}