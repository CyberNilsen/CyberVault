using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CyberVault.WebExtension;

namespace CyberVault.Viewmodel
{
    public partial class HomeDashboardControl : UserControl
    {
        private string _accessToken;
        private LocalWebServer _webServer;

        public HomeDashboardControl(string username, byte[] encryptionKey)
        {
            InitializeComponent();
            InitializeWebServer(username, encryptionKey);
            LoadExtensionKey();
            SetRandomQuote();
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
                }
                else
                {
                    ExtensionKeyText.Text = "Server not started";
                }
            }
            catch (Exception ex)
            {
                ExtensionKeyText.Text = "Error loading key";
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







    }
}