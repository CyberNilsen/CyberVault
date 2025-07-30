using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace CyberVault.View
{
    public partial class PasswordHistoryWindow : Window
    {
        public ObservableCollection<PasswordHistoryEntry> HistoryEntries { get; set; }
        private PasswordItem _passwordItem;
        private string _username;
        public bool RestoreRequested { get; private set; }
        public int SelectedHistoryIndex { get; private set; }

        public PasswordHistoryWindow(PasswordItem passwordItem, string username)
        {
            InitializeComponent();
            _passwordItem = passwordItem;
            _username = username;
            HistoryEntries = new ObservableCollection<PasswordHistoryEntry>();
            DataContext = this;
            LoadHistory();
        }

        private void LoadHistory()
        {
            HistoryEntries.Clear();
            foreach (var entry in _passwordItem.GetPasswordHistory())
            {
                HistoryEntries.Add(entry);
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (HistoryListBox.SelectedItem is PasswordHistoryEntry selectedEntry)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to restore the password from {selectedEntry.DateChanged:yyyy-MM-dd HH:mm:ss}?",
                    "Confirm Restore",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SelectedHistoryIndex = HistoryEntries.IndexOf(selectedEntry);
                    RestoreRequested = true;
                    DialogResult = true;
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Please select a password history entry to restore.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all password history? This action cannot be undone.",
                "Confirm Clear History",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _passwordItem.ClearHistory();
                LoadHistory();
                MessageBox.Show("Password history cleared successfully.", "History Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
                
            }

            if (HistoryEntries.Count == 0)
            {
                ClearHistoryButton.IsEnabled = false;
            }

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RestoreButton.IsEnabled = HistoryListBox.SelectedItem != null;
        }
    }
}