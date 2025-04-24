using IWshRuntimeLibrary;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;


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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
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
                Console.WriteLine($"Error saving user setting: {ex.Message}");
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
                string startupFolderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup));

                string shortcutPath = Path.Combine(startupFolderPath, "CyberVault.lnk");

                if (!System.IO.File.Exists(shortcutPath))
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;

                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                    shortcut.Description = "CyberVault";
                    shortcut.Save();
                }

                SaveUserSetting("StartWithWindows", "True");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up startup: {ex.Message}`");
                System.Windows.MessageBox.Show($"Failed to set up startup: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

                StartWithWindowsToggle.Checked -= StartWithWindowsToggle_Checked;
                StartWithWindowsToggle.IsChecked = false;
                StartWithWindowsToggle.Checked += StartWithWindowsToggle_Checked;
            }
        }

        private void StartWithWindowsToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

                string shortcutPath = Path.Combine(startupFolderPath, "CyberVault.lnk");

                if (System.IO.File.Exists(shortcutPath))
                {
                    System.IO.File.Delete(shortcutPath);
                }

                SaveUserSetting("StartWithWindows", "False");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing startup: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to remove startup: {ex.Message}",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

                StartWithWindowsToggle.Unchecked -= StartWithWindowsToggle_Unchecked;
                StartWithWindowsToggle.IsChecked = true;
                StartWithWindowsToggle.Unchecked += StartWithWindowsToggle_Unchecked;
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

        private void BiometricToggle_Checked(object sender, RoutedEventArgs e)
        {
            SaveUserSetting("BiometricEnabled", "True");
        }

        private void BiometricToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveUserSetting("BiometricEnabled", "False");
        }

        private void ChangeMasterPassword_Click(object sender, RoutedEventArgs e)
        {

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
                        Console.WriteLine($"Error importing file {exportedFile}: {ex.Message}");
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

    }
}