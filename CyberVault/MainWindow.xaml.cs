using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Security.Cryptography;
using CyberVault.View;



namespace CyberVault
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void LoginTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
            CyberVault.View.Login loginWindow = new CyberVault.View.Login();
            loginWindow.Show();

            
            this.Close();
        }

        private void UsernameInput_TextChanged(object sender, TextChangedEventArgs e) { }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e) { }

        // Register knapp
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameInput.Text;
            string password = PasswordInput.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and password cannot be empty!", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                byte[] salt = GenerateSalt();
                byte[] hashedPassword = HashPassword(password, salt);
                byte[] encryptionKey = KeyDerivation.DeriveKey(password, salt);

                string saltBase64 = Convert.ToBase64String(salt);
                string hashBase64 = Convert.ToBase64String(hashedPassword);

                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                if (!Directory.Exists(cyberVaultPath)) Directory.CreateDirectory(cyberVaultPath);

                string credentialsFilePath = Path.Combine(cyberVaultPath, "credentials.txt");
                string passwordsFilePath = Path.Combine(cyberVaultPath, $"passwords_{username}.dat");

                string userCredentials = $"{username},{saltBase64},{hashBase64}";

                if (File.Exists(credentialsFilePath))
                {
                    foreach (string line in File.ReadAllLines(credentialsFilePath))
                    {
                        if (line.StartsWith(username + ","))
                        {
                            MessageBox.Show("Username already exists!", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    File.AppendAllText(credentialsFilePath, userCredentials + Environment.NewLine);
                }
                else
                {
                    File.WriteAllText(credentialsFilePath, userCredentials + Environment.NewLine);
                }

                // Create an empty password file with encryption
                PasswordStorage.SavePasswords(new List<PasswordItem>(), username, encryptionKey);

                MessageBox.Show("Registration successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                UsernameInput.Clear();
                PasswordInput.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // salte funskjonen
        private byte[] GenerateSalt(int saltSize = 16)
        {
            byte[] salt = new byte[saltSize];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        // hashe funskjonen.
        private byte[] HashPassword(string password, byte[] salt, int iterations = 10000, int hashSize = 32)
        {   //dette er algoritmen som blir brukt for å hashe. den hasher passord,
            //salta passordet. iterations og algoritme navnet in til en lang passord som havnet i txt filen lengre opp.
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(hashSize);
            }
        }
    }
}
    