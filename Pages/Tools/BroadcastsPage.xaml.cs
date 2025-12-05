using Proximity.Services;

namespace Proximity.Pages.Tools;

public partial class BroadcastsPage : ContentPage
{
	public BroadcastsPage(BroadcastService broadcastService, DiscoveryService discoveryService)    
	{
		InitializeComponent();
	}
}