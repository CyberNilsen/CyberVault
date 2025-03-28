﻿using System;
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
    public partial class PasswordVault : Window
    {
        private string _username;
        private byte[] _encryptionKey;
        private TextBox passwordTextBox;
        public ObservableCollection<PasswordItem> SavedPasswords { get; set; }

        public PasswordVault(string username)
        {
            InitializeComponent();
            _username = username;
            SavedPasswords = new ObservableCollection<PasswordItem>();

            this.DataContext = this;

            LoadPasswords();
            InitializePasswordVisibilityToggle();
        }

        private void InitializePasswordVisibilityToggle()
        {
            // Create the TextBox for showing the password in clear text
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

            // Add the TextBox next to the PasswordBox
            // Note: You need to add this to your XAML grid that contains the NewPasswordBox
            var grid = NewPasswordBox.Parent as Grid;
            if (grid != null)
            {
                Grid.SetRow(passwordTextBox, Grid.GetRow(NewPasswordBox));
                Grid.SetColumn(passwordTextBox, Grid.GetColumn(NewPasswordBox));
                grid.Children.Add(passwordTextBox);
            }
        }

        private void TogglePasswordVisibility_Click(object sender, MouseButtonEventArgs e)
        {
            if (NewPasswordBox.Visibility == Visibility.Visible)
            {
                // Show password as plain text
                PlainTextPassword.Text = NewPasswordBox.Password;
                NewPasswordBox.Visibility = Visibility.Collapsed;
                PlainTextPassword.Visibility = Visibility.Visible;
            }
            else
            {
                // Hide password (show dots)
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
                // Use the encryption key passed to this window
                if (_encryptionKey == null)
                {
                    // As a fallback, try to get from Login
                    _encryptionKey = Login.GetEncryptionKey();

                    if (_encryptionKey == null)
                    {
                        MessageBox.Show("Session expired. Please log in again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        LogOut();
                        return;
                    }
                }

                // Clear existing passwords
                SavedPasswords.Clear();

                // Load passwords from encrypted file
                List<PasswordItem> passwords = PasswordStorage.LoadPasswords(_username, _encryptionKey);

                // Add loaded passwords to observable collection
                foreach (var password in passwords)
                {
                    SavedPasswords.Add(password);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading passwords: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAllPasswords()
        {
            try
            {
                // Use the encryption key passed to this window
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
            Login loginWindow = new Login();
            loginWindow.Show();
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            VaultDashboard dashboard = new VaultDashboard(_username);
            dashboard.SetEncryptionKey(_encryptionKey);
            dashboard.Show();
            this.Close();
        }

        private void CreateSavePassword_Click(object sender, RoutedEventArgs e)
        {
            OpenPasswordForm(null);
        }

        private void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            // Ensure we get the password from whichever control is currently visible
            string password = NewPasswordBox.Visibility == Visibility.Visible ?
                NewPasswordBox.Password : passwordTextBox.Text;

            var selectedItem = PasswordListBox.SelectedItem as PasswordItem;

            if (selectedItem != null)
            {
                // Update existing password
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
                // Create new password
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
            fadeOut.Completed += (s, a) => CreatePasswordGrid.Visibility = Visibility.Collapsed;
            CreatePasswordGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private void ShowDeleteConfirmationGrid()
        {
            DeleteConfirmationGrid.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            DeleteConfirmationGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        private void HideDeleteConfirmationGrid()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
            fadeOut.Completed += (s, a) => DeleteConfirmationGrid.Visibility = Visibility.Collapsed;
            DeleteConfirmationGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
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
            }
        }

        private void OpenPasswordForm(PasswordItem item)
        {
            CreatePasswordGrid.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            CreatePasswordGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            PasswordListBox.SelectedItem = item;

            // Ensure password is hidden when opening the form
            passwordTextBox.Visibility = Visibility.Collapsed;
            NewPasswordBox.Visibility = Visibility.Visible;

            // Reset the eye icon
            if (EyeIcon != null)
            {
                EyeIcon.Source = new BitmapImage(new Uri("/Images/eyes.png", UriKind.Relative));
            }

            if (item != null)
            {
                PasswordNameTextBox.Text = item.Name;
                WebsiteTextBox.Text = item.Website;
                EmailTextBox.Text = item.Email;
                UsernameTextBox.Text = item.Username;
                NewPasswordBox.Password = item.Password;
                passwordTextBox.Text = item.Password;
            }
            else
            {
                PasswordNameTextBox.Text = "";
                WebsiteTextBox.Text = "";
                EmailTextBox.Text = "";
                UsernameTextBox.Text = "";
                NewPasswordBox.Password = "";
                passwordTextBox.Text = "";
            }
        }
    }
}