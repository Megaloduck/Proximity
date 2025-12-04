using Proximity.PageModels;
using Proximity.Services;

namespace Proximity.Pages.Tools;

public partial class AuditoriumPage : ContentPage
{
    public AuditoriumPage(AuditoriumService auditoriumService)
    {
        InitializeComponent();
        BindingContext = new AuditoriumPageModel(auditoriumService);
    }

    public AuditoriumPage()
    {
        InitializeComponent();
    }
}