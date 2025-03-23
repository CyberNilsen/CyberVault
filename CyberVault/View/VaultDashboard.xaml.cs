using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace CyberVault.View
{
    public partial class VaultDashboard : Window
    {
        public string _username;
        private byte[] _encryptionKey;

        public VaultDashboard(string username)
        {
            InitializeComponent();

            _username = username;
            // Try to get encryption key from Login
            _encryptionKey = Login.GetEncryptionKey();

            Title = $"CyberVault - {_username}";

            Username.Text = $"Welcome, {_username}!";

            SetRandomQuote();
        }

        public void SetEncryptionKey(byte[] key)
        {
            _encryptionKey = key;
        }

        public byte[] GetEncryptionKey()
        {
            return _encryptionKey;
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
            QuoteTextBlock.Text = quotes[index];
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void PasswordVault_Click(object sender, RoutedEventArgs e)
        {
            PasswordVault passwordVaultWindow = new PasswordVault(_username);
            passwordVaultWindow.SetEncryptionKey(_encryptionKey);
            passwordVaultWindow.Show();
            this.Close();
        }

        private void TwoFactorAuth_Click(object sender, RoutedEventArgs e)
        {
            TwoFactorAuthenticator twoFactorAuthWindow = new TwoFactorAuthenticator(_username);
            twoFactorAuthWindow.SetEncryptionKey(_encryptionKey);
            twoFactorAuthWindow.Show();
            this.Close();
        }

        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            // Clean up the encryption key when logging out
            typeof(Login).GetProperty("CurrentEncryptionKey",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static)?.SetValue(null, null);

            _username = string.Empty;
            Login login = new Login();
            login.Show();
            this.Close();
        }
    }
}