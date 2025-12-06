using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.MainMenu;

public partial class DashboardPage : ContentPage
{
    public DashboardPage(DiscoveryService discoveryService, ChatService chatService, VoiceService voiceService)
    {
        InitializeComponent();
        BindingContext = new DashboardPageModel(discoveryService, chatService, voiceService);
    }
}