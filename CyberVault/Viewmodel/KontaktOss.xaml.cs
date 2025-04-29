using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace CyberVault.Viewmodel
{
    public partial class KontaktOss : UserControl
    {
        private const string FormspreeEndpoint = "https://formspree.io/f/xjkyjvka";
        private Window mainWindow;

        public KontaktOss(Window mainWindow)
        {
            this.mainWindow = mainWindow;
            InitializeComponent();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) ||
                string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                SubjectComboBox.SelectedItem == null ||
                string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                MessageBox.Show("Please fill in all fields.", "Missing information",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SendButton.IsEnabled = false;
                StatusMessage.Text = "Sending message...";
                StatusMessage.Visibility = Visibility.Visible;

                using (HttpClient client = new HttpClient())
                {
                    var formData = new Dictionary<string, string>
                    {
                        { "name", NameTextBox.Text },
                        { "email", EmailTextBox.Text },
                        { "subject", ((ComboBoxItem)SubjectComboBox.SelectedItem).Content.ToString() },
                        { "message", MessageTextBox.Text }
                    };

                    var content = new FormUrlEncodedContent(formData);
                    HttpResponseMessage response = await client.PostAsync(FormspreeEndpoint, content);

                    if (response.IsSuccessStatusCode)
                    {
                        StatusMessage.Text = "Message sent!";
                        ClearForm();
                    }
                    else
                    {
                        StatusMessage.Text = $"Failed to send. Status code: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Text = "An error occurred. Please try again later.";
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
            finally
            {
                SendButton.IsEnabled = true;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open link: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            NameTextBox.Text = string.Empty;
            EmailTextBox.Text = string.Empty;
            SubjectComboBox.SelectedIndex = -1;
            MessageTextBox.Text = string.Empty;
        }
    }
}