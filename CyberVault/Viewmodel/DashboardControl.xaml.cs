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
            DashboardContent.Content = new HomeDashboardControl(username, encryptionKey);
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
            DashboardContent.Content = new HomeDashboardControl(username, encryptionKey);
        }
        private void PasswordManagerButton_Click(object sender, RoutedEventArgs e)
        {
            WelcomeText.Text = "Password Manager";
            DashboardContent.Content = new PasswordVaultControl(mainWindow, username, encryptionKey);
        }

        private void AuthenticatorButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardContent.Content = null;

            WelcomeText.Text = "Two-Factor Authentication";

            var authenticatorControl = new AuthenticatorControl();
            authenticatorControl.Initialize(username, encryptionKey);

            DashboardContent.Content = authenticatorControl;
        }
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            typeof(LoginControl).GetProperty("CurrentEncryptionKey",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static)?.SetValue(null, null);

            username = string.Empty;
            mainWindow.Navigate(new LoginControl(mainWindow));
        }

    }
}
