using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.Tools;

public partial class RoomsPage : ContentPage
{
    public RoomsPage(RoomService roomService)
    {
        InitializeComponent();
        BindingContext = new RoomsPageModel(roomService);
    }

    public RoomsPage()
    {
        InitializeComponent();
    }
}