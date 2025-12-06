using Proximity.PageModels;
using SystemDiagnostics = System.Diagnostics; // Add alias to avoid conflict

namespace Proximity.Pages;

public partial class SidebarPage : ContentPage
{
    private readonly SidebarPageModel _pageModel;
    private Page _currentPage; // Keep reference to current page

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
        try
        {
            // Cleanup previous page if it exists
            if (_currentPage != null)
            {
                // If previous page was SettingsPage, cleanup resources
                if (_currentPage is ContentPage oldContentPage &&
                    oldContentPage.BindingContext is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                // Clear the content area
                if (ContentArea.Content != null)
                {
                    ContentArea.Content = null;
                }
            }

            // Store reference to new page
            _currentPage = page;

            // Cast to ContentPage and extract the Content
            if (page is ContentPage contentPage && contentPage.Content != null)
            {
                // Remove content from its parent page first
                var content = contentPage.Content;
                contentPage.Content = null; // Remove from parent

                // Now add to ContentArea
                ContentArea.Content = content;

                SystemDiagnostics.Debug.WriteLine($"SidebarPage: Navigated to {page.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            SystemDiagnostics.Debug.WriteLine($"SidebarPage NavigateToPage error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Cleanup when sidebar page is closing
        if (_currentPage is ContentPage contentPage &&
            contentPage.BindingContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}