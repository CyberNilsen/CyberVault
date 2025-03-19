using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace CyberVault.View
{
    public partial class PasswordVault : Window
    {
        private string _username;
        public PasswordVault(string username)
        {
            InitializeComponent();
            _username = username;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            VaultDashboard dashboard = new VaultDashboard(_username);
            dashboard.Show();
            this.Close();
        }
        private void CreateSavePassword_Click(object sender, RoutedEventArgs e)
        {
            CreatePasswordGrid.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            CreatePasswordGrid.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }
        private void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Password saved!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0));
            fadeOut.Completed += (s, a) => CreatePasswordGrid.Visibility = Visibility.Collapsed;
            CreatePasswordGrid.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        private void ShowDeleteConfirmationGrid()
        {
            DeleteConfirmationGrid.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.0));
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
            MessageBox.Show("Password deleted!", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            HideDeleteConfirmationGrid();
            CloseCreatePasswordGrid();
        }
        private void ConfirmDeleteNo_Click(object sender, RoutedEventArgs e)
        {
            HideDeleteConfirmationGrid();
        }
        
    }
}
