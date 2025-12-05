using Proximity.Models;
using Proximity.PageModels;
using Proximity.Pages.Tools;
using Proximity.Services;
using System;
using System.Diagnostics;

namespace Proximity.Pages.MainMenu;

public partial class DiscoverPage : ContentPage
{
    private DiscoverPageModel? _pageModel;

    // Constructor for DI - THIS IS THE ONE THAT GETS CALLED
    public DiscoverPage(DiscoveryService discoveryService, ChatService chatService)
    {
        InitializeComponent();
        _pageModel = new DiscoverPageModel(discoveryService, chatService);
        BindingContext = _pageModel;
    }

    private void OnSaveNameClicked(object sender, EventArgs e)
    {
        try
        {
            _pageModel?.SaveUserName();
            DisplayAlert("Success", "Username saved!", "OK");
        }
        catch (Exception ex)
        {
            DisplayAlert("Error", $"Failed to save username: {ex.Message}", "OK");
        }
    }

    private async void OnChatClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Button button || button.BindingContext is not PeerInfo peer)
            {
                return;
            }

            // Verify peer is valid
            if (peer == null || string.IsNullOrEmpty(peer.PeerId))
            {
                await DisplayAlert("Error", "Invalid peer selected", "OK");
                return;
            }

            // Get ChatService from DI
            var chatService = Handler?.MauiContext?.Services?.GetService<ChatService>();

            if (chatService == null)
            {
                await DisplayAlert("Error", "Chat service not available", "OK");
                return;
            }

            // Verify peer is connected
            if (!peer.IsConnected)
            {
                await DisplayAlert("Error", "Please connect to this peer first", "OK");
                return;
            }

            // Create and navigate to chat page
            var chatPage = new ContactsPage(peer, chatService);
            await Navigation.PushAsync(chatPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to open chat: {ex.Message}", "OK");

        }
    }
}