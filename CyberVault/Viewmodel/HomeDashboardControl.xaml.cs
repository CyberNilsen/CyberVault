using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CyberVault.WebExtension;

namespace CyberVault.Viewmodel
{
    public partial class HomeDashboardControl : UserControl
    {
        private string? _accessToken;
        private LocalWebServer? _webServer;
        private string _currentVersion = "v3.0";
        private readonly string _githubRepoUrl = "https://github.com/CyberNilsen/CyberVault";
        private readonly string _githubApiReleaseUrl = "https://api.github.com/repos/CyberNilsen/CyberVault/releases/latest";
        private bool _updateAvailable = false;
        private string _latestVersion = "";
        private string _downloadUrl = "";
        private bool _isDownloading = false;
        private readonly string _tempDownloadPath;
        private readonly string _applicationPath;
        private UpdateProgressWindow? _updateProgressWindow;

        public HomeDashboardControl(string username, byte[] encryptionKey)
        {
            InitializeComponent();

            LoadCurrentVersion();

            _applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            _tempDownloadPath = Path.Combine(Path.GetTempPath(), "CyberVaultUpdate");

            if (App.WebServer != null)
            {
                _webServer = App.WebServer;
                _accessToken = App.CurrentAccessToken;
            }
            else
            {
                InitializeWebServer(username, encryptionKey);
            }

            this.Loaded += UserControl_Loaded;

            SetRandomQuote();
            CurrentVersionText.Text = _currentVersion;
        }

        private void LoadCurrentVersion()
        {
            try
            {
                string versionFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
                if (File.Exists(versionFilePath))
                {
                    string versionFromFile = File.ReadAllText(versionFilePath).Trim();
                    if (!string.IsNullOrEmpty(versionFromFile) && versionFromFile.StartsWith("v"))
                    {
                        _currentVersion = versionFromFile;
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CheckForUpdatesAsync();
            ExtensionKeyDisplay();
        }

        private void ExtensionKeyDisplay()
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                _accessToken = App.CurrentAccessToken;

                if (string.IsNullOrEmpty(_accessToken) && _webServer != null)
                {
                    
                        _accessToken = _webServer.GetAccessToken();
                        App.CurrentAccessToken = _accessToken;
                    
                    
                }
            }

            if (!string.IsNullOrEmpty(_accessToken))
            {
                ExtensionKeyText.Text = _accessToken;
                string maskedText = new string('•', _accessToken.Length);
                MaskedKeyText.Text = maskedText;
            }
            else
            {
                ExtensionKeyText.Text = "Server not started";
                MaskedKeyText.Text = "Server not started";
            }

            ExtensionKeyText.Visibility = Visibility.Collapsed;
            MaskedKeyText.Visibility = Visibility.Visible;
        }

        private async void CheckForUpdatesAsync()
        {
            try
            {
                await CheckForUpdates(false);
            }
            catch (Exception)
            {
                UpdateStatusText.Text = "Update status: Unknown";
            }
        }

        private void InitializeWebServer(string username, byte[] encryptionKey)
        {
            try
            {
                _webServer = new LocalWebServer(username, encryptionKey);
                _webServer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start web server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadExtensionKey()
        {
            try
            {
                if (_webServer != null && string.IsNullOrEmpty(_accessToken))
                {
                    _accessToken = _webServer.GetAccessToken();
                    App.CurrentAccessToken = _accessToken;
                }

                ExtensionKeyDisplay();
            }
            catch (Exception ex)
            {
                ExtensionKeyText.Text = "Error loading key";
                MaskedKeyText.Text = "Error loading key";
                MessageBox.Show($"Failed to load extension key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyKeyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    Clipboard.SetText(_accessToken);
                    ShowCopySuccess();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ToggleKeyVisibilityButton_Click(object sender, RoutedEventArgs e)
        {
            if (ExtensionKeyText.Visibility == Visibility.Collapsed)
            {
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    ExtensionKeyText.Text = _accessToken;
                }

                ExtensionKeyText.Visibility = Visibility.Visible;
                MaskedKeyText.Visibility = Visibility.Collapsed;
                ((TextBlock)ToggleKeyVisibilityButton.Content).Text = "\uE7B3";
            }
            else
            {
                ExtensionKeyText.Visibility = Visibility.Collapsed;
                MaskedKeyText.Visibility = Visibility.Visible;
                ((TextBlock)ToggleKeyVisibilityButton.Content).Text = "\uE7B3";
            }
        }

        private void ShowCopySuccess()
        {
            string? originalContent = CopyKeyButton.Content.ToString();
            CopyKeyButton.Content = "Copied!";
            CopyKeyButton.Background = System.Windows.Media.Brushes.Green;

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += (s, args) =>
            {
                CopyKeyButton.Content = originalContent;
                CopyKeyButton.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#5E81AC"));
                timer.Stop();
            };
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Start();
        }

        private void SetRandomQuote()
        {
            List<string> quotes = new List<string>
                {
                    "☕ Strong passwords and strong coffee: two things you should never compromise.",
                    "🔥 Your firewall is only as strong as your coffee is black.",
                    "🔐 Encrypted data, decrypted brain: coffee does both.",
                    "💀 Brute-force attacks won't work on me, but a strong espresso might.",
                    "🛡️ Coffee is like cybersecurity—stay patched and stay awake.",
                    "🎣 No phishing attempts can fool me before my first coffee.",
                    "⚠️ Without coffee, even the most secure system has vulnerabilities: ME.",
                    "🚫 A weak password is like decaf coffee—completely useless.",
                    "💰 Ransomware can't hold my coffee hostage.",
                    "🌙 Cyber threats don't sleep, but neither do I—thanks to coffee.",
                    "👨‍💻 Hackers exploit human errors. I exploit coffee for survival.",
                    "⌨️ Every keystroke is a bit more secure with a sip of coffee.",
                    "🕵️ Digital forensics is easier with a forensic amount of coffee.",
                    "🌍 But first, coffee. Then, world domination.",
                    "🤝 Coffee doesn't ask questions; coffee understands.",
                    "🛠️ If coffee can't fix it, it's a serious problem.",
                    "😴 A yawn is just a silent scream for coffee.",
                    "😊 Happiness is a freshly brewed cup of coffee.",
                    "🤔 Coffee first, decisions later.",
                    "💞 Keep your friends close and your coffee closer.",
                    "⚡ Caffeine: the only reason I function before noon.",
                    "🌧️ Today's forecast: 100% chance of coffee.",
                    "💻 Behind every great developer is a lot of empty coffee cups.",
                    "⏳ Sleep is optional. Coffee is mandatory.",
                    "❤️ You had me at 'coffee.'",
                    "☕ More espresso, less depresso.",
                    "🌅 Life begins after coffee.",
                    "🎭 Espresso yourself!",
                    "💡 Good ideas start with coffee.",
                    "🔒 May your coffee be strong and your passwords stronger."
                };

            Random rand = new Random();
            int index = rand.Next(quotes.Count);
            QuoteText.Text = quotes[index];
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdates(true);
        }

        private async Task CheckForUpdates(bool showErrors)
        {
            try
            {
                UpdateStatusText.Text = "Checking for updates...";
                CheckUpdateButton.IsEnabled = false;

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "CyberVault-Program");
                    client.Timeout = TimeSpan.FromSeconds(5);

                    string responseBody = await client.GetStringAsync(_githubApiReleaseUrl);

                    Regex versionRegex = new Regex("\"tag_name\":\\s*\"(v[0-9]+\\.[0-9]+)\"");
                    Match versionMatch = versionRegex.Match(responseBody);

                    if (versionMatch.Success)
                    {
                        _latestVersion = versionMatch.Groups[1].Value;

                        Regex downloadUrlRegex = new Regex("\"browser_download_url\":\\s*\"([^\"]+\\.zip)\"");
                        Match downloadUrlMatch = downloadUrlRegex.Match(responseBody);

                        if (downloadUrlMatch.Success)
                            _downloadUrl = downloadUrlMatch.Groups[1].Value;
                        else
                            _downloadUrl = $"{_githubRepoUrl}/releases/tag/{_latestVersion}";

                        CompareVersions(_currentVersion, _latestVersion);
                    }
                    else
                    {
                        if (showErrors)
                            UpdateStatusText.Text = "Could not retrieve version information.";
                    }
                }
            }
            catch (Exception ex)
            {
                if (showErrors)
                    UpdateStatusText.Text = $"Error checking for updates: {ex.Message}";
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
            }
        }

        private void CompareVersions(string currentVersion, string latestVersion)
        {
            try
            {
                currentVersion = currentVersion.TrimStart('v');
                latestVersion = latestVersion.TrimStart('v');

                Version current = new Version(currentVersion);
                Version latest = new Version(latestVersion);

                if (latest > current)
                {
                    _updateAvailable = true;
                    UpdateStatusText.Text = $"Update available: {_latestVersion}";
                    UpdateNowButton.Visibility = Visibility.Visible;
                }
                else
                {
                    _updateAvailable = false;
                    UpdateStatusText.Text = "You have the latest version.";
                    UpdateNowButton.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"Error comparing versions: {ex.Message}";
            }
        }

        private void UpdateNowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_updateAvailable && !string.IsNullOrEmpty(_downloadUrl) && !_isDownloading)
            {
                ShowUpdateConfirmationDialog();
            }
        }

        private void ShowUpdateConfirmationDialog()
        {
            Window confirmationDialog = new Window
            {
                Title = "Update Confirmation",
                Width = 400,
                Height = 230,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            Grid dialogGrid = new Grid();
            dialogGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            dialogGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock messageText = new TextBlock
            {
                Text = $"A new version of CyberVault ({_latestVersion}) is available.\n\nDo you want to update now?\n\nThe application will close, update, and restart automatically.",
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20)
            };
            Grid.SetRow(messageText, 0);
            dialogGrid.Children.Add(messageText);

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(buttonPanel, 1);

            Button yesButton = new Button
            {
                Content = "Update Now",
                Width = 120,
                Height = 30,
                Margin = new Thickness(10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5E81AC")),
                Foreground = Brushes.White
            };
            yesButton.Click += async (s, args) =>
            {
                confirmationDialog.Close();
                await PerformUpdate();
            };

            Button noButton = new Button
            {
                Content = "Later",
                Width = 120,
                Height = 30,
                Margin = new Thickness(10)
            };
            noButton.Click += (s, args) => confirmationDialog.Close();

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            dialogGrid.Children.Add(buttonPanel);

            confirmationDialog.Content = dialogGrid;
            confirmationDialog.ShowDialog();
        }

        private async Task PerformUpdate()
        {
            try
            {
                if (!_downloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _downloadUrl,
                        UseShellExecute = true
                    });
                    return;
                }

                _isDownloading = true;
                UpdateNowButton.IsEnabled = false;
                CheckUpdateButton.IsEnabled = false;

                _updateProgressWindow = new UpdateProgressWindow();
                _updateProgressWindow.TitleText.Text = $"Updating CyberVault to {_latestVersion}";
                _updateProgressWindow.Show();

                if (Directory.Exists(_tempDownloadPath))
                    Directory.Delete(_tempDownloadPath, true);

                Directory.CreateDirectory(_tempDownloadPath);

                string zipPath = Path.Combine(_tempDownloadPath, "update.zip");
                await DownloadFileAsync(_downloadUrl, zipPath);

                _updateProgressWindow.StatusText.Text = "Download complete. Installing update...";

                try
                {
                    File.WriteAllText(Path.Combine(_tempDownloadPath, "version.txt"), _latestVersion);
                }
                catch
                {
                }

                CreateUpdaterScript(zipPath);

                RunUpdaterInBackground();
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _isDownloading = false;
                UpdateNowButton.IsEnabled = true;
                CheckUpdateButton.IsEnabled = true;
                UpdateStatusText.Text = $"Update failed: {ex.Message}";

                if (_updateProgressWindow != null && _updateProgressWindow.IsVisible)
                {
                    _updateProgressWindow.Close();
                    _updateProgressWindow = null;
                }

                MessageBox.Show($"Failed to download update: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateUpdaterScript(string zipPath)
        {
            string psScriptPath = Path.Combine(_tempDownloadPath, "update.ps1");

            string psScriptContent = @"
                                    # Log function
                                    function Write-Log {
                                        param (
                                            [string]$Message
                                        )
                                        Add-Content -Path '$LOGFILE$' -Value ""$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') - $Message""
                                    }

                                    try {
                                        Write-Log ""Update script started""

                                        Start-Sleep -Seconds 2
                                        Write-Log ""Waiting for application to close completely""

                                        $versionFile = ""$TEMPPATH$\version.txt""
                                        if (Test-Path -Path $versionFile) {
                                            Write-Log ""Found version file, will restore after update""
                                            $versionContent = Get-Content -Path $versionFile -Raw
                                        }

                                        # Create a backup of critical files that should be preserved
                                        Write-Log ""Creating backup of critical files""
                                        $backupDir = ""$TEMPPATH$\backup""
                                        if (Test-Path -Path $backupDir) {
                                            Remove-Item -Path $backupDir -Recurse -Force
                                        }
                                        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

                                        $filesToPreserve = @(

                                        )

                                        foreach ($file in $filesToPreserve) {
                                            $sourcePath = Join-Path ""$APPPATH$"" $file
                                            if (Test-Path -Path $sourcePath) {
                                                $destPath = Join-Path $backupDir $file
                                                Copy-Item -Path $sourcePath -Destination $destPath -Force -Recurse
                                                Write-Log ""Backed up: $file""
                                            }
                                        }

                                        Write-Log ""Cleaning application directory""
                                        Get-ChildItem -Path ""$APPPATH$"" -Exclude ""CyberVaultUpdate"" | ForEach-Object {
                                            if ($_.FullName -ne $backupDir) {
                                                Remove-Item -Path $_.FullName -Recurse -Force
                                                Write-Log ""Removed: $($_.Name)""
                                            }
                                        }

                                        Write-Log ""Extracting update directly to application directory""
                                        $extractDir = ""$TEMPPATH$\extracted""
                                        if (Test-Path -Path $extractDir) {
                                            Remove-Item -Path $extractDir -Recurse -Force
                                        }
                                        New-Item -ItemType Directory -Path $extractDir -Force | Out-Null

                                        Expand-Archive -Path ""$ZIPPATH$"" -DestinationPath $extractDir -Force

                                        $extractedItems = Get-ChildItem -Path $extractDir
                                        if ($extractedItems.Count -eq 1 -and (Get-Item -Path $extractedItems[0].FullName).PSIsContainer) {
                                            # If the zip contains a root folder, copy content from inside that folder
                                            Write-Log ""Detected single root folder in archive, extracting from inside it""
                                            $rootFolder = $extractedItems[0].FullName
                                            Get-ChildItem -Path $rootFolder -Recurse | ForEach-Object {
                                                $relativePath = $_.FullName.Substring($rootFolder.Length + 1)
                                                $targetPath = Join-Path ""$APPPATH$"" $relativePath

                                                if ($_.PSIsContainer) {
                                                    if (-not (Test-Path -Path $targetPath)) {
                                                        New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
                                                        Write-Log ""Created directory: $relativePath""
                                                    }
                                                } else {
                                                    $targetDir = [System.IO.Path]::GetDirectoryName($targetPath)
                                                    if (-not (Test-Path -Path $targetDir)) {
                                                        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
                                                    }
                                                    Copy-Item -Path $_.FullName -Destination $targetPath -Force
                                                    Write-Log ""Copied file: $relativePath""
                                                }
                                            }
                                        } else {
                                            Write-Log ""No root folder in archive, copying files directly""
                                            Get-ChildItem -Path $extractDir -Recurse | ForEach-Object {
                                                $relativePath = $_.FullName.Substring($extractDir.Length + 1)
                                                $targetPath = Join-Path ""$APPPATH$"" $relativePath

                                                if ($_.PSIsContainer) {
                                                    if (-not (Test-Path -Path $targetPath)) {
                                                        New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
                                                        Write-Log ""Created directory: $relativePath""
                                                    }
                                                } else {
                                                    $targetDir = [System.IO.Path]::GetDirectoryName($targetPath)
                                                    if (-not (Test-Path -Path $targetDir)) {
                                                        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
                                                    }
                                                    Copy-Item -Path $_.FullName -Destination $targetPath -Force
                                                    Write-Log ""Copied file: $relativePath""
                                                }
                                            }
                                        }

                                        if ($versionContent) {
                                            Write-Log ""Restoring version file""
                                            Set-Content -Path ""$APPPATH$\version.txt"" -Value $versionContent
                                        }

                                        Write-Log ""Restoring preserved files""
                                        foreach ($file in $filesToPreserve) {
                                            $sourcePath = Join-Path $backupDir $file
                                            if (Test-Path -Path $sourcePath) {
                                                $destPath = Join-Path ""$APPPATH$"" $file
                                                Copy-Item -Path $sourcePath -Destination $destPath -Force -Recurse
                                                Write-Log ""Restored: $file""
                                            }
                                        }

                                        Write-Log ""Cleaning up temporary directories""
                                        if (Test-Path -Path $backupDir) {
                                            Remove-Item -Path $backupDir -Recurse -Force
                                        }
                                        if (Test-Path -Path $extractDir) {
                                            Remove-Item -Path $extractDir -Recurse -Force
                                        }

                                        $appExePath = ""$APPPATH$\CyberVault.exe""
                                        if (Test-Path -Path $appExePath) {
                                            Write-Log ""Application executable found at: $appExePath""

                                            Start-Sleep -Seconds 2

                                            Write-Log ""Starting application""
                                            try {
                                                $startInfo = New-Object System.Diagnostics.ProcessStartInfo
                                                $startInfo.FileName = $appExePath
                                                $startInfo.WorkingDirectory = ""$APPPATH$""
                                                $startInfo.UseShellExecute = $true

                                                [System.Diagnostics.Process]::Start($startInfo)
                                                Write-Log ""Application started successfully""
                                            }
                                            catch {
                                                Write-Log ""Error starting application: $_""
                                                try {
                                                    Start-Process -FilePath $appExePath -WorkingDirectory ""$APPPATH$""
                                                    Write-Log ""Application started with fallback method""
                                                }
                                                catch {
                                                    Write-Log ""All attempts to start application failed: $_""
                                                }
                                            }
                                        }
                                        else {
                                            Write-Log ""ERROR: Application executable not found at: $appExePath""
                                        }

                                        # Exit script
                                        Write-Log ""Update completed successfully""
                                        exit 0
                                    }
                                    catch {
                                        Write-Log ""Error during update: $_""
                                        exit 1
                                    }
                                    ";

            string logFilePath = Path.Combine(_tempDownloadPath, "update_log.txt");

            psScriptContent = psScriptContent.Replace("$LOGFILE$", logFilePath.Replace("\\", "\\\\"));
            psScriptContent = psScriptContent.Replace("$ZIPPATH$", zipPath.Replace("\\", "\\\\"));
            psScriptContent = psScriptContent.Replace("$APPPATH$", _applicationPath.Replace("\\", "\\\\"));
            psScriptContent = psScriptContent.Replace("$TEMPPATH$", _tempDownloadPath.Replace("\\", "\\\\"));

            File.WriteAllText(psScriptPath, psScriptContent);

            string batchContent = $@"@echo off
                                  start /b powershell.exe -ExecutionPolicy Bypass -WindowStyle Hidden -File ""{psScriptPath}""
                                  exit";

            File.WriteAllText(Path.Combine(_tempDownloadPath, "update_launcher.bat"), batchContent);
        }

        private void RunUpdaterInBackground()
        {
            try
            {
                string launcherPath = Path.Combine(_tempDownloadPath, "update_launcher.bat");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = launcherPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start updater: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DownloadFileAsync(string url, string filePath)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "CyberVault");

                var progress = new Progress<double>(percent =>
                {
                    if (_updateProgressWindow != null && _updateProgressWindow.IsVisible)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _updateProgressWindow.UpdateProgressBar.Value = percent * 100;
                            _updateProgressWindow.StatusText.Text = $"Downloading update: {percent:P0}";
                        });
                    }
                });

                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        {
                            if (totalBytes == -1)
                            {
                                await contentStream.CopyToAsync(fileStream);
                            }
                            else
                            {
                                var buffer = new byte[8192];
                                var totalBytesRead = 0L;
                                var bytesRead = 0;

                                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                                    totalBytesRead += bytesRead;
                                    ((IProgress<double>)progress).Report((double)totalBytesRead / totalBytes);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}