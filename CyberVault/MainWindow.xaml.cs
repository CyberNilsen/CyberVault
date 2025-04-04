using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberVault
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            MainContent.Content = new LoginControl(this);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetSizeAndPositionOnPrimaryScreen();
        }

        private void SetSizeAndPositionOnPrimaryScreen()
        {
            var primaryScreen = System.Windows.SystemParameters.WorkArea;
            double widthRatio = 0.75;
            double heightRatio = 0.75;
            this.Width = primaryScreen.Width * widthRatio;
            this.Height = primaryScreen.Height * heightRatio;
            this.Left = primaryScreen.Left + (primaryScreen.Width - this.Width) / 2;
            this.Top = primaryScreen.Top + (primaryScreen.Height - this.Height) / 2;
        }

        public void Navigate(UserControl newPage)
        {
            MainContent.Content = newPage;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                window.Close();
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWindowState();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                window.WindowState = WindowState.Minimized;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Check if the original source is not an interactive element
            if (!IsInteractiveElement(e.OriginalSource as DependencyObject))
            {
                ToggleWindowState();
            }
        }

        private bool IsInteractiveElement(DependencyObject element)
        {
            // Check if the element is null
            if (element == null)
                return false;

            // Check if element is one of interactive controls
            if (element is Button || element is TextBox || element is ComboBox ||
                element is CheckBox || element is RadioButton || element is ListBox ||
                element is ListView || element is Slider || element is ScrollBar ||
                element is TextBlock) // TextBlock included as sometimes they are used for interactive elements
            {
                return true;
            }

            // Check if parent of the element is an interactive element
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            if (parent != null && IsInteractiveElement(parent))
            {
                return true;
            }

            return false;
        }

        private void ToggleWindowState()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                SetSizeAndPositionOnPrimaryScreen();
                MinimizeButton.Margin = new Thickness(0, 0, 0, 0);
                MaximizeButton.Margin = new Thickness(0, 0, 0, 0);
                CloseButton.Margin = new Thickness(0, 0, 0, 0);
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MinimizeButton.Margin = new Thickness(0, 4, 0, 0);
                MaximizeButton.Margin = new Thickness(0, 4, 0, 0);
                CloseButton.Margin = new Thickness(0, 4, 6.6, 0);
            }
            MaximizeIcon.Text = (this.WindowState == WindowState.Maximized) ? "\uE923" : "\uE922";
        }
    }
}