using Proximity.Models;
using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.Tools;

public partial class ContactsPage : ContentPage
{
    // Constructor with DI support - THIS IS THE ONE THAT GETS CALLED
    public ContactsPage(PeerInfo peer, ChatService chatService)
    {
        InitializeComponent();
        BindingContext = new ContactsPageModel(peer, chatService);
    }
}