using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CyberVault
{
    public partial class RegisterControl : UserControl
    {
        private MainWindow mainWindow;
        private bool _passwordVisible = false;

        public RegisterControl(MainWindow mw)
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

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameInput.Text;
            string password = _passwordVisible ? PasswordVisible.Text : PasswordInput.Password;

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
                if (!Directory.Exists(cyberVaultPath))
                    Directory.CreateDirectory(cyberVaultPath);

                string credentialsFilePath = Path.Combine(cyberVaultPath, "credentials.txt");
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

                PasswordStorage.SavePasswords(new List<PasswordItem>(), username, encryptionKey);

                // Create settings file for the new user
                CreateUserSettingsFile(username, cyberVaultPath);

                MessageBox.Show("Registration successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                UsernameInput.Clear();
                PasswordInput.Clear();
                PasswordVisible.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateUserSettingsFile(string username, string cyberVaultPath)
        {
            try
            {
                string settingsFileName = $"{username}_settings.ini";
                string settingsFilePath = Path.Combine(cyberVaultPath, settingsFileName);

                // Create default settings with all toggles initially set to false
                string defaultSettings = "[Settings]\n" +
                                        "TwoFactorEnabled=false\n" +
                                        "StartWithWindows=false\n" +
                                        "MinimizeToTray=false\n" +
                                        "DarkModeEnabled=false\n" +
                                        "CloudSyncEnabled=false\n" +
                                        "BiometricEnabled=false\n";

                File.WriteAllText(settingsFilePath, defaultSettings);
            }
            catch (Exception ex)
            {
                // Log the error but don't interrupt the registration flow
                Console.WriteLine($"Error creating settings file: {ex.Message}");
            }
        }

        private void LoginTextBlock_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            mainWindow.Navigate(new LoginControl(mainWindow));
        }

        private byte[] GenerateSalt(int saltSize = 16)
        {
            byte[] salt = new byte[saltSize];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        private byte[] HashPassword(string password, byte[] salt, int iterations = 10000, int hashSize = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(hashSize);
            }
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = PasswordInput.Password;
            int strength = CalculatePasswordStrength(password);
            AnimateProgressBar(strength);
            PasswordStrengthMessage.Text = GetStrengthMessage(strength);
        }

        private void PasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            string password = PasswordVisible.Text;
            int strength = CalculatePasswordStrength(password);
            AnimateProgressBar(strength);
            PasswordStrengthMessage.Text = GetStrengthMessage(strength);
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _passwordVisible = !_passwordVisible;

            if (_passwordVisible)
            {
                PasswordVisible.Text = PasswordInput.Password;
                PasswordVisible.Visibility = Visibility.Visible;
                PasswordInput.Visibility = Visibility.Collapsed;
            }
            else
            {
                PasswordInput.Password = PasswordVisible.Text;
                PasswordVisible.Visibility = Visibility.Collapsed;
                PasswordInput.Visibility = Visibility.Visible;
            }
        }

        private void AnimateProgressBar(int targetValue)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = PasswordStrengthBar.Value,
                To = targetValue,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase()
            };

            PasswordStrengthBar.BeginAnimation(ProgressBar.ValueProperty, animation);

            if (targetValue < 50)
                PasswordStrengthBar.Foreground = new SolidColorBrush(Colors.Red);
            else if (targetValue < 81)
                PasswordStrengthBar.Foreground = new SolidColorBrush(Colors.Yellow);
            else
                PasswordStrengthBar.Foreground = new SolidColorBrush(Colors.Green);
        }

        private int CalculatePasswordStrength(string password)
        {
            int strength = 0;

            if (password.Length >= 4)
                strength += 5;
            if (password.Length >= 6)
                strength += 10;
            if (password.Length >= 8)
                strength += 20;
            if (password.Length >= 15)
                strength += 50;

            if (Regex.IsMatch(password, @"[A-Z]"))
                strength += 20;
            if (Regex.IsMatch(password, @"[0-9]"))
                strength += 20;
            if (Regex.IsMatch(password, @"[\W_]"))
                strength += 20;

            return strength;
        }

        private string GetStrengthMessage(int strength)
        {
            if (strength < 40)
                return "Weak password";
            else if (strength < 70)
                return "Moderate password";
            else
                return "Strong password";
        }
    }
}