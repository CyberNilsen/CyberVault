using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;


namespace CyberVault.View
{
    public partial class VaultDashboard : Window
    {
        public string _username;

        public VaultDashboard(string username)
        {
            InitializeComponent();

            _username = username;
            Title = $"CyberVault - {_username}";

            Username.Text = $"Welcome, {_username}!";

            SetRandomQuote(); 
        }

        private void SetRandomQuote()
        {
            List<string> quotes = new List<string>
            {
                "Strong passwords and strong coffee: two things you should never compromise.",
                "Your firewall is only as strong as your coffee is black.",
                "Encrypted data, decrypted brain: coffee does both.",
                "Cybersecurity rule #1: Never trust, always verify. Cybersecurity rule #2: Always caffeinate.",
                "Brute-force attacks won’t work on me, but a strong espresso might.",
                "Just like two-factor authentication, my coffee has two layers: caffeine and more caffeine.",
                "Coffee is like cybersecurity—stay patched and stay awake.",
                "No phishing attempts can fool me before my first coffee.",
                "Without coffee, even the most secure system has vulnerabilities: ME.",
                "A weak password is like decaf coffee—completely useless.",
                "Security updates keep your system safe. Coffee updates keep your brain safe.",
                "Ransomware can’t hold my coffee hostage.",
                "Cyber threats don’t sleep, but neither do I—thanks to coffee.",
                "Would you trust an unencrypted network? No? Then don’t trust a coffee without caffeine.",
                "Hackers exploit human errors. I exploit coffee for survival.",
                "I don’t need caffeine injections… yet. Just coffee and a good firewall.",
                "Every keystroke is a bit more secure with a sip of coffee.",
                "Digital forensics is easier with a forensic amount of coffee.",
                "Remember: The weakest link in security is the user. The weakest link in my day is running out of coffee.",
                "Life without coffee is like a computer without an internet connection.",
                "But first, coffee. Then, world domination.",
                "Coffee doesn’t ask questions; coffee understands.",
                "If coffee can’t fix it, it’s a serious problem.",
                "A yawn is just a silent scream for coffee.",
                "Happiness is a freshly brewed cup of coffee.",
                "Coffee first, decisions later.",
                "Procaffeinating: The tendency to delay everything until you’ve had coffee.",
                "Keep your friends close and your coffee closer.",
                "Caffeine: the only reason I function before noon.",
                "Today’s forecast: 100% chance of coffee.",
                "Behind every great developer is a lot of empty coffee cups.",
                "Sleep is optional. Coffee is mandatory.",
                "You had me at ‘coffee.’",
                "More espresso, less depresso.",
                "Life begins after coffee.",
                "Espresso yourself!",
                "Give me coffee to change the things I can, and wine to accept the things I can’t.",
                "Good ideas start with coffee.",
                "May your coffee be strong and your passwords stronger."
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
            passwordVaultWindow.Show();
            this.Close(); 
        }

        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            _username = string.Empty;
            Login Login = new Login();
            Login.Show();
            this.Close();
        }
    }
}
