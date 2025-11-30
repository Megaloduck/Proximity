using Proximity.PageModels;

namespace Proximity.Pages;

public partial class SidebarPage : ContentPage
{
    private readonly SidebarPageModel _pageModel;

    public SidebarPage()
    {
        InitializeComponent();

        _pageModel = new SidebarPageModel();
        _pageModel.NavigateAction = NavigateToPage;
        BindingContext = _pageModel;

        // Load initial page
        _pageModel.NavigateToDashboardCommand.Execute(null);
    }

    private void NavigateToPage(Page page)
    {
        // Cast to ContentPage and extract the Content
        if (page is ContentPage contentPage)
        {
            ContentArea.Content = contentPage.Content;
        }
    }
}