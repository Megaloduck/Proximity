namespace Proximity
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Start the app at LoginPage
            GoToAsync("//LoginPage");
        }
    }
}