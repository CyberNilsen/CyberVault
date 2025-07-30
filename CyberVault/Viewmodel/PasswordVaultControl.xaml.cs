using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;


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
                    _encryptionKey = LoginControl.CurrentEncryptionKey!;
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
            if (SavedPasswords.Count == 0 || PasswordListBox.SelectedItem == null)
            {
                WelcomeGrid.Visibility = Visibility.Visible;
                CreatePasswordGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                WelcomeGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            ClearSearchButton.Visibility = Visibility.Collapsed;
            PasswordListBox.ItemsSource = SavedPasswords;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = SearchBox.Text.ToLowerInvariant();

            ClearSearchButton.Visibility = string.IsNullOrWhiteSpace(searchTerm)
                ? Visibility.Collapsed
                : Visibility.Visible;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                PasswordListBox.ItemsSource = SavedPasswords;
                return;
            }

            var filteredPasswords = SavedPasswords.Where(p =>
                (!string.IsNullOrEmpty(p.Name) && p.Name.ToLowerInvariant().Contains(searchTerm)) ||
                (!string.IsNullOrEmpty(p.Website) && p.Website.ToLowerInvariant().Contains(searchTerm)) ||
                (!string.IsNullOrEmpty(p.Username) && p.Username.ToLowerInvariant().Contains(searchTerm)) ||
                (!string.IsNullOrEmpty(p.Email) && p.Email.ToLowerInvariant().Contains(searchTerm))
            ).ToList();

            PasswordListBox.ItemsSource = filteredPasswords;
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

        public void SaveAllPasswordsPublic()
        {
            SaveAllPasswords();
        }

        private void LogOut()
        {
            mainWindow.Navigate(new LoginControl(mainWindow));
        }

        private void CreateSavePassword_Click(object sender, RoutedEventArgs e)
        {
            OpenPasswordForm(null!);
            WelcomeGrid.Visibility = Visibility.Collapsed;
        }

        private void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            string password = NewPasswordBox.Visibility == Visibility.Visible ? NewPasswordBox.Password : PlainTextPassword.Text;

            if (string.IsNullOrWhiteSpace(PasswordNameTextBox.Text) ||
                string.IsNullOrWhiteSpace(WebsiteTextBox.Text) ||
                string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                string.IsNullOrWhiteSpace(UsernameTextBox.Text) ||
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please fill in all required fields before proceeding.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedItem = PasswordListBox.SelectedItem as PasswordItem;

            if (selectedItem != null)
            {
                selectedItem.Name = PasswordNameTextBox.Text;
                selectedItem.Website = WebsiteTextBox.Text;
                selectedItem.Email = EmailTextBox.Text;
                selectedItem.Username = UsernameTextBox.Text;

                if (selectedItem.Password != password)
                {
                    selectedItem.UpdatePassword(password, _username);
                }

                MessageBox.Show("Password updated!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var newItem = new PasswordItem
                {
                    Name = PasswordNameTextBox.Text,
                    Website = WebsiteTextBox.Text,
                    Email = EmailTextBox.Text,
                    Username = UsernameTextBox.Text
                };
                newItem.UpdatePassword(password, _username);
                SavedPasswords.Add(newItem);
                MessageBox.Show("Password saved!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            SaveAllPasswords();
            LoadPasswords();
            CloseCreatePasswordGrid();
        }

        private void ViewHistory_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = PasswordListBox.SelectedItem as PasswordItem;
            if (selectedItem != null)
            {
                var historyWindow = new PasswordHistoryWindow(selectedItem, _username, this);
                historyWindow.Owner = Window.GetWindow(this);

                if (historyWindow.ShowDialog() == true && historyWindow.RestoreRequested)
                {
                    selectedItem.RestorePassword(historyWindow.SelectedHistoryIndex, _username);
                    SaveAllPasswords();

                    PasswordNameTextBox.Text = selectedItem.Name;
                    WebsiteTextBox.Text = selectedItem.Website;
                    EmailTextBox.Text = selectedItem.Email;
                    UsernameTextBox.Text = selectedItem.Username;
                    NewPasswordBox.Password = selectedItem.Password;
                    PlainTextPassword.Text = selectedItem.Password;

                    ViewHistoryButton.Visibility = selectedItem.PasswordHistory.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                    MessageBox.Show("Password restored from history!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (historyWindow.HistoryCleared)
                {
                    ViewHistoryButton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                MessageBox.Show("Please select a password to view its history.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
                PasswordListBox.SelectedItem = null;
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
                ViewHistoryButton.Visibility = item.PasswordHistory.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                PasswordNameTextBox.Text = "";
                WebsiteTextBox.Text = "";
                EmailTextBox.Text = "";
                UsernameTextBox.Text = "";
                NewPasswordBox.Password = "";
                PlainTextPassword.Text = "";
                ViewHistoryButton.Visibility = Visibility.Collapsed;
            }
        }
    }
}