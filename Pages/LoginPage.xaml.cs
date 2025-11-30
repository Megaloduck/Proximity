using Proximity.PageModels;

namespace Proximity.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = new LoginPageModel();

    }
}