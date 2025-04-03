using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CyberVault
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainContent.Content = new LoginControl(this);
        }
        public void Navigate(UserControl newPage)
        {
            MainContent.Content = newPage;
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
