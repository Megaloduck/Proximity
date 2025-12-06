using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.System;

public partial class ProfilePage : ContentPage
{
   

    public ProfilePage(DiscoveryService discoveryService)
    {
        InitializeComponent();
        BindingContext = new ProfilePageModel(discoveryService);
    }   
}
