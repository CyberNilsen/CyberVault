using CyberVault.WebExtension;
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Windows.Security.Credentials.UI;

namespace CyberVault
{
    public partial class BiometricLoginControl : UserControl
    {
        private MainWindow mainWindow;

        public BiometricLoginControl(MainWindow mw)
        {
            InitializeComponent();
            mainWindow = mw;
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    mainWindow.ToggleWindowState();
                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Window.GetWindow(this).DragMove();
                }
            }
        }

        private async void BiometricAuthButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var availabilityResult = await UserConsentVerifier.CheckAvailabilityAsync();

                if (availabilityResult == UserConsentVerifierAvailability.Available)
                {
                    var consentResult = await UserConsentVerifier.RequestVerificationAsync("CyberVault Authentication");

                    if (consentResult == UserConsentVerificationResult.Verified)
                    {
                        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                        string bioConfigFilePath = Path.Combine(cyberVaultPath, "biometric_config.txt");

                        if (File.Exists(bioConfigFilePath))
                        {
                            string[] bioConfig = File.ReadAllLines(bioConfigFilePath);
                            if (bioConfig.Length > 0)
                            {
                                string[] credentials = bioConfig[0].Split(',');
                                if (credentials.Length >= 2)
                                {
                                    string username = credentials[0];
                                    byte[] encryptedPassword = Convert.FromBase64String(credentials[1]);

                                    HandleBiometricLogin(username, encryptedPassword);
                                    return;
                                }
                            }
                            MessageBox.Show("Biometric configuration is invalid. Please set up biometric authentication again.",
                                "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            MessageBox.Show("Biometric login not configured. Please enable it in settings first.",
                                "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Biometric verification failed. Please try again or use password login.",
                            "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Windows Hello is not available on this device or is not properly set up.",
                        "Windows Hello Not Available", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during biometric authentication: {ex.Message}",
                    "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleBiometricLogin(string username, byte[] encryptedPassword)
        {
            try
            {
                byte[] decryptedPasswordBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                    encryptedPassword,
                    null,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);

                string decryptedPassword = Encoding.UTF8.GetString(decryptedPasswordBytes);

                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string credentialsPath = Path.Combine(cyberVaultPath, "credentials.txt");

                byte[]? salt = null;

                if (File.Exists(credentialsPath))
                {
                    string[] lines = File.ReadAllLines(credentialsPath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith(username + ","))
                        {
                            string[] parts = line.Split(',');
                            if (parts.Length >= 2)
                            {
                                salt = Convert.FromBase64String(parts[1]);
                                break;
                            }
                        }
                    }
                }

                if (salt == null)
                {
                    MessageBox.Show("Could not find user credentials.",
                        "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] derivedKey = KeyDerivation.DeriveKey(decryptedPassword, salt);

                App.CurrentUsername = username;
                App.LoadUserSettings(username);

                mainWindow.Navigate(new DashboardControl(mainWindow, username, derivedKey));
                int lockTimeMinutes = GetAutoLockTimeFromSettings(username);
                mainWindow.UserLoggedIn(username, lockTimeMinutes);

                if (App.WebServer == null)
                {
                    App.WebServer = new LocalWebServer(username, derivedKey);
                    App.WebServer.Start();
                    App.CurrentAccessToken = App.WebServer.GetAccessToken();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login failed: {ex.Message}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetAutoLockTimeFromSettings(string username)
        {
            int minutes = 5;

            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string settingsFilePath = Path.Combine(cyberVaultPath, $"{username}_settings.ini");

                if (File.Exists(settingsFilePath))
                {
                    string[] lines = File.ReadAllLines(settingsFilePath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2 && parts[0] == "AutoLockTime")
                        {
                            string value = parts[1];
                            if (value == "Never")
                                return 0;
                            else if (value == "1 Minute")
                                return 1;
                            else if (value == "5 Minutes")
                                return 5;
                            else if (value == "15 Minutes")
                                return 15;
                            else if (value == "30 Minutes")
                                return 30;
                            else if (value == "1 Hour")
                                return 60;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting auto-lock time: {ex.Message}");
            }

            return minutes;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Navigate(new LoginControl(mainWindow));
        }

        private void PasswordLogin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mainWindow.Navigate(new LoginControl(mainWindow));
        }
    }
}