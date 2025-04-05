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
        public Border TopBar => Dashboardtopbar;
        public DashboardControl(MainWindow mw, string user, byte[] key)
        {
            InitializeComponent();
            mainWindow = mw;
            username = user;
            encryptionKey = key;
            WelcomeText.Text = $"Welcome to CyberVault, {username}!";
            DashboardContent.Content = new HomeDashboardControl();
        }
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Window.GetWindow(this).DragMove();
            }
        }
        public void SetTopBarHeight(double height)
        {
            Dashboardtopbar.Height = height;
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            WelcomeText.Text = $"Welcome to CyberVault, {username}!";
            DashboardContent.Content = null;
        }
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            WelcomeText.Text = "Password Manager";
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
