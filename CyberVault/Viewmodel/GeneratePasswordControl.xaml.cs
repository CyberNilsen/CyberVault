using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace CyberVault.Viewmodel
{

    public partial class GeneratePasswordControl : UserControl
    {
        private string _username;
        private byte[] _encryptionKey;
        public GeneratePasswordControl(string username, byte[] encryptionKey)
        {
            InitializeComponent();
            _username = username;
            _encryptionKey = encryptionKey;
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SavePassword_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
