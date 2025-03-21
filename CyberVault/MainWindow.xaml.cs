using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Windows.Media.Animation;
using CyberVault.View;
using System.Windows.Media;





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

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = PasswordInput.Password;

            // Calculate password strength
            int strength = CalculatePasswordStrength(password);

            // Animate the progress bar to the new strength value
            AnimateProgressBar(strength);

            // Update the strength message
            PasswordStrengthMessage.Text = GetStrengthMessage(strength);
        }

        private void AnimateProgressBar(int targetValue)
        {
            // Create a new animation for the ProgressBar value
            DoubleAnimation animation = new DoubleAnimation
            {
                From = PasswordStrengthBar.Value,
                To = targetValue,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase()
            };

            // Begin the animation on the ProgressBar
            PasswordStrengthBar.BeginAnimation(ProgressBar.ValueProperty, animation);

            // Change the color of the ProgressBar based on the password strength
            if (targetValue < 50)
            {
                PasswordStrengthBar.Foreground = new SolidColorBrush(Colors.Red); // Red for weak passwords
            }
            else if (targetValue < 81)
            {
                PasswordStrengthBar.Foreground = new SolidColorBrush(Colors.Yellow); // Yellow for medium strength passwords
            }
            else
            {
                PasswordStrengthBar.Foreground = new SolidColorBrush(Colors.Green); // Green for strong passwords
            }
        }

        private void UpdatePasswordStrength(object sender, RoutedEventArgs e)
        {
            // Get the password from PasswordInput
            string password = PasswordInput.Password;

            // Calculate the strength (you can replace this with your strength calculation logic)
            int strength = CalculatePasswordStrength(password);

            // Update the strength bar
            AnimateProgressBar(strength);
        }


        private int CalculatePasswordStrength(string password)
        {
            int strength = 0;

            // Check password length (stronger if it's longer)
            if (password.Length >= 4) strength += 5;
            if (password.Length >= 6) strength += 10;
            if (password.Length >= 8) strength += 20;
            if (password.Length >= 15) strength += 50;

            // Check for upper case letters
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]"))
                strength += 20;

            // Check for digits
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]"))
                strength += 20;

            // Check for special characters
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[\W_]"))
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


        // Add these new methods to your MainWindow.xaml.cs file

        private bool _passwordVisible = false;

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            _passwordVisible = !_passwordVisible;

            if (_passwordVisible)
            {
                // Show the password
                PasswordVisible.Text = PasswordInput.Password;
                PasswordVisible.Visibility = Visibility.Visible;
                PasswordInput.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Hide the password
                PasswordInput.Password = PasswordVisible.Text;
                PasswordVisible.Visibility = Visibility.Collapsed;
                PasswordInput.Visibility = Visibility.Visible;
            }
        }

        private void PasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update the password strength when the visible textbox changes
            string password = PasswordVisible.Text;

            // Calculate password strength
            int strength = CalculatePasswordStrength(password);

            // Animate the progress bar to the new strength value
            AnimateProgressBar(strength);

            // Update the strength message
            PasswordStrengthMessage.Text = GetStrengthMessage(strength);
        }


    }
}
    