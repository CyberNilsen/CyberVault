using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CyberVault
{
    public partial class LoginControl : UserControl
    {
        private MainWindow mainWindow;
        public static byte[] CurrentEncryptionKey { get; private set; }

        public LoginControl(MainWindow mw)
        {
            InitializeComponent();
            mainWindow = mw;
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Window.GetWindow(this).DragMove();
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameInput.Text;
            string password = PasswordInput.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and password cannot be empty!", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string credentialsFilePath = Path.Combine(cyberVaultPath, "credentials.txt");

                if (!File.Exists(credentialsFilePath))
                {
                    MessageBox.Show("No user accounts found. Please register first.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool userFound = false;
                byte[] salt = null;
                byte[] storedHash = null;

                foreach (string line in File.ReadAllLines(credentialsFilePath))
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 3 && parts[0] == username)
                    {
                        userFound = true;
                        salt = Convert.FromBase64String(parts[1]);
                        storedHash = Convert.FromBase64String(parts[2]);
                        break;
                    }
                }

                if (!userFound)
                {
                    MessageBox.Show("Account not found!", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] inputHash = HashPassword(password, salt);
                if (!CompareByteArrays(inputHash, storedHash))
                {
                    MessageBox.Show("Incorrect password!", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CurrentEncryptionKey = KeyDerivation.DeriveKey(password, salt);
                mainWindow.Navigate(new DashboardControl(mainWindow, username, CurrentEncryptionKey));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private byte[] HashPassword(string password, byte[] salt, int iterations = 10000, int hashSize = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(hashSize);
            }
        }

        private bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }

            return true;
        }

        private void RegisterTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mainWindow.Navigate(new RegisterControl(mainWindow));
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordInput.Visibility == Visibility.Visible)
            {
                PasswordInput.Visibility = Visibility.Collapsed;
                PlainTextPassword.Text = PasswordInput.Password;
                PlainTextPassword.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordInput.Password = PlainTextPassword.Text;
                PlainTextPassword.Visibility = Visibility.Collapsed;
                PasswordInput.Visibility = Visibility.Visible;
            }
        }
    }
}