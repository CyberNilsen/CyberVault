using CyberVault.Main;
using IWshRuntimeLibrary;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Windows.Security.Credentials.UI;
using Windows.Security.Credentials;
using System.Windows.Navigation;
using CyberVault.Viewmodel;
using Microsoft.Win32;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;



namespace CyberVault.Viewmodel
{
    public partial class Settings : System.Windows.Controls.UserControl, IDisposable
    {
        private bool disposed = false;

        private string? username;
        private byte[]? encryptionKey;
        private System.Timers.Timer? backupTimer;
        private string? backupLocation;
        private System.Timers.Timer? syncTimer;
        private string? syncLocation;
        private string? deviceName;
        private FileSystemWatcher? syncWatcher;
        private readonly object backupLock = new object();
        private readonly object syncLock = new object();
        private System.Timers.Timer? syncDebounceTimer;
        private volatile bool isBackupInProgress = false;
        private volatile bool isSyncInProgress = false;

        public Settings(string user, byte[] key)
        {
            InitializeComponent();
            username = user;
            encryptionKey = key;

            InitializeDeviceSync();

            LoadBackupSettings();
            LoadSyncSettings();

            Loaded += Settings_InitialBackupSync;
        }

        private async void Settings_InitialBackupSync(object sender, RoutedEventArgs e)
        {
            Loaded -= Settings_InitialBackupSync;

            await Task.Delay(2000);

            try
            {
                if (AutoBackupToggle?.IsChecked == true && !string.IsNullOrEmpty(backupLocation))
                {
                    await PerformBackup("login_backup");
                }

                if (!string.IsNullOrEmpty(syncLocation))
                {
                    await PerformSync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login backup/sync failed: {ex.Message}");
            }
        }

        private async Task PerformSync()
        {
            if (isSyncInProgress)
            {
                System.Diagnostics.Debug.WriteLine("Sync already in progress, skipping...");
                return;
            }

            lock (syncLock)
            {
                if (isSyncInProgress)
                    return;
                isSyncInProgress = true;
            }

            try
            {
                await PerformBidirectionalSync();

                string syncTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveUserSetting("LastSyncTime", syncTime);

                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            if (LastSyncTextBlock != null)
                                LastSyncTextBlock.Text = syncTime;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error updating sync UI: {ex.Message}");
                        }
                    });
                }

                System.Diagnostics.Debug.WriteLine("Sync completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sync failed: {ex.Message}");
            }
            finally
            {
                lock (syncLock)
                {
                    isSyncInProgress = false;
                }
            }
        }


        private void NotifyConflictResolution(string fileName, string winningDevice, string losingDevice)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show($"Sync conflict resolved for '{fileName}'.\nUsing version from: {winningDevice}\nOverriding version from: {losingDevice}",
                    "Sync Conflict Resolved", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
        public void Dispose()
        {
            try
            {
                StopBackupTimer();
                StopSyncWatcher();

                syncDebounceTimer?.Stop();
                syncDebounceTimer?.Dispose();
                syncDebounceTimer = null;

                syncWatcher?.Dispose();
                syncWatcher = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing Settings: {ex.Message}");
            }
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(username))
            {
                LoadSettingsFromFileAndAppState();
            }
        }

        private void LoadSettingsFromFileAndAppState()
        {
            try
            {
                Dictionary<string, bool> settings = new Dictionary<string, bool>();
                Dictionary<string, string> stringSettings = new Dictionary<string, string>();
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string settingsFilePath = Path.Combine(cyberVaultPath, $"{username}_settings.ini");

                if (System.IO.File.Exists(settingsFilePath))
                {
                    string[] lines = System.IO.File.ReadAllLines(settingsFilePath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            bool success = bool.TryParse(parts[1], out bool value);
                            if (success)
                            {
                                settings[parts[0]] = value;
                            }
                            else
                            {
                                stringSettings[parts[0]] = parts[1];
                            }
                        }
                    }
                }

                settings["MinimizeToTray"] = App.MinimizeToTrayEnabled;
                TemporarilyDisableToggleEvents();
                MinimizeToTrayToggle.IsChecked = settings.GetValueOrDefault("MinimizeToTray", false);
                StartWithWindowsToggle.IsChecked = settings.GetValueOrDefault("StartWithWindows", false);
                BiometricToggle.IsChecked = settings.GetValueOrDefault("BiometricEnabled", false);
                ReattachToggleEvents();

                string autoLockTime = stringSettings.GetValueOrDefault("AutoLockTime", "5 Minutes");
                if (AutoLockComboBox != null && AutoLockComboBox.Items.Count > 0)
                {
                    bool foundMatchingItem = false;
                    foreach (ComboBoxItem item in AutoLockComboBox.Items)
                    {
                        if (item.Content.ToString() == autoLockTime)
                        {
                            AutoLockComboBox.SelectedItem = item;
                            foundMatchingItem = true;
                            break;
                        }
                    }
                    if (!foundMatchingItem)
                    {
                        foreach (ComboBoxItem item in AutoLockComboBox.Items)
                        {
                            if (item.Content.ToString() == "5 Minutes")
                            {
                                AutoLockComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }

                string clipboardClearTime = stringSettings.GetValueOrDefault("ClipboardClearTime", "30 Seconds");
                if (ClipboardClearComboBox != null && ClipboardClearComboBox.Items.Count > 0)
                {
                    bool foundMatchingItem = false;
                    foreach (ComboBoxItem item in ClipboardClearComboBox.Items)
                    {
                        if (item.Content.ToString() == clipboardClearTime)
                        {
                            ClipboardClearComboBox.SelectedItem = item;
                            foundMatchingItem = true;
                            break;
                        }
                    }
                    if (!foundMatchingItem)
                    {
                        foreach (ComboBoxItem item in ClipboardClearComboBox.Items)
                        {
                            if (item.Content.ToString() == "30 Seconds")
                            {
                                ClipboardClearComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }

                MainWindow? mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    int seconds = 30;
                    switch (clipboardClearTime)
                    {
                        case "Never": seconds = 0; break;
                        case "15 Seconds": seconds = 15; break;
                        case "30 Seconds": seconds = 30; break;
                        case "1 Minute": seconds = 60; break;
                        case "2 Minutes": seconds = 120; break;
                        case "5 Minutes": seconds = 300; break;
                    }
                    mainWindow.UpdateClipboardClearTime(seconds);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading settings: {ex.Message}");
            }
        }

        private void TemporarilyDisableToggleEvents()
        {
            MinimizeToTrayToggle.Checked -= MinimizeToTrayToggle_Checked;
            MinimizeToTrayToggle.Unchecked -= MinimizeToTrayToggle_Unchecked;

            StartWithWindowsToggle.Checked -= StartWithWindowsToggle_Checked;
            StartWithWindowsToggle.Unchecked -= StartWithWindowsToggle_Unchecked;

            BiometricToggle.Checked -= BiometricToggle_Checked;
            BiometricToggle.Unchecked -= BiometricToggle_Unchecked;
        }

        private void ReattachToggleEvents()
        {
            MinimizeToTrayToggle.Checked += MinimizeToTrayToggle_Checked;
            MinimizeToTrayToggle.Unchecked += MinimizeToTrayToggle_Unchecked;

            StartWithWindowsToggle.Checked += StartWithWindowsToggle_Checked;
            StartWithWindowsToggle.Unchecked += StartWithWindowsToggle_Unchecked;

            BiometricToggle.Checked += BiometricToggle_Checked;
            BiometricToggle.Unchecked += BiometricToggle_Unchecked;
        }

        private void SaveUserSetting(string key, string value)
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");

                if (!Directory.Exists(cyberVaultPath))
                {
                    Directory.CreateDirectory(cyberVaultPath);
                }

                string settingsFilePath = Path.Combine(cyberVaultPath, $"{username}_settings.ini");

                Dictionary<string, string> settings = new Dictionary<string, string>();

                if (System.IO.File.Exists(settingsFilePath))
                {
                    string[] existingLines = System.IO.File.ReadAllLines(settingsFilePath);
                    foreach (string line in existingLines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains('='))
                            continue;

                        string[] parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            settings[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }

                settings[key] = value;

                List<string> outputLines = new List<string>();
                foreach (var kvp in settings)
                {
                    outputLines.Add($"{kvp.Key}={kvp.Value}");
                }

                System.IO.File.WriteAllLines(settingsFilePath, outputLines);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving setting: {ex.Message}", "Settings Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MinimizeToTrayToggle_Checked(object sender, RoutedEventArgs e)
        {
            App.MinimizeToTrayEnabled = true;
            SaveUserSetting("MinimizeToTray", "True");
        }

        private void MinimizeToTrayToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            App.MinimizeToTrayEnabled = false;
            SaveUserSetting("MinimizeToTray", "False");
        }

        private void StartWithWindowsToggle_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                string appName = "CyberVault";
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;

                using (Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    key?.SetValue(appName, exePath);
                }
                SaveUserSetting("StartWithWindows", "True");
            }
            catch (Exception )
            {
                StartWithWindowsToggle.Checked -= StartWithWindowsToggle_Checked;
                StartWithWindowsToggle.IsChecked = false;
                StartWithWindowsToggle.Checked += StartWithWindowsToggle_Checked;
            }
        }

        private void StartWithWindowsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string appName = "CyberVault";

                using (Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    key?.DeleteValue(appName, false);
                }
                SaveUserSetting("StartWithWindows", "False");
            }
            catch (Exception )
            {
                StartWithWindowsToggle.Checked -= StartWithWindowsToggle_Checked;
                StartWithWindowsToggle.IsChecked = false;
                StartWithWindowsToggle.Checked += StartWithWindowsToggle_Checked;
            }
        }

        private void AutoLockComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AutoLockComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectionText = selectedItem.Content.ToString()!;
                SaveUserSetting("AutoLockTime", selectionText);

                MainWindow? mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null && !string.IsNullOrEmpty(username))
                {
                    int minutes = 5;

                    switch (selectionText)
                    {
                        case "Never": minutes = 0; break;
                        case "1 Minute": minutes = 1; break;
                        case "5 Minutes": minutes = 5; break;
                        case "15 Minutes": minutes = 15; break;
                        case "30 Minutes": minutes = 30; break;
                        case "1 Hour": minutes = 60; break;
                    }

                    mainWindow.UserLoggedIn(username, minutes);
                }
            }
        }

        private void ClipboardClearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClipboardClearComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectionText = selectedItem.Content.ToString()!;
                SaveUserSetting("ClipboardClearTime", selectionText);

                MainWindow? mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    int seconds = 30;
                    switch (selectionText)
                    {
                        case "Never": seconds = 0; break;
                        case "15 Seconds": seconds = 15; break;
                        case "30 Seconds": seconds = 30; break;
                        case "1 Minute": seconds = 60; break;
                        case "2 Minutes": seconds = 120; break;
                        case "5 Minutes": seconds = 300; break;
                    }
                    mainWindow.UpdateClipboardClearTime(seconds);
                }
            }
        }

        private async void BiometricToggle_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                var availabilityResult = await Windows.Security.Credentials.UI.UserConsentVerifier.CheckAvailabilityAsync();

                if (availabilityResult != Windows.Security.Credentials.UI.UserConsentVerifierAvailability.Available)
                {
                    System.Windows.MessageBox.Show("Windows Hello is not available on this device or is not properly configured. Please set up Windows Hello in your Windows settings.",
                        "Windows Hello Not Available", MessageBoxButton.OK, MessageBoxImage.Warning);

                    BiometricToggle.Checked -= BiometricToggle_Checked;
                    BiometricToggle.IsChecked = false;
                    BiometricToggle.Checked += BiometricToggle_Checked;

                    return;
                }

                var consentResult = await Windows.Security.Credentials.UI.UserConsentVerifier.RequestVerificationAsync(
                    "Please verify your identity to enable biometric login");

                if (consentResult == Windows.Security.Credentials.UI.UserConsentVerificationResult.Verified)
                {
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");

                    var passwordWindow = new PasswordConfirmationDialog();
                    passwordWindow.Owner = Window.GetWindow(this);
                    bool? result = passwordWindow.ShowDialog();

                    if (result == true && !string.IsNullOrEmpty(passwordWindow.Password))
                    {
                        string credentialsFilePath = Path.Combine(cyberVaultPath, "credentials.txt");
                        bool passwordVerified = false;

                        if (System.IO.File.Exists(credentialsFilePath))
                        {
                            string[] lines = System.IO.File.ReadAllLines(credentialsFilePath);

                            foreach (string line in lines)
                            {
                                if (line.StartsWith(username + ","))
                                {
                                    string[] parts = line.Split(',');
                                    if (parts.Length >= 3)
                                    {
                                        byte[] salt = Convert.FromBase64String(parts[1]);
                                        string storedHash = parts[2];

                                        byte[] enteredKey = KeyDerivation.DeriveKey(passwordWindow.Password, salt);
                                        string enteredHash = Convert.ToBase64String(enteredKey);

                                        if (enteredHash == storedHash)
                                        {
                                            passwordVerified = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (!passwordVerified)
                        {
                            System.Windows.MessageBox.Show("Incorrect master password. Biometric login not enabled.",
                                "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Error);

                            BiometricToggle.Checked -= BiometricToggle_Checked;
                            BiometricToggle.IsChecked = false;
                            BiometricToggle.Checked += BiometricToggle_Checked;

                            SaveUserSetting("BiometricEnabled", "False");
                            return;
                        }

                        byte[] encodedPassword = System.Text.Encoding.UTF8.GetBytes(passwordWindow.Password);
                        byte[] encryptedPassword = System.Security.Cryptography.ProtectedData.Protect(
                            encodedPassword,
                            null,
                            System.Security.Cryptography.DataProtectionScope.CurrentUser);

                        string bioConfigPath = Path.Combine(cyberVaultPath, "biometric_config.txt");
                        System.IO.File.WriteAllText(bioConfigPath, $"{username},{Convert.ToBase64String(encryptedPassword)}");

                        SaveUserSetting("BiometricEnabled", "True");
                        System.Windows.MessageBox.Show("Biometric login has been enabled successfully.",
                            "Biometric Setup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        BiometricToggle.Checked -= BiometricToggle_Checked;
                        BiometricToggle.IsChecked = false;
                        BiometricToggle.Checked += BiometricToggle_Checked;

                        SaveUserSetting("BiometricEnabled", "False");
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Windows Hello verification failed. Biometric login was not enabled.",
                    "Verification Failed", MessageBoxButton.OK, MessageBoxImage.Warning);

                    BiometricToggle.Checked -= BiometricToggle_Checked;
                    BiometricToggle.IsChecked = false;
                    BiometricToggle.Checked += BiometricToggle_Checked;

                    SaveUserSetting("BiometricEnabled", "False");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error setting up biometric authentication: {ex.Message}",
                "Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);

                BiometricToggle.Checked -= BiometricToggle_Checked;
                BiometricToggle.IsChecked = false;
                BiometricToggle.Checked += BiometricToggle_Checked;

                SaveUserSetting("BiometricEnabled", "False");
            }
        }

        private void BiometricToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string bioConfigPath = Path.Combine(cyberVaultPath, "biometric_config.txt");

                if (System.IO.File.Exists(bioConfigPath))
                {
                    System.IO.File.Delete(bioConfigPath);
                }

                SaveUserSetting("BiometricEnabled", "False");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error removing biometric config: {ex.Message}");
            }
        }

        private void ChangeMasterPassword_Click(object sender, RoutedEventArgs e)
        {
              var result = System.Windows.MessageBox.Show(
             "Are you sure you want to change your master password?\nThis will re-encrypt all your stored data.",
             "Change Master Password",
             MessageBoxButton.YesNoCancel,
             MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var passwordWindow = new MasterPassword(username!, encryptionKey!);
                passwordWindow.Owner = Window.GetWindow(this);
                bool? dialogResult = passwordWindow.ShowDialog();

                if (dialogResult == true && !string.IsNullOrEmpty(passwordWindow.OldPassword) && !string.IsNullOrEmpty(passwordWindow.NewPassword))
                {
                    try
                    {
                        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                        string passwordsFilePath = Path.Combine(cyberVaultPath, $"passwords_{username}.dat");

                        List<PasswordItem> existingPasswords = PasswordStorage.LoadPasswords(username!, encryptionKey!);

                        string credentialsFilePath = Path.Combine(cyberVaultPath, "credentials.txt");
                        string[] lines = System.IO.File.ReadAllLines(credentialsFilePath);
                        byte[] newSalt = null!;

                        foreach (string line in lines)
                        {
                            if (line.StartsWith(username + ","))
                            {
                                string[] parts = line.Split(',');
                                newSalt = Convert.FromBase64String(parts[1]);
                                break;
                            }
                        }

                        if (newSalt == null)
                        {
                            throw new Exception("Could not find salt for user credentials");
                        }

                        byte[] newEncryptionKey = KeyDerivation.DeriveKey(passwordWindow.NewPassword, newSalt);

                        PasswordStorage.SavePasswords(existingPasswords, username!, newEncryptionKey);

                        ReencryptAuthenticatorsFile(username!, encryptionKey!, newEncryptionKey);

                        encryptionKey = newEncryptionKey;

                        System.Windows.MessageBox.Show("Master password changed successfully.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error changing master password: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                }
                else if (result == MessageBoxResult.No)
                {
                    return;
                }
                else if (result == MessageBoxResult.None)
                {
                    return;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }

            }
        }

        private void ReencryptAuthenticatorsFile(string username, byte[] oldKey, byte[] newKey)
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string authFilePath = Path.Combine(cyberVaultPath, $"{username}.authenticators.enc");

                if (!System.IO.File.Exists(authFilePath))
                {
                    return;
                }

                byte[] encryptedData = System.IO.File.ReadAllBytes(authFilePath);

                
                byte[] iv = new byte[16];
                byte[] actualEncryptedData = new byte[encryptedData.Length - 16];

                Array.Copy(encryptedData, 0, iv, 0, 16);
                Array.Copy(encryptedData, 16, actualEncryptedData, 0, actualEncryptedData.Length);

                string decryptedString = AesEncryption.Decrypt(actualEncryptedData, oldKey, iv);

                byte[] decryptedData = System.Text.Encoding.UTF8.GetBytes(decryptedString);

                byte[] newIv = AesEncryption.GenerateIV();

                byte[] newEncryptedData = AesEncryption.Encrypt(System.Text.Encoding.UTF8.GetString(decryptedData), newKey, newIv);

                byte[] finalData = new byte[16 + newEncryptedData.Length];
                Array.Copy(newIv, 0, finalData, 0, 16);
                Array.Copy(newEncryptedData, 0, finalData, 16, newEncryptedData.Length);

                System.IO.File.WriteAllBytes(authFilePath, finalData);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error re-encrypting authenticators: {ex.Message}");
            }
        }

        private void LoadBackupSettings()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");

                if (!Directory.Exists(cyberVaultPath))
                {
                    Directory.CreateDirectory(cyberVaultPath);
                }

                string settingsFilePath = Path.Combine(cyberVaultPath, $"{username}_settings.ini");

                if (System.IO.File.Exists(settingsFilePath))
                {
                    string[] lines = System.IO.File.ReadAllLines(settingsFilePath);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains('='))
                            continue;

                        string[] parts = line.Split('=', 2); 
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();

                            switch (key)
                            {
                                case "AutoBackupEnabled":
                                    if (bool.TryParse(value, out bool autoBackup))
                                    {
                                        AutoBackupToggle.IsChecked = autoBackup;
                                    }
                                    break;
                                case "BackupLocation":
                                    backupLocation = value;
                                    BackupLocationTextBox.Text = value;
                                    break;
                                case "BackupFrequency":
                                    SetComboBoxSelection(BackupFrequencyComboBox, value);
                                    break;
                                case "BackupRetention":
                                    SetComboBoxSelection(BackupRetentionComboBox, value);
                                    break;
                                case "LastBackupTime":
                                    if (DateTime.TryParse(value, out DateTime lastBackup))
                                    {
                                        LastBackupTextBlock.Text = lastBackup.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                    break;
                            }
                        }
                    }
                }

                if (AutoBackupToggle.IsChecked == true)
                {
                    StartBackupTimer();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading backup settings: {ex.Message}", "Settings Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SetComboBoxSelection(System.Windows.Controls.ComboBox comboBox, string value)
        {
            if (comboBox?.Items == null) return;

            foreach (ComboBoxItem item in comboBox.Items.OfType<ComboBoxItem>())
            {
                if (item.Content?.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void AutoBackupToggle_Checked(object sender, RoutedEventArgs e)
        {
            SaveUserSetting("AutoBackupEnabled", "True");
            StartBackupTimer();
        }

        private void AutoBackupToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveUserSetting("AutoBackupEnabled", "False");
            StopBackupTimer();
        }


        private void BackupFrequencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BackupFrequencyComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                SaveUserSetting("BackupFrequency", selectedItem.Content?.ToString() ?? "Weekly");
                if (AutoBackupToggle.IsChecked == true)
                {
                    StartBackupTimer();
                }
            }
        }

        private void BackupRetentionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BackupRetentionComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                SaveUserSetting("BackupRetention", selectedItem.Content?.ToString() ?? "10");
            }
        }

        private void BrowseBackupLocation_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select backup location",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                backupLocation = folderDialog.SelectedPath;
                BackupLocationTextBox.Text = backupLocation;
                SaveUserSetting("BackupLocation", backupLocation);
            }
        }

        private async void BackupNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as System.Windows.Controls.Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Content = "Backing up...";
                }

                await PerformBackup("manual_backup");

                System.Windows.MessageBox.Show("Backup completed successfully!", "Backup",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                var button = sender as System.Windows.Controls.Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "Backup Now";
                }
            }
        }


        private void RestoreFromBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CyberVault Backup Files (*.cvbackup)|*.cvbackup|All files (*.*)|*.*",
                    Title = "Select backup file to restore"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var result = System.Windows.MessageBox.Show(
                        "This will restore your vault data from the selected backup. Your current data will be backed up first. Continue?",
                        "Confirm Restore",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        _ = Task.Run(async () => await PerformBackup("pre_restore_backup"));

                        RestoreFromBackupFile(openFileDialog.FileName);

                        System.Windows.MessageBox.Show("Backup restored successfully! Please restart the application.",
                            "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error restoring backup: {ex.Message}",
                    "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void StartBackupTimer()
        {
            StopBackupTimer();

            if (string.IsNullOrEmpty(backupLocation))
                return;

            string frequency = "Weekly"; 

            try
            {
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        frequency = ((ComboBoxItem)BackupFrequencyComboBox.SelectedItem)?.Content?.ToString() ?? "Weekly";
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting backup frequency: {ex.Message}");
            }

            TimeSpan interval = frequency switch
            {
                "Daily" => TimeSpan.FromDays(1),
                "Weekly" => TimeSpan.FromDays(7),
                "Monthly" => TimeSpan.FromDays(30),
                _ => TimeSpan.FromDays(7)
            };

            backupTimer = new System.Timers.Timer(interval.TotalMilliseconds);
            backupTimer.Elapsed += async (s, e) =>
            {
                try
                {
                    await PerformBackup("auto_backup");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Automatic backup failed: {ex.Message}");
                }
            };
            backupTimer.AutoReset = true;
            backupTimer.Start();

            System.Diagnostics.Debug.WriteLine($"Backup timer started with {frequency} frequency");
        }


        private void StopBackupTimer()
        {
            if (backupTimer != null)
            {
                backupTimer.Stop();
                backupTimer.Dispose();
                backupTimer = null;
            }
        }

        private void CleanupOldBackups()
        {
            try
            {
                if (string.IsNullOrEmpty(backupLocation))
                    return;

                int retention = 10; 

                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (BackupRetentionComboBox?.SelectedItem is ComboBoxItem selectedItem)
                            {
                                if (!int.TryParse(selectedItem.Content?.ToString(), out retention))
                                    retention = 10;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error getting retention setting: {ex.Message}");
                        }
                    });
                }

                var backupFiles = Directory.GetFiles(backupLocation, "CyberVault_backup_*.cvbackup")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(retention)
                    .ToList();

                int deletedCount = 0;
                foreach (var file in backupFiles)
                {
                    try
                    {
                        file.Delete();
                        deletedCount++;
                    }
                    catch (IOException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Could not delete backup file {file.Name}: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Access denied deleting backup file {file.Name}: {ex.Message}");
                    }
                }

                if (deletedCount > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Cleaned up {deletedCount} old backup files");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up old backups: {ex.Message}");
            }
        }

        private void RestoreFromBackupFile(string backupFilePath)
        {
            if (!System.IO.File.Exists(backupFilePath))
                throw new FileNotFoundException("Backup file not found");

            string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CyberVault");
            string tempDir = Path.Combine(Path.GetTempPath(), "CyberVault_Restore_" + Guid.NewGuid().ToString("N")[..8]);

            try
            {
                ZipFile.ExtractToDirectory(backupFilePath, tempDir);

                if (Directory.Exists(targetDir))
                {
                    string backupCurrentDir = targetDir + "_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    Directory.Move(targetDir, backupCurrentDir);
                }

                Directory.Move(tempDir, targetDir);
            }
            catch
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                throw;
            }
        }

        private void LoadSyncSettings()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string settingsFilePath = Path.Combine(cyberVaultPath, $"{username}_settings.ini");

                if (System.IO.File.Exists(settingsFilePath))
                {
                    string[] lines = System.IO.File.ReadAllLines(settingsFilePath);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains('='))
                            continue;

                        string[] parts = line.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();

                            switch (key)
                            {
                                case "AutoSyncEnabled":
                                    if (bool.TryParse(value, out bool autoSync))
                                    {
                                        if (AutoSyncToggle != null)
                                            AutoSyncToggle.IsChecked = autoSync;
                                    }
                                    break;
                                case "SyncLocation":
                                    syncLocation = value;
                                    if (SyncLocationTextBox != null)
                                        SyncLocationTextBox.Text = value;
                                    break;
                                case "DeviceName":
                                    deviceName = value;
                                    if (DeviceNameTextBox != null)
                                        DeviceNameTextBox.Text = value;
                                    break;
                                case "LastSyncTime":
                                    if (LastSyncTextBlock != null)
                                        LastSyncTextBlock.Text = value;
                                    break;
                            }
                        }
                    }
                }

                if (AutoSyncToggle?.IsChecked == true && !string.IsNullOrEmpty(syncLocation))
                {
                    StartSyncWatcher();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading sync settings: {ex.Message}", "Settings Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AutoSyncToggle_Checked(object sender, RoutedEventArgs e)
        {
            SaveUserSetting("AutoSyncEnabled", "True");
            StartSyncWatcher();
        }

        private void AutoSyncToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveUserSetting("AutoSyncEnabled", "False");
            StopSyncWatcher();
        }

        private void BrowseSyncLocation_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select sync location",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                syncLocation = folderDialog.SelectedPath;

                if (SyncLocationTextBox != null)
                {
                    SyncLocationTextBox.Text = syncLocation;
                }

                SaveUserSetting("SyncLocation", syncLocation);

                if (AutoSyncToggle?.IsChecked == true)
                {
                    StartSyncWatcher();
                }
            }
        }

        private void DeviceNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            deviceName = DeviceNameTextBox.Text;
            SaveUserSetting("DeviceName", deviceName);
        }

        private async void SyncNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as System.Windows.Controls.Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Content = "Syncing...";
                }

                await PerformSync();

                System.Windows.MessageBox.Show("Sync completed successfully!", "Sync",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Sync failed: {ex.Message}", "Sync Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                var button = sender as System.Windows.Controls.Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "Sync Now";
                }
            }
        }

        private void StartSyncWatcher()
        {
            StopSyncWatcher();

            if (string.IsNullOrEmpty(syncLocation))
                return;

            try
            {
                string sourceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CyberVault");

                syncWatcher = new FileSystemWatcher(sourceDir)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    Filter = "*.*",
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };

                syncWatcher.Changed += OnSyncFileChanged;
                syncWatcher.Created += OnSyncFileChanged;
                syncWatcher.Deleted += OnSyncFileChanged;
                syncWatcher.Renamed += OnSyncFileRenamed;

                syncTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
                syncTimer.Elapsed += async (s, e) => await PerformSync();
                syncTimer.AutoReset = true;
                syncTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting sync watcher: {ex.Message}");
            }
        }

        private void StopSyncWatcher()
        {
            syncWatcher?.Dispose();
            syncWatcher = null;

            syncTimer?.Stop();
            syncTimer?.Dispose();
            syncTimer = null;
        }

        private async void OnSyncFileChanged(object sender, FileSystemEventArgs e)
        {
            syncDebounceTimer?.Stop();
            syncDebounceTimer?.Dispose();

            syncDebounceTimer = new System.Timers.Timer(5000);
            syncDebounceTimer.Elapsed += async (s, args) =>
            {
                syncDebounceTimer?.Stop();
                syncDebounceTimer?.Dispose();
                syncDebounceTimer = null;

                await PerformSync();
            };
            syncDebounceTimer.AutoReset = false;
            syncDebounceTimer.Start();
        }

        private void OnSyncFileRenamed(object sender, RenamedEventArgs e)
        {
            OnSyncFileChanged(sender, e);
        }


        private async Task PerformBidirectionalSync()
        {
            if (string.IsNullOrEmpty(syncLocation) || string.IsNullOrEmpty(deviceName))
                return;

            try
            {
                string localDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CyberVault");
                string deviceSyncDir = Path.Combine(syncLocation, deviceName);
                string sharedSyncDir = Path.Combine(syncLocation, "Shared");

                if (!Directory.Exists(localDir))
                    return;

                Directory.CreateDirectory(deviceSyncDir);
                Directory.CreateDirectory(sharedSyncDir);

                await Task.Run(() =>
                {
                    SyncToDevice(localDir, deviceSyncDir);

                    MergeDeviceChanges(syncLocation, sharedSyncDir);

                    SyncFromShared(sharedSyncDir, localDir);
                });

                SaveUserSetting("LastSyncTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bidirectional sync failed: {ex.Message}");
            }
        }

        private class FileConflictInfo
        {
            public string DeviceName { get; set; }
            public DateTime SyncTime { get; set; }
            public DateTime ModificationTime { get; set; }
            public string FilePath { get; set; }
        }

        private void InitializeDeviceSync()
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                deviceName = $"{Environment.MachineName}_{Environment.UserName}_{DateTime.Now:yyyyMMdd}";
                SaveUserSetting("DeviceName", deviceName);
            }
        }

        private void SyncToDevice(string localDir, string deviceDir)
        {
            foreach (string file in Directory.GetFiles(localDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(localDir, file);
                string targetFile = Path.Combine(deviceDir, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                if (!System.IO.File.Exists(targetFile) || System.IO.File.GetLastWriteTime(file) > System.IO.File.GetLastWriteTime(targetFile))
                {
                    System.IO.File.Copy(file, targetFile, true);

                    string metadataFile = targetFile + ".meta";
                    System.IO.File.WriteAllText(metadataFile, $"{deviceName}|{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{System.IO.File.GetLastWriteTime(file):yyyy-MM-dd HH:mm:ss}");
                }
            }
        }

        private void MergeDeviceChanges(string syncLocation, string sharedDir)
        {
            var deviceDirs = Directory.GetDirectories(syncLocation)
                .Where(d => !Path.GetFileName(d).Equals("Shared", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Dictionary<string, List<FileConflictInfo>> allFileVersions = new Dictionary<string, List<FileConflictInfo>>();

            foreach (string deviceDir in deviceDirs)
            {
                string currentDeviceName = Path.GetFileName(deviceDir);

                foreach (string file in Directory.GetFiles(deviceDir, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".meta")) continue;

                    string relativePath = Path.GetRelativePath(deviceDir, file);
                    string metadataFile = file + ".meta";

                    if (System.IO.File.Exists(metadataFile))
                    {
                        string[] metadata = System.IO.File.ReadAllText(metadataFile).Split('|');
                        if (metadata.Length >= 3)
                        {
                            var conflictInfo = new FileConflictInfo
                            {
                                DeviceName = metadata[0],
                                SyncTime = DateTime.Parse(metadata[1]),
                                ModificationTime = DateTime.Parse(metadata[2]),
                                FilePath = file
                            };

                            if (!allFileVersions.ContainsKey(relativePath))
                                allFileVersions[relativePath] = new List<FileConflictInfo>();

                            allFileVersions[relativePath].Add(conflictInfo);
                        }
                    }
                }
            }

            foreach (var kvp in allFileVersions)
            {
                string relativePath = kvp.Key;
                var versions = kvp.Value.OrderByDescending(v => v.ModificationTime).ToList();

                if (versions.Count > 1)
                {
                    var winner = versions[0];
                    var loser = versions[1];
                    NotifyConflictResolution(relativePath, winner.DeviceName, loser.DeviceName);
                }

                string targetFile = Path.Combine(sharedDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                System.IO.File.Copy(versions[0].FilePath, targetFile, true);
            }
        }

        private void SyncFromShared(string sharedDir, string localDir)
        {
            if (!Directory.Exists(sharedDir))
                return;

            foreach (string file in Directory.GetFiles(sharedDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sharedDir, file);
                string localFile = Path.Combine(localDir, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(localFile));

                if (!System.IO.File.Exists(localFile) || System.IO.File.GetLastWriteTime(file) > System.IO.File.GetLastWriteTime(localFile))
                {
                    System.IO.File.Copy(file, localFile, true);
                }
            }
        }


        private void SyncDirectory(string sourceDir, string targetDir)
        {
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string targetFile = Path.Combine(targetDir, fileName);

                if (!System.IO.File.Exists(targetFile) || System.IO.File.GetLastWriteTime(file) > System.IO.File.GetLastWriteTime(targetFile))
                {
                    System.IO.File.Copy(file, targetFile, true);
                }
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string targetSubDir = Path.Combine(targetDir, dirName);

                if (!Directory.Exists(targetSubDir))
                    Directory.CreateDirectory(targetSubDir);

                SyncDirectory(subDir, targetSubDir);
            }
        }

        private async Task UploadLocalBackups()
        {
            try
            {
                if (string.IsNullOrEmpty(backupLocation) || string.IsNullOrEmpty(syncLocation))
                    return;

                string deviceSyncFolder = Path.Combine(syncLocation, $"CyberVault_Backups_{deviceName}");
                Directory.CreateDirectory(deviceSyncFolder);

                var localBackups = Directory.GetFiles(backupLocation, "*.cvbackup");

                foreach (string localBackup in localBackups)
                {
                    string fileName = Path.GetFileName(localBackup);
                    string syncFilePath = Path.Combine(deviceSyncFolder, fileName);

                    if (!System.IO.File.Exists(syncFilePath) ||
                        System.IO.File.GetLastWriteTime(localBackup) > System.IO.File.GetLastWriteTime(syncFilePath))
                    {
                        await Task.Run(() => System.IO.File.Copy(localBackup, syncFilePath, true));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to upload local backups: {ex.Message}");
            }
        }

        private async Task DownloadRemoteBackups()
        {
            try
            {
                if (string.IsNullOrEmpty(backupLocation) || string.IsNullOrEmpty(syncLocation))
                    return;

                string remoteBackupsFolder = Path.Combine(backupLocation, "Remote_Backups");
                Directory.CreateDirectory(remoteBackupsFolder);

                var deviceFolders = Directory.GetDirectories(syncLocation, "CyberVault_Backups_*")
                    .Where(folder => !folder.EndsWith($"CyberVault_Backups_{deviceName}"));

                foreach (string deviceFolder in deviceFolders)
                {
                    string deviceFolderName = Path.GetFileName(deviceFolder);
                    string localDeviceFolder = Path.Combine(remoteBackupsFolder, deviceFolderName);
                    Directory.CreateDirectory(localDeviceFolder);

                    var remoteBackups = Directory.GetFiles(deviceFolder, "*.cvbackup");

                    foreach (string remoteBackup in remoteBackups)
                    {
                        string fileName = Path.GetFileName(remoteBackup);
                        string localFilePath = Path.Combine(localDeviceFolder, fileName);

                        if (!System.IO.File.Exists(localFilePath) ||
                            System.IO.File.GetLastWriteTime(remoteBackup) > System.IO.File.GetLastWriteTime(localFilePath))
                        {
                            await Task.Run(() => System.IO.File.Copy(remoteBackup, localFilePath, true));
                        }
                    }

                    CleanupRemoteBackups(localDeviceFolder);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download remote backups: {ex.Message}");
            }
        }

        private void CleanupRemoteBackups(string deviceFolder)
        {
            try
            {
                var backupFiles = Directory.GetFiles(deviceFolder, "*.cvbackup")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(5) 
                    .ToList();

                foreach (var file in backupFiles)
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up remote backups: {ex.Message}");
            }
        }

        private async Task PerformBackup(string suffix = "")
        {
            if (isBackupInProgress)
            {
                System.Diagnostics.Debug.WriteLine("Backup already in progress, skipping...");
                return;
            }

            lock (backupLock)
            {
                if (isBackupInProgress)
                    return;
                isBackupInProgress = true;
            }

            try
            {
                if (string.IsNullOrEmpty(backupLocation) || string.IsNullOrEmpty(username))
                    throw new InvalidOperationException("Backup location or username not set");

                string sourceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CyberVault");

                if (!Directory.Exists(sourceDir))
                    throw new DirectoryNotFoundException("CyberVault data directory not found");

                if (!Directory.Exists(backupLocation))
                    Directory.CreateDirectory(backupLocation);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = string.IsNullOrEmpty(suffix)
                    ? $"CyberVault_backup_{timestamp}.cvbackup"
                    : $"CyberVault_{suffix}_{timestamp}.cvbackup";

                string backupFilePath = Path.Combine(backupLocation, backupFileName);

                await Task.Run(() =>
                {
                    int maxRetries = 5;
                    for (int retry = 0; retry < maxRetries; retry++)
                    {
                        try
                        {
                            if (retry > 0)
                            {
                                Thread.Sleep(1000 * retry);
                            }

                            ZipFile.CreateFromDirectory(sourceDir, backupFilePath, CompressionLevel.Optimal, false);
                            break; 
                        }
                        catch (IOException ex) when (retry < maxRetries - 1)
                        {
                            System.Diagnostics.Debug.WriteLine($"Backup retry {retry + 1}: {ex.Message}");
                            continue; 
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Backup failed on retry {retry + 1}: {ex.Message}");
                            if (retry == maxRetries - 1)
                                throw; 
                        }
                    }
                });

                SaveUserSetting("LastBackupTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            if (LastBackupTextBlock != null)
                                LastBackupTextBlock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error updating UI: {ex.Message}");
                        }
                    });
                }

                _ = Task.Run(() => CleanupOldBackups());

                System.Diagnostics.Debug.WriteLine($"Backup completed successfully: {backupFileName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backup failed: {ex.Message}");
                throw;
            }
            finally
            {
                lock (backupLock)
                {
                    isBackupInProgress = false;
                }
            }
        }


        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string sourceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CyberVault");
                if (!System.IO.Directory.Exists(sourceDir))
                {
                    System.Windows.MessageBox.Show("No files available to export. The CyberVault directory does not exist.",
                        "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                string defaultExportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CyberVault_Export");

                var result = System.Windows.MessageBox.Show(
                    $"Do you want to export to the default location?\n\n{defaultExportDir}\n\nClick 'Yes' to use default location or 'No' to choose a custom location.",
                    "Export Location", System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Cancel)
                {
                    return;
                }

                string destinationDir;

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    destinationDir = defaultExportDir;
                    if (!System.IO.Directory.Exists(destinationDir))
                    {
                        System.IO.Directory.CreateDirectory(destinationDir);
                    }
                }
                else
                {
                    var folderDialog = new FolderBrowserDialog
                    {
                        Description = "Select destination folder for exported user data",
                        UseDescriptionForTitle = true
                    };

                    if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        string selectedPath = folderDialog.SelectedPath;
                        destinationDir = Path.Combine(selectedPath, "CyberVault_Export");
                    }
                    else
                    {
                        return;
                    }
                }

                if (!System.IO.Directory.Exists(destinationDir))
                {
                    System.IO.Directory.CreateDirectory(destinationDir);
                }

                bool foundUserData = false;

                string userExportFile = Path.Combine(destinationDir, $"{username}_exported.txt");
                string credentialsPath = Path.Combine(sourceDir, "credentials.txt");

                if (System.IO.File.Exists(credentialsPath))
                {
                    string[] allCredentials = System.IO.File.ReadAllLines(credentialsPath);
                    foreach (string line in allCredentials)
                    {
                        if (line.StartsWith(username + ","))
                        {
                            System.IO.File.WriteAllText(userExportFile, line);
                            foundUserData = true;
                            break;
                        }
                    }
                }

                string passwordFile = Path.Combine(sourceDir, $"passwords_{username}.dat");
                if (System.IO.File.Exists(passwordFile))
                {
                    string destPasswordFile = Path.Combine(destinationDir, $"passwords_{username}.dat");
                    System.IO.File.Copy(passwordFile, destPasswordFile, true);
                    foundUserData = true;
                }

                string settingsIniFile = Path.Combine(sourceDir, $"{username}_settings.ini");
                if (System.IO.File.Exists(settingsIniFile))
                {
                    string destSettingsIniFile = Path.Combine(destinationDir, $"{username}_settings.ini");
                    System.IO.File.Copy(settingsIniFile, destSettingsIniFile, true);
                    foundUserData = true;
                }

                string authEncFile = Path.Combine(sourceDir, $"{username}.authenticators.enc");
                if (System.IO.File.Exists(authEncFile))
                {
                    string destAuthEncFile = Path.Combine(destinationDir, $"{username}.authenticators.enc");
                    System.IO.File.Copy(authEncFile, destAuthEncFile, true);
                    foundUserData = true;
                }

                string[] encFiles = System.IO.Directory.GetFiles(sourceDir, $"{username}*.enc");
                foreach (string encFile in encFiles)
                {
                    string fileName = Path.GetFileName(encFile);
                    string destEncFile = Path.Combine(destinationDir, fileName);
                    if (!System.IO.File.Exists(destEncFile))
                    {
                        System.IO.File.Copy(encFile, destEncFile, true);
                        foundUserData = true;
                    }
                }

                if (foundUserData)
                {
                    System.Windows.MessageBox.Show($"User data for '{username}' exported successfully to:\n{destinationDir}",
                        "Export Complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show($"No data found for user '{username}'.",
                        "Export Information", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error exporting user data: {ex.Message}",
                    "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ImportData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderDialog = new FolderBrowserDialog
                {
                    Description = "Select folder containing CyberVault exported data",
                    UseDescriptionForTitle = true
                };

                if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                string importDir = folderDialog.SelectedPath;

                string[] exportedFiles = System.IO.Directory.GetFiles(importDir, "*_exported.txt");
                if (exportedFiles.Length == 0)
                {
                    System.Windows.MessageBox.Show("No exported user data found in the selected folder.\n" +
                        "Please select a folder containing CyberVault exported data.",
                        "Import Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");

                if (!System.IO.Directory.Exists(cyberVaultPath))
                {
                    System.IO.Directory.CreateDirectory(cyberVaultPath);
                }

                string credentialsPath = Path.Combine(cyberVaultPath, "credentials.txt");
                List<string> existingUsernames = new List<string>();
                List<string> existingCredentials = new List<string>();

                if (System.IO.File.Exists(credentialsPath))
                {
                    existingCredentials = System.IO.File.ReadAllLines(credentialsPath).ToList();
                    foreach (string line in existingCredentials)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line.Contains(","))
                        {
                            string existingUsername = line.Split(',')[0];
                            existingUsernames.Add(existingUsername);
                        }
                    }
                }

                int importedUsers = 0;
                List<string> importedUserNames = new List<string>();
                List<string> skippedUserNames = new List<string>();
                List<string> updatedCredentials = new List<string>(existingCredentials);

                foreach (string exportedFile in exportedFiles)
                {
                    try
                    {
                        string exportedCredential = System.IO.File.ReadAllText(exportedFile).Trim();

                        if (string.IsNullOrWhiteSpace(exportedCredential))
                        {
                            continue;
                        }

                        string[] parts = exportedCredential.Split(',');
                        if (parts.Length < 2)
                        {
                            continue;
                        }

                        string importUsername = parts[0];

                        if (existingUsernames.Contains(importUsername))
                        {
                            skippedUserNames.Add(importUsername);
                            continue;
                        }

                        updatedCredentials.Add(exportedCredential);
                        existingUsernames.Add(importUsername);

                        string fileName = Path.GetFileName(exportedFile);
                        string userBaseName = fileName.Replace("_exported.txt", "");

                        string passwordFile = Path.Combine(importDir, $"passwords_{userBaseName}.dat");
                        if (System.IO.File.Exists(passwordFile))
                        {
                            string destPasswordFile = Path.Combine(cyberVaultPath, $"passwords_{userBaseName}.dat");
                            System.IO.File.Copy(passwordFile, destPasswordFile, true);
                        }

                        string settingsFile = Path.Combine(importDir, $"{userBaseName}_settings.ini");
                        if (System.IO.File.Exists(settingsFile))
                        {
                            string destSettingsFile = Path.Combine(cyberVaultPath, $"{userBaseName}_settings.ini");
                            System.IO.File.Copy(settingsFile, destSettingsFile, true);
                        }

                        string authFile = Path.Combine(importDir, $"{userBaseName}.authenticators.enc");
                        if (System.IO.File.Exists(authFile))
                        {
                            string destAuthFile = Path.Combine(cyberVaultPath, $"{userBaseName}.authenticators.enc");
                            System.IO.File.Copy(authFile, destAuthFile, true);
                        }

                        string[] encFiles = System.IO.Directory.GetFiles(importDir, $"{userBaseName}*.enc");
                        foreach (string encFile in encFiles)
                        {
                            string encFileName = Path.GetFileName(encFile);
                            string destEncFile = Path.Combine(cyberVaultPath, encFileName);
                            if (!System.IO.File.Exists(destEncFile))
                            {
                                System.IO.File.Copy(encFile, destEncFile, true);
                            }
                        }

                        importedUsers++;
                        importedUserNames.Add(importUsername);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error importing file {exportedFile}: {ex.Message}");
                    }
                }

                if (importedUsers > 0)
                {
                    System.IO.File.WriteAllLines(credentialsPath, updatedCredentials);
                }

                if (importedUsers > 0)
                {
                    string message = $"Successfully imported {importedUsers} user(s):\n• {string.Join("\n• ", importedUserNames)}";

                    if (skippedUserNames.Count > 0)
                    {
                        message += $"\n\nSkipped {skippedUserNames.Count} existing user(s):\n• {string.Join("\n• ", skippedUserNames)}";
                    }

                    System.Windows.MessageBox.Show(message, "Import Complete",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else if (skippedUserNames.Count > 0)
                {
                    System.Windows.MessageBox.Show(
                        $"All users already exist in the system. Skipped {skippedUserNames.Count} user(s):\n• {string.Join("\n• ", skippedUserNames)}",
                        "Import Information", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("No valid user data was found to import.",
                        "Import Information", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing user data: {ex.Message}",
                    "Import Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ExportPasswords_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<PasswordItem> passwords = PasswordStorage.LoadPasswords(username!, encryptionKey!);

                if (passwords == null || passwords.Count == 0)
                {
                    System.Windows.MessageBox.Show("There are no passwords to export.",
                        "Export Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save Passwords as JSON",
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = ".json",
                    FileName = $"CyberVault_Passwords_{username}_{DateTime.Now:yyyyMMdd}",
                    AddExtension = true
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    return;
                }

                string jsonFilePath = saveFileDialog.FileName;

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true, 
                };

                string jsonContent = System.Text.Json.JsonSerializer.Serialize(passwords, options);

                System.IO.File.WriteAllText(jsonFilePath, jsonContent);

                System.Windows.MessageBox.Show($"Successfully exported {passwords.Count} passwords to:\n{jsonFilePath}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error exporting passwords: {ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportPasswords_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select JSON file with passwords/JSON is the only supported file currently for importing passwords.",
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() != true)
                {
                    return;
                }

                string jsonFilePath = openFileDialog.FileName;

                if (!System.IO.File.Exists(jsonFilePath))
                {
                    System.Windows.MessageBox.Show("Selected file does not exist.",
                        "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string jsonContent = System.IO.File.ReadAllText(jsonFilePath);

                List<PasswordItem> importedPasswords = new List<PasswordItem>();
                int parsedCount = 0;

                try
                {
                    using (var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonContent))
                    {
                        var root = jsonDoc.RootElement;

                        if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var element in root.EnumerateArray())
                            {
                                var passwordItem = ParsePasswordItemFromJsonElement(element);
                                if (passwordItem != null)
                                {
                                    importedPasswords.Add(passwordItem);
                                    parsedCount++;
                                }
                            }
                        }
                        else if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            foreach (var property in root.EnumerateObject())
                            {
                                if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var element in property.Value.EnumerateArray())
                                    {
                                        var passwordItem = ParsePasswordItemFromJsonElement(element);
                                        if (passwordItem != null)
                                        {
                                            importedPasswords.Add(passwordItem);
                                            parsedCount++;
                                        }
                                    }
                                }
                                else if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    var passwordItem = ParsePasswordItemFromJsonElement(property.Value);
                                    if (passwordItem != null)
                                    {
                                        importedPasswords.Add(passwordItem);
                                        parsedCount++;
                                    }
                                }
                            }

                            if (parsedCount == 0)
                            {
                                var passwordItem = ParsePasswordItemFromJsonElement(root);
                                if (passwordItem != null)
                                {
                                    importedPasswords.Add(passwordItem);
                                    parsedCount++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error parsing JSON: {ex.Message}",
                        "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (importedPasswords.Count == 0)
                {
                    System.Windows.MessageBox.Show("No valid password entries found in the file.",
                        "Import Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                List<PasswordItem> existingPasswords = PasswordStorage.LoadPasswords(username!, encryptionKey!);

                int added = 0;
                int skipped = 0;
                int updated = 0;

                Dictionary<string, PasswordItem> existingPasswordDict = new Dictionary<string, PasswordItem>(
                    StringComparer.OrdinalIgnoreCase);

                foreach (var existing in existingPasswords)
                {
                    if (!string.IsNullOrWhiteSpace(existing.Name))
                    {
                        existingPasswordDict[existing.Name] = existing;
                    }
                }

                foreach (var importedPassword in importedPasswords)
                {
                    if (string.IsNullOrWhiteSpace(importedPassword.Name))
                    {
                        skipped++;
                        continue;
                    }

                    if (existingPasswordDict.TryGetValue(importedPassword.Name, out var existingPassword))
                    {
                        bool anyChange = false;

                        if (!string.IsNullOrEmpty(importedPassword.Website))
                        {
                            existingPassword.Website = importedPassword.Website;
                            anyChange = true;
                        }

                        if (!string.IsNullOrEmpty(importedPassword.Username))
                        {
                            existingPassword.Username = importedPassword.Username;
                            anyChange = true;
                        }

                        if (!string.IsNullOrEmpty(importedPassword.Email))
                        {
                            existingPassword.Email = importedPassword.Email;
                            anyChange = true;
                        }

                        if (!string.IsNullOrEmpty(importedPassword.Password))
                        {
                            existingPassword.Password = importedPassword.Password;
                            anyChange = true;
                        }

                        if (anyChange)
                        {
                            updated++;
                        }
                    }
                    else
                    {
                        existingPasswords.Add(importedPassword);
                        existingPasswordDict[importedPassword.Name] = importedPassword;
                        added++;
                    }
                }

                if (added > 0 || updated > 0)
                {
                    PasswordStorage.SavePasswords(existingPasswords, username!, encryptionKey!);

                    string message = $"Import completed successfully!\n\n" +
                                    $"Added: {added} new passwords\n" +
                                    $"Updated: {updated} existing passwords\n" +
                                    $"Skipped: {skipped} invalid entries\n" +
                                    $"Total parsed: {parsedCount}";

                    System.Windows.MessageBox.Show(message, "Import Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show($"No passwords were imported. Skipped {skipped} invalid entries.",
                        "Import Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing passwords: {ex.Message}",
                    "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private PasswordItem ParsePasswordItemFromJsonElement(System.Text.Json.JsonElement element)
        {
            if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return null!;
            }

            string[] nameVariations = { "name", "title", "site_name", "application", "app", "account", "service" };
            string[] websiteVariations = { "website", "url", "uri", "web", "site", "link" };
            string[] usernameVariations = { "username", "user", "user_name", "login", "login_name", "account_name" };
            string[] emailVariations = { "email", "e-mail", "email_address", "mail" };
            string[] passwordVariations = { "password", "pass", "pwd", "secret", "passphrase" };

            var passwordItem = new PasswordItem();
            bool foundAnyProperty = false;

            void TrySetProperty(string[] variations, Action<string> setter)
            {
                foreach (var variation in variations)
                {
                    if (element.TryGetProperty(variation, out var jsonValue) &&
                        jsonValue.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        string? value = jsonValue.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            setter(value);
                            foundAnyProperty = true;
                            break;
                        }
                    }
                }
            }

            TrySetProperty(nameVariations, value => passwordItem.Name = value);
            TrySetProperty(websiteVariations, value => passwordItem.Website = value);
            TrySetProperty(usernameVariations, value => passwordItem.Username = value);
            TrySetProperty(emailVariations, value => passwordItem.Email = value);
            TrySetProperty(passwordVariations, value => passwordItem.Password = value);

            if (string.IsNullOrWhiteSpace(passwordItem.Name) && !string.IsNullOrWhiteSpace(passwordItem.Website))
            {
                try
                {
                    var uri = new Uri(passwordItem.Website);
                    passwordItem.Name = uri.Host.Replace("www.", "");
                    foundAnyProperty = true;
                }
                catch
                {
                    passwordItem.Name = passwordItem.Website;
                }
            }

            if (element.TryGetProperty("origin_url", out var originUrl) &&
                element.TryGetProperty("username_value", out var usernameValue) &&
                element.TryGetProperty("password_value", out var passwordValue))
            {
                passwordItem.Website = originUrl.GetString();
                passwordItem.Username = usernameValue.GetString();
                passwordItem.Password = passwordValue.GetString();

                if (string.IsNullOrWhiteSpace(passwordItem.Name) && !string.IsNullOrWhiteSpace(passwordItem.Website))
                {
                    try
                    {
                        var uri = new Uri(passwordItem.Website);
                        passwordItem.Name = uri.Host.Replace("www.", "");
                    }
                    catch
                    {
                        passwordItem.Name = "Imported from Chrome";
                    }
                }

                foundAnyProperty = true;
            }

            if (element.TryGetProperty("login", out var loginObj) && loginObj.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (loginObj.TryGetProperty("username", out var username))
                    passwordItem.Username = username.GetString();

                if (loginObj.TryGetProperty("password", out var password))
                    passwordItem.Password = password.GetString();

                if (loginObj.TryGetProperty("uris", out var uris) && uris.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var uri in uris.EnumerateArray())
                    {
                        if (uri.TryGetProperty("uri", out var uriValue))
                        {
                            passwordItem.Website = uriValue.GetString();
                            break;
                        }
                    }
                }

                foundAnyProperty = true;
            }

            return foundAnyProperty ? passwordItem : null!;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not open link: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event EventHandler ?KontaktOssRequested;

        // Når du skal trigge eventet:
        private void KontaktOss_Click(object sender, RoutedEventArgs e)
        {
            KontaktOssRequested?.Invoke(this, EventArgs.Empty);
        }

    }
}