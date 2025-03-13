using System;
using System.Windows;
using System.Windows.Input;
using CyberVault.View;

namespace CyberVault.View
{
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Får brukernavn og passord fra input feltet.
            string username = UsernameInput.Text;
            string password = PasswordInput.Password;

            // Deretter sjekker den om et eller begge feltene er tomme.
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                // Hvis de er tomme så kommer det opp du må skrive både brukernavn og passord.
                MessageBox.Show("You must enter both username and password!", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Så får du heller ikke gått videre
            }

            // Hvis alt funker og du har skivd inn i begge feltene så kommer du det videre
            VaultDashboard vaultDashboard = new VaultDashboard(username);
            vaultDashboard.Show();
            this.Close(); //lukker login vinduet.
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
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

        private void UsernameInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
         
        }

        private void SignupTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close(); 
        }
    }
}
