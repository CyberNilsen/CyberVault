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
        private string ?_username;
        private byte[] ?_encryptionKey;
        private string ?_authFilePath;
        private ObservableCollection<AuthenticatorEntry> ?_authenticatorEntries;
        private DispatcherTimer ?_timer;

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
            _timer.Tick += Timer_Tick!;
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

                string decryptedData = AesEncryption.Decrypt(actualEncryptedData, _encryptionKey!, iv);

                string[] entries = decryptedData.Split(new[] { "||ENTRY||" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string entry in entries)
                {
                    string[] parts = entry.Split(new[] { "||NAME||" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        string name = parts[0];
                        string secret = parts[1];

                        _authenticatorEntries!.Add(new AuthenticatorEntry(name, secret));
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

                foreach (var entry in _authenticatorEntries!)
                {
                    if (sb.Length > 0)
                        sb.Append("||ENTRY||");

                    sb.Append(entry.Name);
                    sb.Append("||NAME||");
                    sb.Append(entry.Secret);
                }

                byte[] iv = AesEncryption.GenerateIV();

                byte[] encryptedData = AesEncryption.Encrypt(sb.ToString(), _encryptionKey!, iv);

                byte[] dataToSave = new byte[iv.Length + encryptedData.Length];
                Array.Copy(iv, 0, dataToSave, 0, iv.Length);
                Array.Copy(encryptedData, 0, dataToSave, iv.Length, encryptedData.Length);

                File.WriteAllBytes(_authFilePath!, dataToSave);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving authenticators: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var entry in _authenticatorEntries!)
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
                string name = dialog.AuthenticatorName!;
                string secret = dialog.AuthenticatorSecret!.Replace(" ", "").ToUpperInvariant();

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

                _authenticatorEntries!.Add(new AuthenticatorEntry(name, secret));

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
                    _authenticatorEntries!.Remove(selectedEntry);
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

        public string ?AuthenticatorName { get; private set; }
        public string ?AuthenticatorSecret { get; private set; }

        public AddAuthenticatorDialog()
        {
            Title = "Add New Authenticator";
            Width = 420;
            Height = 390;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(236, 239, 244));
            BorderThickness = new Thickness(1);
            BorderBrush = new SolidColorBrush(Color.FromRgb(236, 239, 244));

            Grid mainGrid = new Grid();
            Content = mainGrid;
            mainGrid.Margin = new Thickness(25);

            Border contentBorder = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                BorderBrush = new SolidColorBrush(Color.FromRgb(216, 222, 233)),
                BorderThickness = new Thickness(1)
            };

            mainGrid.Children.Add(contentBorder);

            Grid grid = new Grid();
            contentBorder.Child = grid;

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            StackPanel headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 20)
            };
            grid.Children.Add(headerPanel);
            Grid.SetRow(headerPanel, 0);

            TextBlock iconBlock = new TextBlock
            {
                Text = "🔐",
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            headerPanel.Children.Add(iconBlock);

            TextBlock titleBlock = new TextBlock
            {
                Text = "Add New Authenticator",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64)),

            };
            headerPanel.Children.Add(titleBlock);

            StackPanel namePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };
            grid.Children.Add(namePanel);
            Grid.SetRow(namePanel, 1);

            TextBlock nameIcon = new TextBlock
            {
                Text = "🏢",
                FontSize = 14,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            namePanel.Children.Add(nameIcon);

            TextBlock nameLabel = new TextBlock
            {
                Text = "Service Name:",
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64))
            };
            namePanel.Children.Add(nameLabel);

            Border nameBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(236, 239, 244)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15)
            };
            grid.Children.Add(nameBorder);
            Grid.SetRow(nameBorder, 2);

            nameTextBox = new TextBox
            {
                Padding = new Thickness(10),
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(236, 239, 244))
            };
            nameBorder.Child = nameTextBox;

            StackPanel secretPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };
            grid.Children.Add(secretPanel);
            Grid.SetRow(secretPanel, 3);

            TextBlock secretIcon = new TextBlock
            {
                Text = "🔑",
                FontSize = 14,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            secretPanel.Children.Add(secretIcon);

            TextBlock secretLabel = new TextBlock
            {
                Text = "Secret Key:",
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64))
            };
            secretPanel.Children.Add(secretLabel);

            Border secretBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(236, 239, 244)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15)
            };
            grid.Children.Add(secretBorder);
            Grid.SetRow(secretBorder, 4);

            secretTextBox = new TextBox
            {
                Padding = new Thickness(10),
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(236, 239, 244))
            };
            secretBorder.Child = secretTextBox;

            TextBlock hintBlock = new TextBlock
            {
                Text = "Enter the secret key exactly as provided by the service",
                FontSize = 11,
                Margin = new Thickness(5, -10, 0, 15),
                Foreground = new SolidColorBrush(Color.FromRgb(94, 129, 172)),
                FontStyle = FontStyles.Italic
            };
            grid.Children.Add(hintBlock);
            Grid.SetRow(hintBlock, 5);

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            grid.Children.Add(buttonPanel);
            Grid.SetRow(buttonPanel, 5);

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(229, 233, 240)),
                Foreground = new SolidColorBrush(Color.FromRgb(59, 66, 82)),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Medium,
                IsCancel = true
            };

            cancelButton.Template = CreateButtonTemplateWithHover(
                new SolidColorBrush(Color.FromRgb(229, 233, 240)),
                new SolidColorBrush(Color.FromRgb(216, 222, 233)));
            cancelButton.Click += CancelButton_Click;
            buttonPanel.Children.Add(cancelButton);

            Button okButton = new Button
            {
                Content = "Save",
                Padding = new Thickness(25, 10, 25, 10),
                Background = new SolidColorBrush(Color.FromRgb(129, 161, 193)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Medium,
                IsDefault = true
            };

            okButton.Template = CreateButtonTemplateWithHover(
                new SolidColorBrush(Color.FromRgb(129, 161, 193)),
                new SolidColorBrush(Color.FromRgb(94, 129, 172)));
            okButton.Click += OkButton_Click;
            buttonPanel.Children.Add(okButton);
        }

        private ControlTemplate CreateButtonTemplateWithHover(SolidColorBrush normalBrush, SolidColorBrush hoverBrush)
        {
            ControlTemplate template = new ControlTemplate(typeof(Button));

            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetValue(Border.BackgroundProperty, normalBrush);
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(0));
            borderFactory.Name = "border";

            FrameworkElementFactory contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.MarginProperty, new Thickness(8));

            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;

            Trigger hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter { Property = Border.BackgroundProperty, Value = hoverBrush, TargetName = "border" });

            template.Triggers.Add(hoverTrigger);

            return template;
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