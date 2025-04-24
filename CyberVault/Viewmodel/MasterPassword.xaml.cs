using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CyberVault.Viewmodel
{
    public partial class MasterPassword : Window
    {
        private TextBox oldPasswordVisible;
        private PasswordBox oldPasswordInput;
        private TextBox newPasswordVisible;
        private PasswordBox newPasswordInput;
        private TextBox confirmPasswordVisible;
        private PasswordBox confirmPasswordInput;
        private Button toggleOldPasswordButton;
        private Button toggleNewPasswordButton;
        private Button toggleConfirmPasswordButton;
        private ProgressBar passwordStrengthBar;
        private TextBlock passwordStrengthMessage;
        private bool oldPasswordVisible_ = false;
        private bool newPasswordVisible_ = false;
        private bool confirmPasswordVisible_ = false;
        private string username;
        private byte[] encryptionKey;

        public string? OldPassword { get; private set; }
        public string? NewPassword { get; private set; }

        public MasterPassword(string username, byte[] encryptionKey)
        {
            this.username = username;
            this.encryptionKey = encryptionKey;

            Title = "Change Master Password";
            Width = 420;
            Height = 560;
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
                Text = "🔑",
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            headerPanel.Children.Add(iconBlock);

            TextBlock titleBlock = new TextBlock
            {
                Text = "Change Master Password",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64)),
            };
            headerPanel.Children.Add(titleBlock);

            StackPanel oldPasswordPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };
            grid.Children.Add(oldPasswordPanel);
            Grid.SetRow(oldPasswordPanel, 1);

            TextBlock oldPasswordIcon = new TextBlock
            {
                Text = "🔒",
                FontSize = 14,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            oldPasswordPanel.Children.Add(oldPasswordIcon);

            TextBlock oldPasswordLabel = new TextBlock
            {
                Text = "Current Password:",
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64))
            };
            oldPasswordPanel.Children.Add(oldPasswordLabel);

            Grid oldPasswordGrid = new Grid();
            grid.Children.Add(oldPasswordGrid);
            Grid.SetRow(oldPasswordGrid, 2);

            Border oldPasswordBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(236, 239, 244)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15)
            };
            oldPasswordGrid.Children.Add(oldPasswordBorder);

            oldPasswordInput = new PasswordBox
            {
                Padding = new Thickness(10),
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(236, 239, 244))
            };
            oldPasswordBorder.Child = oldPasswordInput;

            oldPasswordVisible = new TextBox
            {
                Padding = new Thickness(10),
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(236, 239, 244)),
                Visibility = Visibility.Collapsed
            };
            oldPasswordGrid.Children.Add(oldPasswordVisible);

            toggleOldPasswordButton = new Button
            {
                Content = "👁️",
                Width = 30,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 15),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            toggleOldPasswordButton.Click += ToggleOldPasswordButton_Click;
            oldPasswordGrid.Children.Add(toggleOldPasswordButton);

            StackPanel newPasswordPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };
            grid.Children.Add(newPasswordPanel);
            Grid.SetRow(newPasswordPanel, 3);

            TextBlock newPasswordIcon = new TextBlock
            {
                Text = "🔒",
                FontSize = 14,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            newPasswordPanel.Children.Add(newPasswordIcon);

            TextBlock newPasswordLabel = new TextBlock
            {
                Text = "New Password:",
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64))
            };
            newPasswordPanel.Children.Add(newPasswordLabel);

            Grid newPasswordGrid = new Grid();
            grid.Children.Add(newPasswordGrid);
            Grid.SetRow(newPasswordGrid, 4);

            Border newPasswordBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(236, 239, 244)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15)
            };
            newPasswordGrid.Children.Add(newPasswordBorder);

            newPasswordInput = new PasswordBox
            {
                Padding = new Thickness(10),
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(236, 239, 244))
            };
            newPasswordInput.PasswordChanged += NewPasswordInput_PasswordChanged;
            newPasswordBorder.Child = newPasswordInput;

            newPasswordVisible = new TextBox
            {
                Padding = new Thickness(10),
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(236, 239, 244)),
                Visibility = Visibility.Collapsed
            };
            newPasswordVisible.TextChanged += NewPasswordVisible_TextChanged;
            newPasswordGrid.Children.Add(newPasswordVisible);

            toggleNewPasswordButton = new Button
            {
                Content = "👁️",
                Width = 30,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 15),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            toggleNewPasswordButton.Click += ToggleNewPasswordButton_Click;
            newPasswordGrid.Children.Add(toggleNewPasswordButton);

            passwordStrengthBar = new ProgressBar
            {
                Height = 5,
                Margin = new Thickness(0, -10, 0, 15),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Foreground = new SolidColorBrush(Colors.Red)
            };
            grid.Children.Add(passwordStrengthBar);
            Grid.SetRow(passwordStrengthBar, 5);

            passwordStrengthMessage = new TextBlock
            {
                Text = "Password strength",
                FontSize = 11,
                Margin = new Thickness(5, -10, 0, 15),
                Foreground = new SolidColorBrush(Color.FromRgb(94, 129, 172)),
                FontStyle = FontStyles.Italic
            };
            grid.Children.Add(passwordStrengthMessage);
            Grid.SetRow(passwordStrengthMessage, 6);

            StackPanel confirmPasswordPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 5)
            };
            grid.Children.Add(confirmPasswordPanel);
            Grid.SetRow(confirmPasswordPanel, 7);

            TextBlock confirmPasswordIcon = new TextBlock
            {
                Text = "🔒",
                FontSize = 14,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            confirmPasswordPanel.Children.Add(confirmPasswordIcon);

            TextBlock confirmPasswordLabel = new TextBlock
            {
                Text = "Confirm New Password:",
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(46, 52, 64))
            };
            confirmPasswordPanel.Children.Add(confirmPasswordLabel);

            Grid confirmPasswordGrid = new Grid();
            grid.Children.Add(confirmPasswordGrid);
            Grid.SetRow(confirmPasswordGrid, 8);

            Border confirmPasswordBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                BorderBrush = new SolidColorBrush(Color.FromRgb(236, 239, 244)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 15)
            };
            confirmPasswordGrid.Children.Add(confirmPasswordBorder);

            confirmPasswordInput = new PasswordBox
            {
                Padding = new Thickness(10),
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(236, 239, 244))
            };
            confirmPasswordBorder.Child = confirmPasswordInput;

            confirmPasswordVisible = new TextBox
            {
                Padding = new Thickness(10),
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromRgb(236, 239, 244)),
                Visibility = Visibility.Collapsed
            };
            confirmPasswordGrid.Children.Add(confirmPasswordVisible);

            toggleConfirmPasswordButton = new Button
            {
                Content = "👁️",
                Width = 30,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 15),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            toggleConfirmPasswordButton.Click += ToggleConfirmPasswordButton_Click;
            confirmPasswordGrid.Children.Add(toggleConfirmPasswordButton);

            TextBlock noteBlock = new TextBlock
            {
                Text = "Changing your password will encrypt all of your data with the new password",
                FontSize = 11,
                Margin = new Thickness(5, 5, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(94, 129, 172)),
                TextWrapping = TextWrapping.Wrap,
                FontStyle = FontStyles.Italic
            };
            grid.Children.Add(noteBlock);
            Grid.SetRow(noteBlock, 9);

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            grid.Children.Add(buttonPanel);
            Grid.SetRow(buttonPanel, 10);

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
                Content = "Change Password",
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

        private void ToggleOldPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            oldPasswordVisible_ = !oldPasswordVisible_;

            if (oldPasswordVisible_)
            {
                oldPasswordVisible.Text = oldPasswordInput.Password;
                oldPasswordVisible.Visibility = Visibility.Visible;
                oldPasswordInput.Visibility = Visibility.Collapsed;
            }
            else
            {
                oldPasswordInput.Password = oldPasswordVisible.Text;
                oldPasswordVisible.Visibility = Visibility.Collapsed;
                oldPasswordInput.Visibility = Visibility.Visible;
            }
        }

        private void ToggleNewPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            newPasswordVisible_ = !newPasswordVisible_;

            if (newPasswordVisible_)
            {
                newPasswordVisible.Text = newPasswordInput.Password;
                newPasswordVisible.Visibility = Visibility.Visible;
                newPasswordInput.Visibility = Visibility.Collapsed;
            }
            else
            {
                newPasswordInput.Password = newPasswordVisible.Text;
                newPasswordVisible.Visibility = Visibility.Collapsed;
                newPasswordInput.Visibility = Visibility.Visible;
            }
        }

        private void ToggleConfirmPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            confirmPasswordVisible_ = !confirmPasswordVisible_;

            if (confirmPasswordVisible_)
            {
                confirmPasswordVisible.Text = confirmPasswordInput.Password;
                confirmPasswordVisible.Visibility = Visibility.Visible;
                confirmPasswordInput.Visibility = Visibility.Collapsed;
            }
            else
            {
                confirmPasswordInput.Password = confirmPasswordVisible.Text;
                confirmPasswordVisible.Visibility = Visibility.Collapsed;
                confirmPasswordInput.Visibility = Visibility.Visible;
            }
        }

        private void NewPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            string password = newPasswordInput.Password;
            int strength = CalculatePasswordStrength(password);
            AnimateProgressBar(strength);
            passwordStrengthMessage.Text = GetStrengthMessage(strength);
        }

        private void NewPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            string password = newPasswordVisible.Text;
            int strength = CalculatePasswordStrength(password);
            AnimateProgressBar(strength);
            passwordStrengthMessage.Text = GetStrengthMessage(strength);
        }

        private void AnimateProgressBar(int targetValue)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = passwordStrengthBar.Value,
                To = targetValue,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase()
            };

            passwordStrengthBar.BeginAnimation(ProgressBar.ValueProperty, animation);

            if (targetValue < 50)
                passwordStrengthBar.Foreground = new SolidColorBrush(Colors.Red);
            else if (targetValue < 81)
                passwordStrengthBar.Foreground = new SolidColorBrush(Colors.Yellow);
            else
                passwordStrengthBar.Foreground = new SolidColorBrush(Colors.Green);
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

            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]"))
                strength += 20;
            if (System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]"))
                strength += 20;
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

        private bool ValidateCurrentPassword(string oldPassword)
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string credentialsFilePath = Path.Combine(cyberVaultPath, "credentials.txt");

                if (!File.Exists(credentialsFilePath))
                    return false;

                string[] lines = File.ReadAllLines(credentialsFilePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith(username + ","))
                    {
                        string[] parts = line.Split(',');
                        if (parts.Length >= 3)
                        {
                            string saltBase64 = parts[1];
                            string hashBase64 = parts[2];

                            byte[] salt = Convert.FromBase64String(saltBase64);
                            byte[] storedHash = Convert.FromBase64String(hashBase64);

                            byte[] computedHash = HashPassword(oldPassword, salt);

                            return CompareByteArrays(computedHash, storedHash);
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }

            return true;
        }

        private byte[] HashPassword(string password, byte[] salt, int iterations = 10000, int hashSize = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(hashSize);
            }
        }

        private byte[] GenerateSalt(int saltSize = 16)
        {
            byte[] salt = new byte[saltSize];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string oldPassword = oldPasswordVisible_ ? oldPasswordVisible.Text : oldPasswordInput.Password;
            string newPassword = newPasswordVisible_ ? newPasswordVisible.Text : newPasswordInput.Password;
            string confirmPassword = confirmPasswordVisible_ ? confirmPasswordVisible.Text : confirmPasswordInput.Password;

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("All fields are required.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("New passwords do not match.", "Password Mismatch", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateCurrentPassword(oldPassword))
            {
                MessageBox.Show("Current password is incorrect.", "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (oldPassword == newPassword)
            {
                MessageBox.Show("New password must be different from current password.", "Password Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int strength = CalculatePasswordStrength(newPassword);
            if (strength < 40)
            {
                var result = MessageBox.Show("Your password is weak. Are you sure you want to use this password?",
                    "Weak Password", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                    return;
            }

            try
            {
                if (ChangePasswordInCredentialsFile(oldPassword, newPassword))
                {
                    OldPassword = oldPassword;
                    NewPassword = newPassword;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ChangePasswordInCredentialsFile(string oldPassword, string newPassword)
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string credentialsFilePath = Path.Combine(cyberVaultPath, "credentials.txt");

                if (!File.Exists(credentialsFilePath))
                    return false;

                string[] lines = File.ReadAllLines(credentialsFilePath);
                List<string> updatedLines = new List<string>();
                bool found = false;

                foreach (string line in lines)
                {
                    if (line.StartsWith(username + ","))
                    {
                        byte[] newSalt = GenerateSalt();
                        byte[] newHashedPassword = HashPassword(newPassword, newSalt);
                        string newSaltBase64 = Convert.ToBase64String(newSalt);
                        string newHashBase64 = Convert.ToBase64String(newHashedPassword);

                        updatedLines.Add($"{username},{newSaltBase64},{newHashBase64}");
                        found = true;
                    }
                    else
                    {
                        updatedLines.Add(line);
                    }
                }

                if (found)
                {
                    File.WriteAllLines(credentialsFilePath, updatedLines);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating credentials file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}