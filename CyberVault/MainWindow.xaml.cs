using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using Hardcodet.Wpf.TaskbarNotification;
using System.IO;

namespace CyberVault
{
    public partial class MainWindow : Window
    {
        private const double NormalTopBarHeight = 40;
        private const double MaximizedTopBarHeight = 44;
        private TaskbarIcon ?trayIcon;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            MainContent.Content = new LoginControl(this);
            InitializeTrayIcon();

            Closing += MainWindow_Closing!;
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new TaskbarIcon
            {
                Icon = new System.Drawing.Icon(System.Windows.Application.GetResourceStream(
                    new System.Uri("pack://application:,,,/Images/CyberVault.ico")).Stream),
                ToolTipText = "CyberVault",
                Visibility = Visibility.Hidden
            };

            var contextMenu = new ContextMenu();

            var openMenuItem = new MenuItem { Header = "Open" };
            openMenuItem.Click += (s, e) => ShowMainWindow();

            var exitMenuItem = new MenuItem { Header = "Exit" };
            exitMenuItem.Click += (s, e) => ExitApplication();

            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(exitMenuItem);

            trayIcon.ContextMenu = contextMenu;

            trayIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApplication()
        {
            trayIcon!.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (App.MinimizeToTrayEnabled)
            {
                e.Cancel = true;
                this.Hide();
                trayIcon!.Visibility = Visibility.Visible;
            }
            else
            {
                if (trayIcon != null)
                {
                    trayIcon.Dispose();
                }
            }
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
            if (App.MinimizeToTrayEnabled)
            {
                this.Hide();
                trayIcon!.Visibility = Visibility.Visible;
            }
            else
            {
                Window window = Window.GetWindow(this);
                if (window != null)
                {
                    window.Close();
                }
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

        public void ToggleWindowState()
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