using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CyberVault.View;

namespace CyberVault.Viewmodel
{
    public partial class AuthenticatorControl : UserControl
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

        public AuthenticatorControl()
        {
            InitializeComponent();
        }

        public void Initialize(string username, byte[] encryptionKey)
        {
            _username = username;
            _encryptionKey = encryptionKey;


            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
            _authFilePath = Path.Combine(cyberVaultPath, $"{username}_authenticators.enc");

            _authenticatorEntries = new ObservableCollection<AuthenticatorEntry>();
            AuthenticatorListView.ItemsSource = _authenticatorEntries;

            LoadAuthenticators();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void LoadAuthenticators()
        {
            try
            {
                if (!File.Exists(_authFilePath))
                    return;

                byte[] encryptedData = File.ReadAllBytes(_authFilePath);

                byte[] iv = new byte[16];
                Array.Copy(encryptedData, 0, iv, 0, 16);

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

                byte[] iv = AesEncryption.GenerateIV();

                byte[] encryptedData = AesEncryption.Encrypt(sb.ToString(), _encryptionKey, iv);

                byte[] dataToSave = new byte[iv.Length + encryptedData.Length];
                Array.Copy(iv, 0, dataToSave, 0, iv.Length);
                Array.Copy(encryptedData, 0, dataToSave, iv.Length, encryptedData.Length);

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

                AuthenticatorListView.Items.Refresh();
            }
        }

        private string GenerateTOTPCode(string secret)
        {
            try
            {
                secret = secret.Replace(" ", "").ToUpperInvariant();

                long timeSlice = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() / 30;

                byte[] secretBytes = Base32Decode(secret);

                byte[] timeBytes = BitConverter.GetBytes(timeSlice);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(timeBytes);

                byte[] counterBytes = new byte[8];
                int byteLength = Math.Min(timeBytes.Length, 8);
                Array.Copy(timeBytes, timeBytes.Length - byteLength, counterBytes, 8 - byteLength, byteLength);

                using (var hmac = new System.Security.Cryptography.HMACSHA1(secretBytes))
                {
                    byte[] hash = hmac.ComputeHash(counterBytes);

                    int offset = hash[hash.Length - 1] & 0x0F;
                    int binary = ((hash[offset] & 0x7F) << 24) |
                                ((hash[offset + 1] & 0xFF) << 16) |
                                ((hash[offset + 2] & 0xFF) << 8) |
                                (hash[offset + 3] & 0xFF);

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
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

            input = input.TrimEnd('=');

            int outputLength = input.Length * 5 / 8;
            byte[] result = new byte[outputLength];

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
            var dialog = new AddAuthenticatorDialog();
            dialog.Owner = Window.GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                string name = dialog.AuthenticatorName;
                string secret = dialog.AuthenticatorSecret.Replace(" ", "").ToUpperInvariant();

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

                _authenticatorEntries.Add(new AuthenticatorEntry(name, secret));

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
    }

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
            Height = 320;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(236, 239, 244));
            BorderThickness = new Thickness(1);
            BorderBrush = new SolidColorBrush(Color.FromRgb(76, 86, 106));
            

            Grid grid = new Grid();
            Content = grid;
            grid.Margin = new Thickness(20);

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            TextBlock titleBlock = new TextBlock
            {
                Text = "Add New Authenticator",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64))
            };
            grid.Children.Add(titleBlock);
            Grid.SetRow(titleBlock, 0);

            TextBlock nameLabel = new TextBlock
            {
                Text = "Service Name:",
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64))
            };
            grid.Children.Add(nameLabel);
            Grid.SetRow(nameLabel, 1);

            nameTextBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(8),
                FontSize = 14,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(76, 86, 106))
            };
            grid.Children.Add(nameTextBox);
            Grid.SetRow(nameTextBox, 2);

            TextBlock secretLabel = new TextBlock
            {
                Text = "Secret Key:",
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64))
            };
            grid.Children.Add(secretLabel);
            Grid.SetRow(secretLabel, 3);

            secretTextBox = new TextBox
            {
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(8),
                FontSize = 14,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(76, 86, 106))
            };
            grid.Children.Add(secretTextBox);
            Grid.SetRow(secretTextBox, 4);

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            grid.Children.Add(buttonPanel);
            Grid.SetRow(buttonPanel, 5);

            Button okButton = new Button
            {
                Content = "Save",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(136, 192, 208)),
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64)),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                IsDefault = true
            };
            okButton.Click += OkButton_Click;
            buttonPanel.Children.Add(okButton);

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Padding = new Thickness(20, 8, 20, 8),
                Background = new SolidColorBrush(Color.FromRgb(67, 76, 94)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
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