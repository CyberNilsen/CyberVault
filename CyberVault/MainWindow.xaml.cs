using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace CyberVault
{
    public partial class MainWindow : Window
    {
        private const double NormalTopBarHeight = 40;
        private const double MaximizedTopBarHeight = 44;
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

            if (newPage is DashboardControl dashboard)
            {
                dashboard.SetTopBarHeight(NormalTopBarHeight);
            }
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

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!IsInteractiveElement(e.OriginalSource as DependencyObject))
            {
                ToggleWindowState();
            }
        }

        private bool IsInteractiveElement(DependencyObject element)
        {

            if (element == null)
                return false;

            if (element is Button || element is TextBox || element is ComboBox ||
                element is CheckBox || element is RadioButton || element is ListBox ||
                element is ListView || element is Slider || element is ScrollBar ||
                element is TextBlock) 
            {
                return true;
            }


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

                if (MainContent.Content is DashboardControl dashboard)
                {
                    dashboard.TopBar.Height = NormalTopBarHeight;
                }

                MinimizeButton.Margin = new Thickness(0, 0, 0, 0);
                MaximizeButton.Margin = new Thickness(0, 0, 0, 0);
                CloseButton.Margin = new Thickness(0, 0, 0, 0);
            }
            else
            {
                this.WindowState = WindowState.Maximized;

                if (MainContent.Content is DashboardControl dashboard)
                {
                    dashboard.TopBar.Height = MaximizedTopBarHeight;
                }

                MinimizeButton.Margin = new Thickness(0, 4, 0, 0);
                MaximizeButton.Margin = new Thickness(0, 4, 0, 0);
                CloseButton.Margin = new Thickness(0, 4, 4, 0);
            }

            MaximizeIcon.Text = (this.WindowState == WindowState.Maximized) ? "\uE923" : "\uE922";
        }
    }
}