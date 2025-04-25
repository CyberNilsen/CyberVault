using System.Windows;

namespace CyberVault
{
    public partial class PasswordConfirmationDialog : Window
    {
        public string Password { get; private set; }

        public PasswordConfirmationDialog()
        {
            InitializeComponent();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(PasswordInput.Password))
            {
                Password = PasswordInput.Password;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please enter your password.", "Password Required",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}