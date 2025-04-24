using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using CyberVault;
using CyberVault.WebExtension;

namespace CyberVault
{
    public partial class LoginControl : System.Windows.Controls.UserControl
    {
        private MainWindow mainWindow;
        public static byte[] ?CurrentEncryptionKey { get; private set; }

        public LoginControl(MainWindow mw)
        {
            InitializeComponent();
            mainWindow = mw;
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    mainWindow.ToggleWindowState();
                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    Window.GetWindow(this).DragMove();
                }
            }
        }


        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameInput.Text;
            string password = PasswordInput.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                System.Windows.MessageBox.Show("Username and password cannot be empty!", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string credentialsFilePath = Path.Combine(cyberVaultPath, "credentials.txt");

                if (!File.Exists(credentialsFilePath))
                {
                    System.Windows.MessageBox.Show("No accounts found. Please register first.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                bool userFound = false;
                byte[] salt = null!;
                byte[] storedHash = null!;

                foreach (string line in File.ReadAllLines(credentialsFilePath))
                {
                    string[] parts = line.Split(',');
                    if (parts.Length == 3 && parts[0] == username)
                    {
                        userFound = true;
                        salt = Convert.FromBase64String(parts[1]);
                        storedHash = Convert.FromBase64String(parts[2]);
                        break;
                    }
                }

                if (!userFound)
                {
                    System.Windows.MessageBox.Show("Account not found!", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] inputHash = HashPassword(password, salt!);
                if (!CompareByteArrays(inputHash, storedHash!))
                {
                    System.Windows.MessageBox.Show("Incorrect password or username!", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                CurrentEncryptionKey = KeyDerivation.DeriveKey(password, salt!);

                App.CurrentUsername = username; 
                App.LoadUserSettings(username); 

                mainWindow.Navigate(new DashboardControl(mainWindow, username, CurrentEncryptionKey));
                int lockTimeMinutes = GetAutoLockTimeFromSettings(username);
                mainWindow.UserLoggedIn(username, lockTimeMinutes);
                if (App.WebServer == null)
                {
                    App.WebServer = new LocalWebServer(username, CurrentEncryptionKey);
                    App.WebServer.Start();
                    App.CurrentAccessToken = App.WebServer.GetAccessToken();
                }

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetAutoLockTimeFromSettings(string username)
        {
            int minutes = 5;

            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cyberVaultPath = Path.Combine(appDataPath, "CyberVault");
                string settingsFilePath = Path.Combine(cyberVaultPath, $"{username}_settings.ini");

                if (File.Exists(settingsFilePath))
                {
                    string[] lines = File.ReadAllLines(settingsFilePath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2 && parts[0] == "AutoLockTime")
                        {
                            string value = parts[1];
                            if (value == "Never")
                                return 0;
                            else if (value == "1 Minute")
                                return 1;
                            else if (value == "5 Minutes")
                                return 5;
                            else if (value == "15 Minutes")
                                return 15;
                            else if (value == "30 Minutes")
                                return 30;
                            else if (value == "1 Hour")
                                return 60;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting auto-lock time: {ex.Message}");
            }

            return minutes;
        }

        private void ImportTextBlock_MouseLeftButtonDown(object sender, RoutedEventArgs e)
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


        private byte[] HashPassword(string password, byte[] salt, int iterations = 10000, int hashSize = 32)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(hashSize);
            }
        }

        private bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                    return false;
            }

            return true;
        }

        private void RegisterTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mainWindow.Navigate(new RegisterControl(mainWindow));
        }

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordInput.Visibility == Visibility.Visible)
            {
                PasswordInput.Visibility = Visibility.Collapsed;
                PlainTextPassword.Text = PasswordInput.Password;
                PlainTextPassword.Visibility = Visibility.Visible;
            }
            else
            {
                PasswordInput.Password = PlainTextPassword.Text;
                PlainTextPassword.Visibility = Visibility.Collapsed;
                PasswordInput.Visibility = Visibility.Visible;
            }
        }
    }
}