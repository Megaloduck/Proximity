using Microsoft.Extensions.Logging;
using Proximity.PageModels;
using Proximity.Pages;
using Proximity.Pages.MainMenu;
using Proximity.Pages.System;
using Proximity.Pages.Tools;


namespace Proximity
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());
#endif


            // Core Fundamental Pages
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<LoginPageModel>();
            builder.Services.AddSingleton<SidebarPage>();
            builder.Services.AddSingleton<SidebarPageModel>();

            // Main Menu Pages
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<DiscoverPage>();

            // Tools Pages
            builder.Services.AddTransient<ContactsPage>();
            builder.Services.AddTransient<RoomsPage>();
            builder.Services.AddTransient<AuditoriumPage>();

            // Main Menu Pages            
            builder.Services.AddTransient<UsersPage>();
            builder.Services.AddTransient<SettingsPage>();






            return builder.Build();
        }
    }
}
