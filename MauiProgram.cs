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

            // Register Services as Singletons (shared state across app)
            builder.Services.AddSingleton<DiscoveryService>();
            builder.Services.AddSingleton<ChatService>(sp =>
            {
                var discoveryService = sp.GetRequiredService<DiscoveryService>();
                var userName = Preferences.Get("UserName", "User_" + Random.Shared.Next(1000, 9999));
                var chatService = new ChatService(discoveryService.GetLocalId(), userName);
                chatService.StartListening();
                return chatService;
            });
            builder.Services.AddSingleton<VoiceService>(sp =>
            {
                var voiceService = new VoiceService();
                voiceService.Initialize();
                return voiceService;
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

            // System Pages            
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<ProfilePageModel>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SettingsPageModel>();

            return builder.Build();
        }
    }
}