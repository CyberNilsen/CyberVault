using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Security.Cryptography;

namespace CyberVault
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void UsernameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Handle username input changes if needed
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Handle password input changes if needed
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Get username and password from the textboxes
            string username = UsernameInput.Text;
            string password = PasswordInput.Password; // Use Password property from PasswordBox

            // Validate inputs
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username and password cannot be empty!", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Generate a random salt
                byte[] salt = GenerateSalt();

                // Hash the password using PBKDF2
                byte[] hashedPassword = HashPassword(password, salt);

                // Convert hash and salt to base64 strings for storage
                string saltBase64 = Convert.ToBase64String(salt);
                string hashBase64 = Convert.ToBase64String(hashedPassword);

                // Create directory if it doesn't exist
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");

                if (!Directory.Exists(cyberVaultPath))
                {
                    Directory.CreateDirectory(cyberVaultPath);
                }

                // Create user credentials file
                string credentialsFilePath = Path.Combine(cyberVaultPath, "credentials.txt");

                // Store username, salt, and hashed password in the file
                // Format: username,salt,hashedPassword
                string userCredentials = $"{username},{saltBase64},{hashBase64}";

                // Check if the user already exists
                if (File.Exists(credentialsFilePath))
                {
                    string[] existingCredentials = File.ReadAllLines(credentialsFilePath);
                    foreach (string line in existingCredentials)
                    {
                        if (line.StartsWith(username + ","))
                        {
                            MessageBox.Show("Username already exists!", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    // Append new user credentials
                    File.AppendAllText(credentialsFilePath, userCredentials + Environment.NewLine);
                }
                else
                {
                    // Create new file with user credentials
                    File.WriteAllText(credentialsFilePath, userCredentials + Environment.NewLine);
                }

                MessageBox.Show("Registration successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear the input fields after successful registration
                UsernameInput.Clear();
                PasswordInput.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during registration: {ex.Message}", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper method to generate a random salt
        private byte[] GenerateSalt(int saltSize = 16)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] salt = new byte[saltSize];
                rng.GetBytes(salt);
                return salt;
            }
        }

        // Helper method to hash the password using PBKDF2
        private byte[] HashPassword(string password, byte[] salt, int iterations = 10000, int hashSize = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return pbkdf2.GetBytes(hashSize);
            }
        }

        // Method to validate login credentials
        public bool ValidateLogin(string username, string password)
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string credentialsFilePath = Path.Combine(appDataPath, "CyberVault", "credentials.txt");

                if (!File.Exists(credentialsFilePath))
                {
                    return false;
                }

                string[] credentials = File.ReadAllLines(credentialsFilePath);

                foreach (string line in credentials)
                {
                    string[] parts = line.Split(',');
                    if (parts.Length != 3)
                    {
                        continue;
                    }

                    if (parts[0] == username)
                    {
                        // Extract stored salt and hash
                        byte[] storedSalt = Convert.FromBase64String(parts[1]);
                        byte[] storedHash = Convert.FromBase64String(parts[2]);

                        // Hash the provided password with the stored salt
                        byte[] computedHash = HashPassword(password, storedSalt);

                        // Compare the computed hash with the stored hash
                        return CompareByteArrays(computedHash, storedHash);
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Helper method to compare two byte arrays
        private bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
            {
                return false;
            }

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }

            return true;
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

            if (ValidateLogin(username, password))
            {
                MessageBox.Show("Login successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Open the main application window or dashboard
                VaultDashboard dashboard = new VaultDashboard(username);
                dashboard.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid username or password!", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}