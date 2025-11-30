using Proximity.Models;
using Proximity.PageModels;
using Proximity.Pages.Tools;
using Proximity.Services;

namespace Proximity.Pages.MainMenu;

public partial class DiscoverPage : ContentPage
{
    private DiscoverPageModel? _pageModel;

    // Constructor for DI
    public DiscoverPage(DiscoveryService discoveryService, ChatService chatService)
    {
        InitializeComponent();
        _pageModel = new DiscoverPageModel(discoveryService, chatService);
        BindingContext = _pageModel;
    }

    // Parameterless constructor for XAML designer
    public DiscoverPage() : this(new DiscoveryService(), CreateDefaultChatService())
    {
    }

    private static ChatService CreateDefaultChatService()
    {
        var userName = Preferences.Get("UserName", "User");
        var discoveryService = new DiscoveryService();
        return new ChatService(discoveryService.GetLocalId(), userName);
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
            // Get services from DI
            var chatService = Handler?.MauiContext?.Services.GetService<ChatService>();
            if (chatService != null)
            {
                var chatPage = new ContactsPage(peer, chatService);
                await Navigation.PushAsync(chatPage);
            }
        }
    }
}