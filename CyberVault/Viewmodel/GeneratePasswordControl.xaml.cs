using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CyberVault.Viewmodel
{
    public partial class GeneratePasswordControl : UserControl
    {
        private string _username;
        private byte[] _encryptionKey;

        private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        private const string NumberChars = "0123456789";
        private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
        private const string SimilarChars = "0O1lI";
        private const string AmbiguousChars = "{}[]()/'\"~,;.<>";

        public GeneratePasswordControl(string username, byte[] encryptionKey)
        {
            InitializeComponent();
            _username = username;
            _encryptionKey = encryptionKey;

            this.Loaded += (s, e) =>
            {
                GenerateNewPassword();
            };

            LengthSlider.ValueChanged += (s, e) => UpdatePasswordStrength();
            UppercaseToggle.Checked += (s, e) => UpdatePasswordStrength();
            UppercaseToggle.Unchecked += (s, e) => UpdatePasswordStrength();
            LowercaseToggle.Checked += (s, e) => UpdatePasswordStrength();
            LowercaseToggle.Unchecked += (s, e) => UpdatePasswordStrength();
            NumbersToggle.Checked += (s, e) => UpdatePasswordStrength();
            NumbersToggle.Unchecked += (s, e) => UpdatePasswordStrength();
            SpecialToggle.Checked += (s, e) => UpdatePasswordStrength();
            SpecialToggle.Unchecked += (s, e) => UpdatePasswordStrength();
        }

        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            GenerateNewPassword();
        }

        private void GenerateNewPassword()
        {
            try
            {
                int length = (int)LengthSlider.Value;
                string characterSet = BuildCharacterSet();

                if (string.IsNullOrEmpty(characterSet))
                {
                    MessageBox.Show("Please select at least one character type.", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string password = GenerateSecurePassword(characterSet, length);
                GeneratedPasswordTextBox.Text = password;
                UpdatePasswordStrength();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating password: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildCharacterSet()
        {
            StringBuilder charSet = new StringBuilder();

            if (UppercaseToggle.IsChecked == true)
                charSet.Append(UppercaseChars);

            if (LowercaseToggle.IsChecked == true)
                charSet.Append(LowercaseChars);

            if (NumbersToggle.IsChecked == true)
                charSet.Append(NumberChars);

            if (SpecialToggle.IsChecked == true)
                charSet.Append(SpecialChars);

            string result = charSet.ToString();

            if (ExcludeSimilarToggle.IsChecked == true)
            {
                foreach (char c in SimilarChars)
                {
                    result = result.Replace(c.ToString(), "");
                }
            }

            if (ExcludeAmbiguousToggle.IsChecked == true)
            {
                foreach (char c in AmbiguousChars)
                {
                    result = result.Replace(c.ToString(), "");
                }
            }

            return result;
        }

        private string GenerateSecurePassword(string characterSet, int length)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                StringBuilder password = new StringBuilder();
                byte[] randomBytes = new byte[4];

                for (int i = 0; i < length; i++)
                {
                    rng.GetBytes(randomBytes);
                    uint randomValue = BitConverter.ToUInt32(randomBytes, 0);
                    int index = (int)(randomValue % characterSet.Length);
                    password.Append(characterSet[index]);
                }

                return password.ToString();
            }
        }

        private void UpdatePasswordStrength()
        {
            try
            {
                int length = (int)LengthSlider.Value;
                int characterTypes = 0;

                if (UppercaseToggle.IsChecked == true) characterTypes++;
                if (LowercaseToggle.IsChecked == true) characterTypes++;
                if (NumbersToggle.IsChecked == true) characterTypes++;
                if (SpecialToggle.IsChecked == true) characterTypes++;

                int strengthScore = CalculatePasswordStrength(length, characterTypes);

                UpdateStrengthIndicator(strengthScore);
                UpdateStrengthDisplay(strengthScore);
                UpdateStrengthLevelIndicators(strengthScore);
            }
            catch (Exception)
            {
                StrengthIndicator.Width = 20;
                StrengthLabel.Text = "Unknown";
                StrengthPercentage.Text = "0%";
            }
        }

        private void UpdateStrengthIndicator(int score)
        {
            var parentBorder = StrengthIndicator.Parent as Grid;
            if (parentBorder != null)
            {
                double percentage = score / 100.0;
                double targetWidth = parentBorder.ActualWidth * percentage;
                targetWidth = Math.Max(20, targetWidth);

                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = targetWidth,
                    Duration = TimeSpan.FromMilliseconds(500),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };

                StrengthIndicator.BeginAnimation(FrameworkElement.WidthProperty, animation);
            }
        }

        private void UpdateStrengthLevelIndicators(int score)
        {
            WeakIndicator.Background = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));
            FairIndicator.Background = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));
            GoodIndicator.Background = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));
            StrongIndicator.Background = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB));

            if (score >= 25) WeakIndicator.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
            if (score >= 50) FairIndicator.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
            if (score >= 75) GoodIndicator.Background = new SolidColorBrush(Color.FromRgb(0xEA, 0xB3, 0x08));
            if (score >= 90) StrongIndicator.Background = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
        }

        private int CalculatePasswordStrength(int length, int characterTypes)
        {
            int score = 0;

            if (length >= 8) score += 20;
            if (length >= 12) score += 15;
            if (length >= 16) score += 10;
            if (length >= 20) score += 5;

            score += characterTypes * 15;

            return Math.Min(score, 100);
        }

        private void UpdateStrengthDisplay(int score)
        {
            StrengthPercentage.Text = $"{score}%";

            if (score < 30)
            {
                StrengthLabel.Text = "Weak";
                StrengthLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)); // Red
                StrengthIndicator.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
                StrengthIcon.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
            }
            else if (score < 60)
            {
                StrengthLabel.Text = "Fair";
                StrengthLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)); // Orange
                StrengthIndicator.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
                StrengthIcon.Background = new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B));
            }
            else if (score < 80)
            {
                StrengthLabel.Text = "Good";
                StrengthLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xB3, 0x08)); // Yellow
                StrengthIndicator.Background = new SolidColorBrush(Color.FromRgb(0xEA, 0xB3, 0x08));
                StrengthIcon.Background = new SolidColorBrush(Color.FromRgb(0xEA, 0xB3, 0x08));
            }
            else
            {
                StrengthLabel.Text = "Very Strong";
                StrengthLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)); // Green
                StrengthIndicator.Background = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
                StrengthIcon.Background = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
            }
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(GeneratedPasswordTextBox.Text) &&
                    GeneratedPasswordTextBox.Text != "Click 'Generate Password' to create a secure password")
                {
                    string passwordToCopy = GeneratedPasswordTextBox.Text;
                    Clipboard.SetText(passwordToCopy);

                    MainWindow? mainWindow = Window.GetWindow(this) as MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.StartClipboardClearTimer(passwordToCopy);
                    }

                    var button = sender as Button;
                    var originalBrush = button?.Foreground;
                    if (button != null)
                    {
                        button.Foreground = new SolidColorBrush(Color.FromRgb(0xA3, 0xBE, 0x8C));
                        var timer = new System.Windows.Threading.DispatcherTimer();
                        timer.Interval = TimeSpan.FromSeconds(2);
                        timer.Tick += (s, args) =>
                        {
                            button.Foreground = originalBrush;
                            timer.Stop();
                        };
                        timer.Start();
                    }

                    MessageBox.Show("Password copied to clipboard!", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Please generate a password first.", "No Password",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error copying password: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}