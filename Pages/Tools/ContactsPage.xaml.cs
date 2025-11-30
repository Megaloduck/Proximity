using Proximity.Models;
using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.Tools;

public partial class ContactsPage : ContentPage
{
    // Constructor with DI support
    public ContactsPage(PeerInfo? peer, ChatService chatService)
    {
        InitializeComponent();
        BindingContext = new ContactsPageModel(peer, chatService);
    }

    // Parameterless constructor for XAML designer
    public ContactsPage() : this(null, CreateDefaultChatService())
    {
    }

    private static ChatService CreateDefaultChatService()
    {
        var userName = Preferences.Get("UserName", "User");
        var discoveryService = new DiscoveryService();
        var chatService = new ChatService(discoveryService.GetLocalId(), userName);
        chatService.StartListening();
        return chatService;
    }
}