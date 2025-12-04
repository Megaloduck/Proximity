using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.Tools;

public partial class RoomsPage : ContentPage
{
    public RoomsPage(RoomService roomService, DiscoveryService discoveryService)
    {
        InitializeComponent();
        BindingContext = new RoomsPageModel(roomService, discoveryService);
    }

    public RoomsPage()
    {
        InitializeComponent();
    }
}