using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CyberVault.View;
using CyberVault.Viewmodel;

namespace CyberVault
{
    public partial class DashboardControl : UserControl
    {
        private MainWindow mainWindow;
        private string username;
        private byte[] encryptionKey;
        public DashboardControl(MainWindow mw, string user, byte[] key)
        {
            InitializeComponent();
            mainWindow = mw;
            username = user;
            encryptionKey = key;
            WelcomeText.Text = $"Welcome, {username}";
            DashboardContent.Content = new HomeDashboardControl();
        }
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Window.GetWindow(this).DragMove();
            }
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).WindowState = WindowState.Minimized;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this).Close();
        }
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardContent.Content = new PasswordVaultControl(mainWindow, username, encryptionKey);
        }

        private void AuthenticatorButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardContent.Content = new AuthenticatorControl();
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            
            mainWindow.Navigate(new LoginControl(mainWindow));
        }
    }
}
