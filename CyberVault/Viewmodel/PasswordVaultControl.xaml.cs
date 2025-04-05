using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace CyberVault.View
{
    public partial class PasswordVaultControl : UserControl
    {
        private string _username;
        private byte[] _encryptionKey;
        private TextBox passwordTextBox;
        public ObservableCollection<PasswordItem> SavedPasswords { get; set; }
        private MainWindow mainWindow;
        public bool IsUACActive { get; set; }

        public PasswordVaultControl(MainWindow mw, string username, byte[] encryptionKey)
        {
            InitializeComponent();
            mainWindow = mw;
            _username = username;
            _encryptionKey = encryptionKey;
            SavedPasswords = new ObservableCollection<PasswordItem>();
            DataContext = this;
            LoadPasswords();
            InitializePasswordVisibilityToggle();
            CheckWelcomeGridVisibility();
        }

        private void InitializePasswordVisibilityToggle()
        {
            passwordTextBox = new TextBox
            {
                Visibility = Visibility.Collapsed,
                BorderThickness = new Thickness(0),
                Background = NewPasswordBox.Background,
                Foreground = NewPasswordBox.Foreground,
                FontSize = NewPasswordBox.FontSize,
                Padding = NewPasswordBox.Padding,
                Margin = NewPasswordBox.Margin,
                VerticalAlignment = NewPasswordBox.VerticalAlignment,
                HorizontalAlignment = NewPasswordBox.HorizontalAlignment
            };
            var grid = NewPasswordBox.Parent as Grid;
            if (grid != null)
            {
                Grid.SetRow(passwordTextBox, Grid.GetRow(NewPasswordBox));
                Grid.SetColumn(passwordTextBox, Grid.GetColumn(NewPasswordBox));
                grid.Children.Add(passwordTextBox);
            }
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (NewPasswordBox.Visibility == Visibility.Visible)
            {
                PlainTextPassword.Text = NewPasswordBox.Password;
                NewPasswordBox.Visibility = Visibility.Collapsed;
                PlainTextPassword.Visibility = Visibility.Visible;
            }
            else
            {
                NewPasswordBox.Password = PlainTextPassword.Text;
                PlainTextPassword.Visibility = Visibility.Collapsed;
                NewPasswordBox.Visibility = Visibility.Visible;
            }
        }

        public void SetEncryptionKey(byte[] key)
        {
            _encryptionKey = key;
        }

        private void LoadPasswords()
        {
            try
            {
                if (_encryptionKey == null)
                {
                    _encryptionKey = LoginControl.CurrentEncryptionKey;
                    if (_encryptionKey == null)
                    {
                        MessageBox.Show("Session expired. Please log in again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        LogOut();
                        return;
                    }
                }
                SavedPasswords.Clear();
                List<PasswordItem> passwords = PasswordStorage.LoadPasswords(_username, _encryptionKey);
                foreach (var password in passwords)
                {
                    SavedPasswords.Add(password);
                }
                CheckWelcomeGridVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading passwords: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckWelcomeGridVisibility()
        {
            if (SavedPasswords.Count == 0)
            {
                WelcomeGrid.Visibility = Visibility.Visible;
                CreatePasswordGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                WelcomeGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveAllPasswords()
        {
            try
            {
                if (_encryptionKey == null)
                {
                    MessageBox.Show("Session expired. Please log in again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    LogOut();
                    return;
                }
                List<PasswordItem> passwords = SavedPasswords.ToList();
                PasswordStorage.SavePasswords(passwords, _username, _encryptionKey);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving passwords: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogOut()
        {
            mainWindow.Navigate(new LoginControl(mainWindow));
        }

        private void CreateSavePassword_Click(object sender, RoutedEventArgs e)
        {
            OpenPasswordForm(null);
            WelcomeGrid.Visibility = Visibility.Collapsed;
        }

        private void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            string password = NewPasswordBox.Visibility == Visibility.Visible ? NewPasswordBox.Password : PlainTextPassword.Text;
            var selectedItem = PasswordListBox.SelectedItem as PasswordItem;
            if (selectedItem != null)
            {
                selectedItem.Name = PasswordNameTextBox.Text;
                selectedItem.Website = WebsiteTextBox.Text;
                selectedItem.Email = EmailTextBox.Text;
                selectedItem.Username = UsernameTextBox.Text;
                selectedItem.Password = password;
                SaveAllPasswords();
                MessageBox.Show("Password updated!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadPasswords();
            }
            else
            {
                var newItem = new PasswordItem
                {
                    Name = PasswordNameTextBox.Text,
                    Website = WebsiteTextBox.Text,
                    Email = EmailTextBox.Text,
                    Username = UsernameTextBox.Text,
                    Password = password
                };
                SavedPasswords.Add(newItem);
                SaveAllPasswords();
                MessageBox.Show("Password saved!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            CloseCreatePasswordGrid();
        }

        private void DeletePassword_Click(object sender, RoutedEventArgs e)
        {
            ShowDeleteConfirmationGrid();
        }

        private void CloseCreatePassword_Click(object sender, RoutedEventArgs e)
        {
            CloseCreatePasswordGrid();
        }

        private void CloseCreatePasswordGrid()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
            fadeOut.Completed += (s, a) => {
                CreatePasswordGrid.Visibility = Visibility.Collapsed;
                CheckWelcomeGridVisibility();
            };
            CreatePasswordGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void ShowDeleteConfirmationGrid()
        {
            IsUACActive = true;
            UACOverlay.Visibility = Visibility.Visible;
            DeleteConfirmationGrid.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            DeleteConfirmationGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            UACOverlay.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void HideDeleteConfirmationGrid()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
            fadeOut.Completed += (s, a) => {
                DeleteConfirmationGrid.Visibility = Visibility.Collapsed;
                UACOverlay.Visibility = Visibility.Collapsed;
                IsUACActive = false;
            };
            DeleteConfirmationGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            UACOverlay.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void ConfirmDeleteYes_Click(object sender, RoutedEventArgs e)
        {
            var selected = PasswordListBox.SelectedItem as PasswordItem;
            if (selected != null)
            {
                SavedPasswords.Remove(selected);
                SaveAllPasswords();
                MessageBox.Show("Password deleted!", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            HideDeleteConfirmationGrid();
            CloseCreatePasswordGrid();
        }

        private void ConfirmDeleteNo_Click(object sender, RoutedEventArgs e)
        {
            HideDeleteConfirmationGrid();
        }

        private void PasswordListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = PasswordListBox.SelectedItem as PasswordItem;
            if (selected != null)
            {
                OpenPasswordForm(selected);
                WelcomeGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void OpenPasswordForm(PasswordItem item)
        {
            CreatePasswordGrid.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            CreatePasswordGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            PasswordListBox.SelectedItem = item;
            PlainTextPassword.Visibility = Visibility.Collapsed;
            NewPasswordBox.Visibility = Visibility.Visible;
            if (item != null)
            {
                PasswordNameTextBox.Text = item.Name;
                WebsiteTextBox.Text = item.Website;
                EmailTextBox.Text = item.Email;
                UsernameTextBox.Text = item.Username;
                NewPasswordBox.Password = item.Password;
                PlainTextPassword.Text = item.Password;
            }
            else
            {
                PasswordNameTextBox.Text = "";
                WebsiteTextBox.Text = "";
                EmailTextBox.Text = "";
                UsernameTextBox.Text = "";
                NewPasswordBox.Password = "";
                PlainTextPassword.Text = "";
            }
        }
    }
}