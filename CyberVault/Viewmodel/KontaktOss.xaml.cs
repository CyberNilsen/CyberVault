using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace CyberVault.Viewmodel
{
    public partial class KontaktOss : UserControl
    {
        private Window mainWindow;

        public KontaktOss(Window mainWindow)
        {
            this.mainWindow = mainWindow;
            InitializeComponent();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate input
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

                // Hardcoded Formspree endpoint
                string formspreeEndpoint = "https://formspree.io/f/xjkyjvka"; // Replace with your actual Formspree endpoint

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

                    // Send the POST request to Formspree
                    HttpResponseMessage response = await client.PostAsync(formspreeEndpoint, content);

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

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                // Use Process.Start to open the hyperlink in the default browser
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
