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


namespace CyberVault.Viewmodel
{
    public partial class Settings : System.Windows.Controls.UserControl
    {
        private string? username;
        private byte[]? encryptionKey;

        public Settings(string user, byte[] key)
        {
            InitializeComponent();
            username = user;
            encryptionKey = key;
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

        private void SaveUserSetting(string settingName, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                    return;

                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string settingsFilePath = Path.Combine(cyberVaultPath, $"{username}_settings.ini");

                Dictionary<string, string> settings = new Dictionary<string, string>();

                if (System.IO.File.Exists(settingsFilePath))
                {
                    foreach (string line in System.IO.File.ReadAllLines(settingsFilePath))
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            settings[parts[0]] = parts[1];
                        }
                    }
                }

                settings[settingName] = value;

                Directory.CreateDirectory(cyberVaultPath);
                System.IO.File.WriteAllLines(settingsFilePath,
                    settings.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving user setting: {ex.Message}");
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