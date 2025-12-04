using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using Proximity.PageModels; 
using Proximity.Pages;
using Proximity.Pages.MainMenu;
using Proximity.Pages.System;
using Proximity.Pages.Tools;
using Proximity.Services;

namespace Proximity
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
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

            // ===== Register Services =====

            // Discovery Service - Singleton so all pages share same instance
            builder.Services.AddSingleton<DiscoveryService>(sp =>
            {
                var deviceName = Preferences.Get("UserName", "User");
                return new DiscoveryService(deviceName, port: 9001);
            });
            // Messaging Service - Singleton, depends on Discovery
            builder.Services.AddSingleton<ChatService>(sp =>
            {
                var discovery = sp.GetRequiredService<DiscoveryService>();
                return new ChatService(discovery.MyDeviceId, discovery.MyDeviceName);
            });

            // Core Fundamental Pages
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<LoginPageModel>();
            builder.Services.AddSingleton<SidebarPage>();
            builder.Services.AddSingleton<SidebarPageModel>();

            // Main Menu Pages
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<DashboardPageModel>();
            builder.Services.AddTransient<DiscoverPage>();
            builder.Services.AddTransient<DiscoverPageModel>();

            // Tools Pages
            builder.Services.AddTransient<ContactsPage>();
            builder.Services.AddTransient<ContactsPageModel>();
            builder.Services.AddTransient<RoomsPage>();
            builder.Services.AddTransient<AuditoriumPage>();
            builder.Services.AddTransient<BroadcastsPage>();


            // System Pages            
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<ProfilePageModel>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SettingsPageModel>();

            return builder.Build();
        }
    }
}