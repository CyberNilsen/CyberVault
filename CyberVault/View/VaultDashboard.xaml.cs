using System.Windows;
using System.Windows.Input;

namespace CyberVault.View
{
    public partial class VaultDashboard : Window
    {
        private string _username;

        public VaultDashboard(string username)
        {
            InitializeComponent();  

            _username = username;
            Title = $"CyberVault - {_username}'s Dashboard";
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
