using CyberVault.WebExtension;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
namespace CyberVault;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static bool MinimizeToTrayEnabled { get; set; } = false;
    public static string CurrentUsername { get; set; } = string.Empty;

    public static string ?CurrentAccessToken { get; set; }
    public static LocalWebServer ?WebServer { get; set; }

    public static void LoadUserSettings(string username)
    {
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
                    if (line.StartsWith("MinimizeToTray="))
                    {
                        MinimizeToTrayEnabled = bool.Parse(line.Substring("MinimizeToTray=".Length));
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user settings: {ex.Message}");
        }
    }
}