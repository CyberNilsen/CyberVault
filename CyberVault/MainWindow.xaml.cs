using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using Hardcodet.Wpf.TaskbarNotification;
using System.Windows.Threading;
using CyberVault.Viewmodel;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;



namespace CyberVault
{
    public partial class MainWindow : Window
    {
        private const double NormalTopBarHeight = 40;
        private const double MaximizedTopBarHeight = 44;

        private TaskbarIcon? trayIcon;

        private DispatcherTimer? activityTimer;
        private DateTime lastActivityTime;
        private bool isLocked = false;
        private string? currentUsername;
        private int autoLockMinutes = 5;
        private DispatcherTimer? clipboardClearTimer;
        private int clipboardClearSeconds = 30;
        private string? lastCopiedContent;

        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            MainContent.Content = new LoginControl(this);
            InitializeTrayIcon();
            InitializeActivityMonitoring();
            InitializeClipboardMonitoring();

            Closing += MainWindow_Closing!;
        }

        private void InitializeActivityMonitoring()
        {
            activityTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            activityTimer.Tick += ActivityTimer_Tick!;

            ResetActivityTimer();

            activityTimer.Start();

            this.MouseMove += MainWindow_UserActivity;
            this.KeyDown += MainWindow_UserActivity;
            this.PreviewMouseDown += MainWindow_UserActivity;
        }

        private void ResetActivityTimer()
        {
            lastActivityTime = DateTime.Now;
        }

        private void MainWindow_UserActivity(object sender, EventArgs e)
        {
            if (!isLocked)
            {
                ResetActivityTimer();
            }
        }

        private void ActivityTimer_Tick(object sender, EventArgs e)
        {
            if (isLocked || string.IsNullOrEmpty(currentUsername))
                return;

            TimeSpan inactiveTime = DateTime.Now - lastActivityTime;

            if (inactiveTime.TotalMinutes >= autoLockMinutes && autoLockMinutes > 0)
            {
                LockApplication();
            }
        }

        public void LockApplication()
        {
            isLocked = true;

            App.MinimizeToTrayEnabled = false;
            App.CurrentUsername = null!;
            App.CurrentAccessToken = null;
            App.WebServer?.Stop();
            App.WebServer = null;

            typeof(LoginControl).GetProperty("CurrentEncryptionKey",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static)?.SetValue(null, null);

            MainContent.Content = new LoginControl(this);

            MessageBox.Show("Application locked due to inactivity.", "Security",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void UserLoggedIn(string username, int lockTimeMinutes)
        {
            currentUsername = username;
            autoLockMinutes = lockTimeMinutes;
            isLocked = false;
            ResetActivityTimer();
        }

        private void InitializeClipboardMonitoring()
        {
            clipboardClearTimer = new DispatcherTimer();
            clipboardClearTimer.Tick += ClipboardClearTimer_Tick!;
        }

        private void ClipboardClearTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Clipboard.Clear();

                ClearClipboardCompletely();

                ClearWindowsClipboardHistory();

                clipboardClearTimer?.Stop();
                lastCopiedContent = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Clipboard clear error: {ex.Message}"); 
                clipboardClearTimer?.Stop();
            }
        }

        private void ClearClipboardCompletely()
        {
            try
            {
                IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                if (OpenClipboard(hwnd))
                {
                    EmptyClipboard();
                    CloseClipboard();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Windows API clipboard clear error: {ex.Message}");
            }
        }

        private void ClearWindowsClipboardHistory()
        {
            try
            {
                ClearClipboardHistoryUsingWinRT();

                OverwriteClipboardHistory();

                var clearScript = @"
                    try {
                        # Clear current clipboard
                        [System.Windows.Forms.Clipboard]::Clear()
                        
                        # Try to access and clear clipboard history using Windows APIs
                        Add-Type -AssemblyName PresentationCore
                        [System.Windows.Clipboard]::Clear()
                        
                        # Force garbage collection to clear any cached clipboard data
                        [System.GC]::Collect()
                        [System.GC]::WaitForPendingFinalizers()
                        [System.GC]::Collect()
                        
                    } catch {}";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-WindowStyle Hidden -ExecutionPolicy Bypass -Command \"{clearScript}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using (var process = Process.Start(psi))
                {
                    process?.WaitForExit(2000);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to clear Windows clipboard history: {ex.Message}");
            }
        }

        private void ClearClipboardHistoryUsingWinRT()
        {
            try
            {
                var clearHistoryScript = @"
                    try {
                        # Load Windows Runtime
                        [Windows.ApplicationModel.DataTransfer.Clipboard, Windows.ApplicationModel.DataTransfer, ContentType = WindowsRuntime] | Out-Null
                        
                        # Clear clipboard content
                        [Windows.ApplicationModel.DataTransfer.Clipboard]::Clear()
                        
                        # Try to clear history (Windows 10 1809+ feature)
                        try {
                            [Windows.ApplicationModel.DataTransfer.Clipboard]::ClearHistory()
                        } catch {}
                        
                    } catch {}";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-WindowStyle Hidden -Command \"{clearHistoryScript}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process.Start(psi)?.WaitForExit(1000);
            }
            catch { }
        }

        private void OverwriteClipboardHistory()
        {
            try
            {
                for (int i = 0; i < 25; i++)
                {
                    string dummyContent = $"CLEARED_{i}_{DateTime.Now.Ticks}";

                    Clipboard.SetText(dummyContent);
                    Thread.Sleep(10);

                    Clipboard.Clear();
                    Thread.Sleep(10);
                }

                Clipboard.Clear();
            }
            catch
            { 

            }

        }

        public void StartClipboardClearTimer(string content)
        {
            if (clipboardClearSeconds > 0)
            {
                lastCopiedContent = content;
                clipboardClearTimer?.Stop();
                clipboardClearTimer!.Interval = TimeSpan.FromSeconds(clipboardClearSeconds);
                clipboardClearTimer.Start();
                
            }
        }

        public void UpdateClipboardClearTime(int seconds)
        {
            clipboardClearSeconds = seconds;
            if (seconds == 0)
            {
                clipboardClearTimer?.Stop();
                lastCopiedContent = null;
            }
        }

        public void ClearClipboardImmediately()
        {
            try
            {
                Clipboard.Clear();

                ClearClipboardCompletely();

                ClearWindowsClipboardHistory();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login clipboard clear error: {ex.Message}");
            }
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new TaskbarIcon
            {
                Icon = new System.Drawing.Icon(System.Windows.Application.GetResourceStream(
                    new System.Uri("pack://application:,,,/Images/CyberVault.ico")).Stream),
                ToolTipText = "CyberVault Password Manager",
                Visibility = Visibility.Hidden
            };

            var contextMenu = new ContextMenu();

            ControlTemplate contextMenuTemplate = new ControlTemplate(typeof(ContextMenu));
            var contextBorderFactory = new FrameworkElementFactory(typeof(Border), "Border");
            contextBorderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B4252")));
            contextBorderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C566A")));
            contextBorderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            contextBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            contextBorderFactory.SetValue(Border.PaddingProperty, new Thickness(3));
            contextBorderFactory.SetValue(Border.MinHeightProperty, 60.0);

            var itemsPresenterFactory = new FrameworkElementFactory(typeof(ItemsPresenter), "ItemsPresenter");
            itemsPresenterFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0));
            contextBorderFactory.AppendChild(itemsPresenterFactory);

            contextMenuTemplate.VisualTree = contextBorderFactory;
            contextMenu.Template = contextMenuTemplate;

            contextMenu.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 270,
                ShadowDepth = 4,
                Opacity = 0.6,
                BlurRadius = 8
            };

            contextMenu.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B4252"));
            contextMenu.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C566A"));
            contextMenu.BorderThickness = new Thickness(1);
            contextMenu.Padding = new Thickness(3);

            Style menuItemStyle = new Style(typeof(MenuItem));
            menuItemStyle.Setters.Add(new Setter(MenuItem.ForegroundProperty, new SolidColorBrush(Colors.White)));
            menuItemStyle.Setters.Add(new Setter(MenuItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B4252"))));
            menuItemStyle.Setters.Add(new Setter(MenuItem.PaddingProperty, new Thickness(12, 12, 12, 12)));
            menuItemStyle.Setters.Add(new Setter(MenuItem.BorderThicknessProperty, new Thickness(0)));
            menuItemStyle.Setters.Add(new Setter(MenuItem.FontSizeProperty, 14.0));
            menuItemStyle.Setters.Add(new Setter(MenuItem.MinHeightProperty, 30.0));
            menuItemStyle.Setters.Add(new Setter(MenuItem.MinWidthProperty, 180.0));
            menuItemStyle.Setters.Add(new Setter(MenuItem.MarginProperty, new Thickness(2)));

            Style iconTextBlockStyle = new Style(typeof(TextBlock));
            iconTextBlockStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 12.0));
            iconTextBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Colors.White)));
            iconTextBlockStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            iconTextBlockStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            iconTextBlockStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(0)));

            var borderFactory = new FrameworkElementFactory(typeof(Border), "Border");
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(MenuItem.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(MenuItem.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(MenuItem.BorderThicknessProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            var gridFactory = new FrameworkElementFactory(typeof(Grid));

            var iconColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            iconColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(22));

            var textColumn = new FrameworkElementFactory(typeof(ColumnDefinition));
            textColumn.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));

            gridFactory.AppendChild(iconColumn);
            gridFactory.AppendChild(textColumn);

            var iconPresenter = new FrameworkElementFactory(typeof(ContentPresenter), "IconContent");
            iconPresenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(MenuItem.IconProperty));
            iconPresenter.SetValue(Grid.ColumnProperty, 0);
            iconPresenter.SetValue(ContentPresenter.WidthProperty, 18.0);
            iconPresenter.SetValue(ContentPresenter.HeightProperty, 18.0);
            iconPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            iconPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter), "HeaderContent");
            contentPresenter.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(MenuItem.HeaderProperty));
            contentPresenter.SetValue(Grid.ColumnProperty, 1);
            contentPresenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.MarginProperty, new Thickness(-16, 0, 0, 0));

            gridFactory.AppendChild(iconPresenter);
            gridFactory.AppendChild(contentPresenter);

            borderFactory.AppendChild(gridFactory);

            var menuItemTemplate = new ControlTemplate(typeof(MenuItem));
            menuItemTemplate.VisualTree = borderFactory;
            menuItemStyle.Setters.Add(new Setter(MenuItem.TemplateProperty, menuItemTemplate));

            Trigger hoverTrigger = new Trigger { Property = MenuItem.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(MenuItem.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5E81AC"))));
            hoverTrigger.Setters.Add(new Setter(MenuItem.ForegroundProperty, new SolidColorBrush(Colors.White)));
            menuItemStyle.Triggers.Add(hoverTrigger);

            var enterAnimation = new Storyboard();
            var colorAnimation = new ColorAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.2)),
                To = (Color)ColorConverter.ConvertFromString("#5E81AC")
            };
            Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("Background.Color"));
            enterAnimation.Children.Add(colorAnimation);

            var scaleXAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.2)),
                To = 1.02
            };
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));
            enterAnimation.Children.Add(scaleXAnimation);

            var scaleYAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.2)),
                To = 1.02
            };
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));
            enterAnimation.Children.Add(scaleYAnimation);

            EventTrigger mouseEnterTrigger = new EventTrigger { RoutedEvent = MenuItem.MouseEnterEvent };
            mouseEnterTrigger.Actions.Add(new BeginStoryboard { Storyboard = enterAnimation });
            menuItemStyle.Triggers.Add(mouseEnterTrigger);

            var openMenuItem = new MenuItem
            {
                Header = "Open CyberVault",
                Style = menuItemStyle,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1, 1)
            };

            var openIconTextBlock = new TextBlock
            {
                Text = "🔓",
                Style = iconTextBlockStyle
            };
            openMenuItem.Icon = openIconTextBlock;
            openMenuItem.Click += (s, e) => ShowMainWindow();

            var settingsMenuItem = new MenuItem
            {
                Header = "Settings",
                Style = menuItemStyle,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1, 1)
            };

            var settingsIconTextBlock = new TextBlock
            {
                Text = "⚙️",
                Style = iconTextBlockStyle
            };
            settingsMenuItem.Icon = settingsIconTextBlock;
            settingsMenuItem.Click += (s, e) => SettingsControl();

            var separator = new Separator
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C566A")),
                Margin = new Thickness(8, 4, 8, 4),
                Height = 1
            };

            var exitMenuItem = new MenuItem
            {
                Header = "Exit",
                Style = menuItemStyle,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1, 1)
            };

            var exitIconTextBlock = new TextBlock
            {
                Text = "✖️",
                Style = iconTextBlockStyle
            };
            exitMenuItem.Icon = exitIconTextBlock;
            exitMenuItem.Click += (s, e) => ExitApplication();

            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(settingsMenuItem);
            contextMenu.Items.Add(separator);
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

        private void SettingsControl()
        {

            if (string.IsNullOrEmpty(currentUsername) || isLocked)
            {
                MessageBox.Show("Please login to access settings.", "Authentication Required",
                                 MessageBoxButton.OK, MessageBoxImage.Information);

                MainContent.Content = new LoginControl(this);
            }
            else
            {
                byte[]? encryptionKey = (byte[]?)typeof(LoginControl).GetProperty("CurrentEncryptionKey",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static)?.GetValue(null);

                if (MainContent.Content is DashboardControl dashboard)
                {
                    dashboard.Settings_Click(this, new RoutedEventArgs());
                    ShowMainWindow();
                }
                else
                {
                    var dashboardControl = new DashboardControl(this, currentUsername, encryptionKey!);
                    MainContent.Content = dashboardControl;
                    dashboardControl.Settings_Click(this, new RoutedEventArgs());
                    ShowMainWindow();
                }
            }
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
                    App.CurrentUsername = null!;
                    App.CurrentAccessToken = null;
                    App.WebServer?.Stop();
                    App.WebServer = null;
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