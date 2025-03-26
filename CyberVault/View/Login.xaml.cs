using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Security.Cryptography;
using System.Windows.Controls;

namespace CyberVault.View
{
    public partial class Login : Window
    {
        private static byte[] _currentEncryptionKey;

        public static byte[] CurrentEncryptionKey
        {
            get { return _currentEncryptionKey; }
            private set { _currentEncryptionKey = value; }
        }

        public Login()
        {
            InitializeComponent();
        }



        public static byte[] GetEncryptionKey()
        {
            return CurrentEncryptionKey;
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
                    MessageBox.Show("Username not found!", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] inputHash = HashPassword(password, salt);
                bool passwordMatch = CompareByteArrays(inputHash, storedHash);

                if (!passwordMatch)
                {
                    MessageBox.Show("Incorrect password!", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CurrentEncryptionKey = KeyDerivation.DeriveKey(password, salt);

                VaultDashboard dashboard = new VaultDashboard(username);
                dashboard.SetEncryptionKey(CurrentEncryptionKey);
                dashboard.Show();
                this.Close();
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

        private void RegisterTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }


        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
        }

        private void UsernameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void SignupTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            LoginButton_Click(sender, e);
        }

    }
}