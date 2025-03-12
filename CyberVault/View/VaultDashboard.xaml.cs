using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows;

namespace CyberVault.View.Dashboard
{
    public class VaultDashboard : Window
    {
        private string _username;

        public VaultDashboard(string username)
        {
            _username = username;
            Title = $"CyberVault - {_username}'s Dashboard";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Grid mainGrid = new Grid();
            Content = mainGrid;

            Label welcomeLabel = new Label
            {
                Content = $"Welcome to your secure vault, {_username}!",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            mainGrid.Children.Add(welcomeLabel);
        }
    }
}
