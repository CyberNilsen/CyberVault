using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CyberVault.View
{
    public partial class TwoFactorAuthenticator : Window
    {
        private string _username;
        private byte[] _encryptionKey;
        private string _authFilePath;
        private ObservableCollection<AuthenticatorEntry> _authenticatorEntries;
        private DispatcherTimer _timer;

        public class AuthenticatorEntry
        {
            public string Name { get; set; }
            public string Secret { get; set; }
            public string CurrentCode { get; set; }
            public int SecondsRemaining { get; set; }
            public double ProgressValue => (30 - SecondsRemaining) / 30.0 * 100;

            public AuthenticatorEntry(string name, string secret)
            {
                Name = name;
                Secret = secret;
                CurrentCode = string.Empty;
                SecondsRemaining = 0;
            }
        }

        public TwoFactorAuthenticator(string username)
        {
            InitializeComponent();

            _username = username;
            Title = $"2FA Authenticator - {username}";

            // Get encryption key from Login
            _encryptionKey = Login.GetEncryptionKey();

            // Set up file path
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
            _authFilePath = Path.Combine(cyberVaultPath, $"{username}_authenticators.enc");

            // Initialize collections
            _authenticatorEntries = new ObservableCollection<AuthenticatorEntry>();
            AuthenticatorListView.ItemsSource = _authenticatorEntries;

            // Load saved authenticators
            LoadAuthenticators();

            // Start timer to update codes
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        public void SetEncryptionKey(byte[] key)
        {
            _encryptionKey = key;
        }

        private void LoadAuthenticators()
        {
            try
            {
                if (!File.Exists(_authFilePath))
                    return;

                byte[] encryptedData = File.ReadAllBytes(_authFilePath);

                // Generate IV from the first 16 bytes of the file
                byte[] iv = new byte[16];
                Array.Copy(encryptedData, 0, iv, 0, 16);

                // The rest is the actual encrypted data
                byte[] actualEncryptedData = new byte[encryptedData.Length - 16];
                Array.Copy(encryptedData, 16, actualEncryptedData, 0, actualEncryptedData.Length);

                string decryptedData = AesEncryption.Decrypt(actualEncryptedData, _encryptionKey, iv);

                string[] entries = decryptedData.Split(new[] { "||ENTRY||" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string entry in entries)
                {
                    string[] parts = entry.Split(new[] { "||NAME||" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        string name = parts[0];
                        string secret = parts[1];

                        _authenticatorEntries.Add(new AuthenticatorEntry(name, secret));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading authenticators: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAuthenticators()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");

                // Create directory if it doesn't exist
                if (!Directory.Exists(cyberVaultPath))
                    Directory.CreateDirectory(cyberVaultPath);

                StringBuilder sb = new StringBuilder();

                foreach (var entry in _authenticatorEntries)
                {
                    if (sb.Length > 0)
                        sb.Append("||ENTRY||");

                    sb.Append(entry.Name);
                    sb.Append("||NAME||");
                    sb.Append(entry.Secret);
                }

                // Generate IV for encryption
                byte[] iv = AesEncryption.GenerateIV();

                // Encrypt the data
                byte[] encryptedData = AesEncryption.Encrypt(sb.ToString(), _encryptionKey, iv);

                // Combine IV and encrypted data
                byte[] dataToSave = new byte[iv.Length + encryptedData.Length];
                Array.Copy(iv, 0, dataToSave, 0, iv.Length);
                Array.Copy(encryptedData, 0, dataToSave, iv.Length, encryptedData.Length);

                // Save to file
                File.WriteAllBytes(_authFilePath, dataToSave);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving authenticators: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var entry in _authenticatorEntries)
            {
                entry.SecondsRemaining = 30 - (int)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond % 30);
                entry.CurrentCode = GenerateTOTPCode(entry.Secret);

                // Force UI update
                AuthenticatorListView.Items.Refresh();
            }
        }

        private string GenerateTOTPCode(string secret)
        {
            try
            {
                // Clean up the secret key (remove spaces)
                secret = secret.Replace(" ", "").ToUpperInvariant();

                // Time slice: Unix timestamp divided by 30 seconds
                long timeSlice = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() / 30;

                // Convert secret to byte array (Base32 decoding)
                byte[] secretBytes = Base32Decode(secret);

                // Convert time slice to byte array (Big endian)
                byte[] timeBytes = BitConverter.GetBytes(timeSlice);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(timeBytes);

                // Truncate or pad to 8 bytes
                byte[] counterBytes = new byte[8];
                int byteLength = Math.Min(timeBytes.Length, 8);
                Array.Copy(timeBytes, timeBytes.Length - byteLength, counterBytes, 8 - byteLength, byteLength);

                // Compute HMAC-SHA1
                using (var hmac = new System.Security.Cryptography.HMACSHA1(secretBytes))
                {
                    byte[] hash = hmac.ComputeHash(counterBytes);

                    // Dynamic truncation
                    int offset = hash[hash.Length - 1] & 0x0F;
                    int binary = ((hash[offset] & 0x7F) << 24) |
                                ((hash[offset + 1] & 0xFF) << 16) |
                                ((hash[offset + 2] & 0xFF) << 8) |
                                (hash[offset + 3] & 0xFF);

                    // Generate 6-digit code
                    int otp = binary % 1000000;
                    return otp.ToString("D6");
                }
            }
            catch
            {
                return "------";
            }
        }

        private byte[] Base32Decode(string input)
        {
            // Base32 character set (RFC 4648)
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

            // Remove padding if present
            input = input.TrimEnd('=');

            // Calculate output length
            int outputLength = input.Length * 5 / 8;
            byte[] result = new byte[outputLength];

            // Process in 5-byte chunks
            int buffer = 0;
            int bitsLeft = 0;
            int outputIndex = 0;

            foreach (char c in input)
            {
                int value = base32Chars.IndexOf(c);
                if (value < 0)
                    throw new ArgumentException("Invalid base32 character in input");

                buffer <<= 5;
                buffer |= value;
                bitsLeft += 5;

                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    result[outputIndex++] = (byte)(buffer >> bitsLeft);
                }
            }

            return result;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Show add dialog
            var dialog = new AddAuthenticatorDialog();
            if (dialog.ShowDialog() == true)
            {
                string name = dialog.AuthenticatorName;
                string secret = dialog.AuthenticatorSecret.Replace(" ", "").ToUpperInvariant();

                // Validate secret (basic check if it's valid base32)
                try
                {
                    Base32Decode(secret);
                }
                catch
                {
                    MessageBox.Show("Invalid secret key. Please enter a valid base32 encoded secret.",
                        "Invalid Secret", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Add to collection
                _authenticatorEntries.Add(new AuthenticatorEntry(name, secret));

                // Save to file
                SaveAuthenticators();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (AuthenticatorListView.SelectedItem is AuthenticatorEntry selectedEntry)
            {
                var result = MessageBox.Show($"Are you sure you want to remove '{selectedEntry.Name}'?",
                    "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _authenticatorEntries.Remove(selectedEntry);
                    SaveAuthenticators();
                }
            }
            else
            {
                MessageBox.Show("Please select an authenticator to remove.",
                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AuthenticatorEntry entry)
            {
                Clipboard.SetText(entry.CurrentCode);

                // Visual feedback for copy
                var originalBrush = button.Background;
                button.Background = new SolidColorBrush(Colors.Green);

                Task.Delay(500).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        button.Background = originalBrush;
                    });
                });
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            VaultDashboard dashboard = new VaultDashboard(_username);
            dashboard.SetEncryptionKey(_encryptionKey);
            dashboard.Show();
            Close();
        }
    }

    // Dialog for adding new authenticator
    public class AddAuthenticatorDialog : Window
    {
        private TextBox nameTextBox;
        private TextBox secretTextBox;

        public string AuthenticatorName { get; private set; }
        public string AuthenticatorSecret { get; private set; }

        public AddAuthenticatorDialog()
        {
            Title = "Add New Authenticator";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;

            Grid grid = new Grid();
            Content = grid;

            // Row definitions
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Name label
            TextBlock nameLabel = new TextBlock
            {
                Text = "Service Name:",
                Margin = new Thickness(10, 10, 10, 5)
            };
            grid.Children.Add(nameLabel);
            Grid.SetRow(nameLabel, 0);

            // Name textbox
            nameTextBox = new TextBox
            {
                Margin = new Thickness(10, 5, 10, 10),
                Padding = new Thickness(5),
                FontSize = 14
            };
            grid.Children.Add(nameTextBox);
            Grid.SetRow(nameTextBox, 1);

            // Secret label
            TextBlock secretLabel = new TextBlock
            {
                Text = "Secret Key:",
                Margin = new Thickness(10, 5, 10, 5)
            };
            grid.Children.Add(secretLabel);
            Grid.SetRow(secretLabel, 2);

            // Secret textbox
            secretTextBox = new TextBox
            {
                Margin = new Thickness(10, 5, 10, 10),
                Padding = new Thickness(5),
                FontSize = 14
            };
            grid.Children.Add(secretTextBox);
            Grid.SetRow(secretTextBox, 3);

            // Buttons
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 10, 10)
            };
            grid.Children.Add(buttonPanel);
            Grid.SetRow(buttonPanel, 4);

            Button okButton = new Button
            {
                Content = "OK",
                Padding = new Thickness(20, 5, 20, 5),
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += OkButton_Click;
            buttonPanel.Children.Add(okButton);

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Padding = new Thickness(20, 5, 20, 5),
                IsCancel = true
            };
            cancelButton.Click += CancelButton_Click;
            buttonPanel.Children.Add(cancelButton);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("Please enter a service name.", "Missing Information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(secretTextBox.Text))
            {
                MessageBox.Show("Please enter a secret key.", "Missing Information",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AuthenticatorName = nameTextBox.Text;
            AuthenticatorSecret = secretTextBox.Text;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}