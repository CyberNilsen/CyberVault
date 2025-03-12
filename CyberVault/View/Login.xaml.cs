using System;
using System.Windows;
using System.Windows.Input;

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
            // Logic for login button click
            MessageBox.Show("Login button clicked!");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Closes the application 
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

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove(); 
            }
        }

        private void SignupTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();



            this.Close();
        }
    }
}
