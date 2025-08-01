using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace CyberVault.Viewmodel
{
    public partial class PasskeyVaultControl : UserControl
    {
        public ObservableCollection<PasskeyEntry> Passkeys { get; set; }

        public PasskeyVaultControl()
        {
            InitializeComponent();
            Passkeys = new ObservableCollection<PasskeyEntry>();
            PasskeyList.ItemsSource = Passkeys;
            Passkeys.CollectionChanged += (s, e) => UpdateEmptyState();
            UpdateEmptyState();
        }

        public class PasskeyEntry
        {
            public string ServiceName { get; set; }
            public string Username { get; set; }
            public string CredentialId { get; set; }
            // You can add more fields as needed (public key, etc.)
        }

        private void AddPasskey_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show dialog and start WebAuthn registration
            MessageBox.Show("Passkey registration coming soon!", "Info");
        }

        private void CopyCredentialId_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string credId)
            {
                Clipboard.SetText(credId);
                MessageBox.Show("Credential ID copied to clipboard.", "Copied");
            }
        }

        private void UpdateEmptyState()
        {
            bool isEmpty = Passkeys.Count == 0;
            EmptyStatePanel.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            PasskeyList.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
