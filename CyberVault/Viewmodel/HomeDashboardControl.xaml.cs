using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CyberVault.WebExtension;
using System.Windows.Threading;
using System.ComponentModel;

namespace CyberVault.Viewmodel
{
    public partial class HomeDashboardControl : UserControl
    {
        private string _accessToken;
        private LocalWebServer _webServer;
        private string _currentVersion = "v3.0";
        private readonly string _githubRepoUrl = "https://github.com/CyberNilsen/CyberVault";
        private readonly string _githubApiReleaseUrl = "https://api.github.com/repos/CyberNilsen/CyberVault/releases/latest";
        private bool _updateAvailable = false;
        private string _latestVersion = "";
        private string _downloadUrl = "";
        private bool _isDownloading = false;
        private readonly string _tempDownloadPath;
        private readonly string _applicationPath;
        private UpdateProgressWindow _updateProgressWindow;

        public HomeDashboardControl(string username, byte[] encryptionKey)
        {
            InitializeComponent();

            // Read current version from a file or configuration
            LoadCurrentVersion();

            // Get application path
            _applicationPath = AppDomain.CurrentDomain.BaseDirectory;
            _tempDownloadPath = Path.Combine(Path.GetTempPath(), "CyberVaultUpdate");

            InitializeWebServer(username, encryptionKey);
            LoadExtensionKey();
            SetRandomQuote();
            CurrentVersionText.Text = _currentVersion;
        }

        private void LoadCurrentVersion()
        {
            // Try to read version from a file or configuration
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
                // If there's an error, fall back to the hardcoded version
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Check for updates automatically when control is loaded
            CheckForUpdatesAsync();
        }

        private async void CheckForUpdatesAsync()
        {
            try
            {
                await CheckForUpdates(false);
            }
            catch (Exception)
            {
                // Silently fail if there's no internet connection
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
                if (_webServer != null)
                {
                    _accessToken = _webServer.GetAccessToken();
                    ExtensionKeyText.Text = _accessToken;

                    string maskedText = new string('•', _accessToken.Length);
                    MaskedKeyText.Text = maskedText;

                    ExtensionKeyText.Visibility = Visibility.Collapsed;
                    MaskedKeyText.Visibility = Visibility.Visible;
                }
                else
                {
                    ExtensionKeyText.Text = "Server not started";
                    MaskedKeyText.Text = "Server not started";
                }
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
                ExtensionKeyText.Visibility = Visibility.Visible;
                MaskedKeyText.Visibility = Visibility.Collapsed;

                ((TextBlock)ToggleKeyVisibilityButton.Content).Text = "\uE7AA";
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
            string originalContent = CopyKeyButton.Content.ToString();
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
                    client.DefaultRequestHeaders.Add("User-Agent", "CyberVault-UpdateChecker");
                    client.Timeout = TimeSpan.FromSeconds(5); // Timeout after 5 seconds

                    string responseBody = await client.GetStringAsync(_githubApiReleaseUrl);

                    Regex versionRegex = new Regex("\"tag_name\":\\s*\"(v[0-9]+\\.[0-9]+)\"");
                    Match versionMatch = versionRegex.Match(responseBody);

                    if (versionMatch.Success)
                    {
                        _latestVersion = versionMatch.Groups[1].Value;

                        // Look for direct download URL
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

        private async void UpdateNowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_updateAvailable && !string.IsNullOrEmpty(_downloadUrl) && !_isDownloading)
            {
                try
                {
                    // Check if the URL ends with .zip
                    if (!_downloadUrl.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        // Open browser to download page if direct download URL is not available
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

                    // Show the progress window
                    _updateProgressWindow = new UpdateProgressWindow();
                    _updateProgressWindow.TitleText.Text = $"Updating CyberVault to {_latestVersion}";
                    _updateProgressWindow.Show();

                    // Create temp directory if it doesn't exist
                    if (Directory.Exists(_tempDownloadPath))
                        Directory.Delete(_tempDownloadPath, true);

                    Directory.CreateDirectory(_tempDownloadPath);

                    // Download the update
                    string zipPath = Path.Combine(_tempDownloadPath, "update.zip");
                    await DownloadFileAsync(_downloadUrl, zipPath);

                    // Update progress window
                    _updateProgressWindow.StatusText.Text = "Download complete. Installing update...";

                    // Save the latest version to a file to ensure we know the correct version after update
                    try
                    {
                        // Create version file in the temp directory to be copied during update
                        File.WriteAllText(Path.Combine(_tempDownloadPath, "version.txt"), _latestVersion);
                    }
                    catch { /* Continue even if version file couldn't be created */ }

                    // Create updater PowerShell script
                    CreateUpdaterScript(zipPath);

                    // Run the updater script and exit the application
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
        }

        private void CreateUpdaterScript(string zipPath)
        {
            // Path to the PowerShell script file
            string psScriptPath = Path.Combine(_tempDownloadPath, "update.ps1");

            // Build the PowerShell script content with automatic update and direct extraction
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
    
    # Wait for main application to close
    Start-Sleep -Seconds 2
    Write-Log ""Waiting for application to close completely""
    
    # Save version file if it exists
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
    
    # List of files/directories to preserve (add any configuration or data files here)
    $filesToPreserve = @(
        # Example: ""config.xml"",
        # Example: ""userdata.db""
    )
    
    foreach ($file in $filesToPreserve) {
        $sourcePath = Join-Path ""$APPPATH$"" $file
        if (Test-Path -Path $sourcePath) {
            $destPath = Join-Path $backupDir $file
            Copy-Item -Path $sourcePath -Destination $destPath -Force -Recurse
            Write-Log ""Backed up: $file""
        }
    }
    
    # Delete everything in the application directory except the updater folder
    Write-Log ""Cleaning application directory""
    Get-ChildItem -Path ""$APPPATH$"" -Exclude ""CyberVaultUpdate"" | ForEach-Object {
        if ($_.FullName -ne $backupDir) {
            Remove-Item -Path $_.FullName -Recurse -Force
            Write-Log ""Removed: $($_.Name)""
        }
    }
    
    # Extract files directly to the application directory
    Write-Log ""Extracting update to application directory""
    Expand-Archive -Path ""$ZIPPATH$"" -DestinationPath ""$APPPATH$"" -Force
    Write-Log ""Extraction complete""
    
    # Restore version file
    if ($versionContent) {
        Write-Log ""Restoring version file""
        Set-Content -Path ""$APPPATH$\version.txt"" -Value $versionContent
    }
    
    # Restore preserved files
    Write-Log ""Restoring preserved files""
    foreach ($file in $filesToPreserve) {
        $sourcePath = Join-Path $backupDir $file
        if (Test-Path -Path $sourcePath) {
            $destPath = Join-Path ""$APPPATH$"" $file
            Copy-Item -Path $sourcePath -Destination $destPath -Force -Recurse
            Write-Log ""Restored: $file""
        }
    }
    
    # Cleanup temporary directories
    Write-Log ""Cleaning up temporary directories""
    if (Test-Path -Path $backupDir) {
        Remove-Item -Path $backupDir -Recurse -Force
    }
    Write-Log ""Cleanup complete""
    
    # Start the application automatically
    Write-Log ""Starting application""
    Start-Process -FilePath ""$APPPATH$\CyberVault.exe""
    Write-Log ""Application started""
    
    # Exit script
    Write-Log ""Update completed successfully""
    exit 0
}
catch {
    Write-Log ""Error during update: $_""
    exit 1
}
";

            // Replace placeholders with actual paths
            string logFilePath = Path.Combine(_tempDownloadPath, "update_log.txt");

            psScriptContent = psScriptContent.Replace("$LOGFILE$", logFilePath.Replace("\\", "\\\\"));
            psScriptContent = psScriptContent.Replace("$ZIPPATH$", zipPath.Replace("\\", "\\\\"));
            psScriptContent = psScriptContent.Replace("$APPPATH$", _applicationPath.Replace("\\", "\\\\"));
            psScriptContent = psScriptContent.Replace("$TEMPPATH$", _tempDownloadPath.Replace("\\", "\\\\"));

            // Write PowerShell script to file
            File.WriteAllText(psScriptPath, psScriptContent);

            // Create a small batch file launcher to hide the PowerShell window
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

                // Use ProcessStartInfo to hide the window
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
                client.DefaultRequestHeaders.Add("User-Agent", "CyberVault-Updater");

                // Create progress reporter
                var progress = new Progress<double>(percent => {
                    // Only update the dedicated progress window, not the main UI
                    if (_updateProgressWindow != null && _updateProgressWindow.IsVisible)
                    {
                        Application.Current.Dispatcher.Invoke(() => {
                            _updateProgressWindow.UpdateProgressBar.Value = percent * 100;
                            _updateProgressWindow.StatusText.Text = $"Downloading update: {percent:P0}";
                        });
                    }
                });

                // Download file with progress reporting
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
                                // If content length is unknown, just copy the stream
                                await contentStream.CopyToAsync(fileStream);
                            }
                            else
                            {
                                // If content length is known, report progress
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