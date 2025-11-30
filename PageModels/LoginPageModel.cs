using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Proximity.Pages;
using Proximity.Pages.MainMenu;
using Proximity.Pages.Tools;
using Proximity.Pages.System;

namespace Proximity.PageModels
{
    public partial class LoginPageModel : ObservableObject
    {
        public IRelayCommand LoginCommand { get; }
        public LoginPageModel()
        {
            LoginCommand = new RelayCommand(OnLogin);
        }
        private async void OnLogin()
        {
            await Shell.Current.GoToAsync("///SidebarPage");

        }
    }
}