using System.Configuration;
using System.Data;
using System.Windows;
namespace CyberVault;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static bool MinimizeToTrayEnabled { get; set; } = false;
    public static string CurrentUsername { get; set; } = string.Empty;  // Add this line
}