using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Security.Cryptography;
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

            // Sjekk om credentials.txt filen finnes
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
            string credentialsFilePath = Path.Combine(cyberVaultPath, "credentials.txt");

            if (!File.Exists(credentialsFilePath))
            {
                MessageBox.Show("No users registered in the system.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                bool userFound = false;
                bool loginSuccess = false;

                // Les hver linje fra credentials.txt
                foreach (string line in File.ReadAllLines(credentialsFilePath))
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 3 && parts[0] == username)
                    {

                        userFound = true;

                        // Hent salt og hashet passord
                        byte[] salt = Convert.FromBase64String(parts[1]);
                        byte[] storedHash = Convert.FromBase64String(parts[2]);

                        // Hash det angitte passordet med samme salt
                        byte[] inputHash = HashPassword(password, salt);

                        // Sammenlign de to hashene
                        loginSuccess = CompareByteArrays(inputHash, storedHash);
                        break;
                    }
                }

                if (!userFound)
                {
                    MessageBox.Show("Username or password is incorrect", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (!loginSuccess)
                {
                    MessageBox.Show("Username or password is incorrect.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // Hvis alt funker og passord er riktig så kommer du videre
                    VaultDashboard vaultDashboard = new VaultDashboard(username);
                    vaultDashboard.Show();
                    this.Close(); // lukker login vinduet.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Sammenlign to byte arrays
        private bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            // Bruk en constant-time sammenligning for å unngå timing attacks
            int result = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                result |= array1[i] ^ array2[i];
            }
            return result == 0;
        }

        // Hash passord med samme metode som i registrering
        private byte[] HashPassword(string password, byte[] salt, int iterations = 10000, int hashSize = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(hashSize);
            }
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