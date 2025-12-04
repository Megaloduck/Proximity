using Proximity.Models;
using Proximity.PageModels;
using Proximity.Pages.Tools;
using Proximity.Services;

namespace Proximity.Pages.MainMenu;

public partial class DiscoverPage : ContentPage
{
    private DiscoverPageModel? _pageModel;

    // Constructor for DI - THIS IS THE ONE THAT GETS CALLED
    public DiscoverPage(DiscoveryService discoveryService, ChatService chatService)
    {
        InitializeComponent();
        _pageModel = new DiscoverPageModel(discoveryService, chatService);
        BindingContext = _pageModel;
    }

    private void OnSaveNameClicked(object sender, EventArgs e)
    {
        _pageModel?.SaveUserName();
        DisplayAlert("Success", "Username saved!", "OK");
    }

    private async void OnChatClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is PeerInfo peer)
        {
            // Get ChatService from DI
            var chatService = Handler?.MauiContext?.Services.GetService<ChatService>();
            if (chatService != null)
            {
                var chatPage = new ContactsPage(peer, chatService);
                await Navigation.PushAsync(chatPage);
            }
        }
    }
}