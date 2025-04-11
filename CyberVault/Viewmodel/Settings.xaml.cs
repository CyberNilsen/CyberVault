using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using IWshRuntimeLibrary;
using System.Windows.Controls.Primitives;


namespace CyberVault.Viewmodel
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class Settings : System.Windows.Controls.UserControl
    {
        private string username;
        private byte[] encryptionKey;

        public Settings()
        {
            InitializeComponent();
        }

        public void Initialize(string user, byte[] key)
        {
            username = user;
            encryptionKey = key;
        }

        private void TwoFactorToggle_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void TwoFactorToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Implementation for disabling two-factor authentication
        }

        private void StartWithWindowsToggle_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the startup folder path
                string startupFolderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup));

                // Define the shortcut path in the startup folder
                string shortcutPath = Path.Combine(startupFolderPath, "CyberVault.lnk");

                // If the shortcut doesn't exist, create it
                if (!System.IO.File.Exists(shortcutPath))
                {
                    // Get the path to the executable
                    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                    // Create a shortcut
                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                    shortcut.Description = "CyberVault Password Manager";
                    shortcut.Save();
                }

                // Save the setting to the user's settings file
                SaveUserSetting("StartWithWindows", "true");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up startup: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to set up startup: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

                // Uncheck the toggle without triggering the event
                StartWithWindowsToggle.Checked -= StartWithWindowsToggle_Checked;
                StartWithWindowsToggle.IsChecked = false;
                StartWithWindowsToggle.Checked += StartWithWindowsToggle_Checked;
            }
        }

        private void StartWithWindowsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the startup folder path
                string startupFolderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup));

                // Define the shortcut path in the startup folder
                string shortcutPath = Path.Combine(startupFolderPath, "CyberVault.lnk");

                // If the shortcut exists, delete it
                if (System.IO.File.Exists(shortcutPath))
                {
                    System.IO.File.Delete(shortcutPath);
                }

                // Save the setting to the user's settings file
                SaveUserSetting("StartWithWindows", "false");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing startup: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to remove startup: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

                // Check the toggle without triggering the event
                StartWithWindowsToggle.Unchecked -= StartWithWindowsToggle_Unchecked;
                StartWithWindowsToggle.IsChecked = true;
                StartWithWindowsToggle.Unchecked += StartWithWindowsToggle_Unchecked;
            }
        }

        private void PrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for opening privacy policy
        }

        private void MinimizeToTrayToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Update the application-wide setting
            App.MinimizeToTrayEnabled = true;
            SaveUserSetting("MinimizeToTray", "true");
        }

        private void MinimizeToTrayToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Update the application-wide setting
            App.MinimizeToTrayEnabled = false;
            SaveUserSetting("MinimizeToTray", "false");
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(App.CurrentUsername))
            {
                LoadSettingsFromFile();
            }
            else
            {
                ResetSettingsUI();
            }
        }

        private void ResetSettingsUI()
        {
            // Reset all toggle controls to default state
            TwoFactorToggle.IsChecked = false;
            MinimizeToTrayToggle.IsChecked = false;
            DarkModeToggle.IsChecked = false;
            CloudSyncToggle.IsChecked = false;
            BiometricToggle.IsChecked = false;

            // Reset any combo boxes if needed
            if (BackupFrequencyComboBox != null)
                BackupFrequencyComboBox.SelectedIndex = 0;

            if (AutoLockComboBox != null)
                AutoLockComboBox.SelectedIndex = 0;
        }

        private void LoadSettingsFromFile()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string settingsFilePath = Path.Combine(cyberVaultPath, $"{App.CurrentUsername}_settings.ini");

                if (System.IO.File.Exists(settingsFilePath))
                {
                    Dictionary<string, bool> settings = new Dictionary<string, bool>();
                    string[] lines = System.IO.File.ReadAllLines(settingsFilePath);

                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            settings[parts[0]] = bool.Parse(parts[1]);
                        }
                    }

                    // Update UI without triggering events
                    UpdateToggleWithoutEvent(MinimizeToTrayToggle, settings.GetValueOrDefault("MinimizeToTray", false));
                    UpdateToggleWithoutEvent(StartWithWindowsToggle, settings.GetValueOrDefault("StartWithWindows", false));
                    UpdateToggleWithoutEvent(TwoFactorToggle, settings.GetValueOrDefault("TwoFactorEnabled", false));
                    UpdateToggleWithoutEvent(DarkModeToggle, settings.GetValueOrDefault("DarkModeEnabled", false));
                    UpdateToggleWithoutEvent(CloudSyncToggle, settings.GetValueOrDefault("CloudSyncEnabled", false));
                    UpdateToggleWithoutEvent(BiometricToggle, settings.GetValueOrDefault("BiometricEnabled", false));

                    // Update app-wide settings
                    App.MinimizeToTrayEnabled = settings.GetValueOrDefault("MinimizeToTray", false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void UpdateToggleWithoutEvent(ToggleButton toggle, bool isChecked)
        {
            if (toggle != null)
            {
                var checkedHandler = toggle.Checked;
                var uncheckedHandler = toggle.Unchecked;

                toggle.Checked -= checkedHandler;
                toggle.Unchecked -= uncheckedHandler;

                toggle.IsChecked = isChecked;

                toggle.Checked += checkedHandler;
                toggle.Unchecked += uncheckedHandler;
            }
        }

        private void SaveUserSetting(string settingName, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(App.CurrentUsername))
                    return;

                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string settingsFilePath = Path.Combine(cyberVaultPath, $"{App.CurrentUsername}_settings.ini");

                Dictionary<string, string> settings = new Dictionary<string, string>();

                // Load existing settings
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

                // Update or add new setting
                settings[settingName] = value;

                // Save all settings
                Directory.CreateDirectory(cyberVaultPath); // Ensure directory exists
                System.IO.File.WriteAllLines(settingsFilePath,
                    settings.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user setting: {ex.Message}");
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
        }

        private void DarkModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Implementation for enabling dark mode
        }

        private void DarkModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Implementation for disabling dark mode
        }

        private void CloudSyncToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Implementation for enabling cloud sync
        }

        private void CloudSyncToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Implementation for disabling cloud sync
        }

        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for checking for updates
        }

        private void ChangeMasterPassword_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for changing master password
        }

        private void BiometricToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Implementation for enabling biometric authentication
        }

        private void BiometricToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Implementation for disabling biometric authentication
        }

        private void BackupLocation_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for selecting backup location
        }

        private void BackupFrequencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Implementation for changing backup frequency
        }

        private void AutoLockComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Implementation for changing auto lock settings
        }
    }
}