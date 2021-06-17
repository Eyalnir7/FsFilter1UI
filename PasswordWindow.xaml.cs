using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FsFilter1UI
{
    /// <summary>
    /// Interaction logic for PasswordWindow.xaml
    /// </summary>
    public partial class PasswordWindow : Window
    {
        public int saltSize = 8;
        public bool match = false;
        public string hash;
        private string regpassword;
        public PasswordWindow()
        {
            InitializeComponent();
        }

        private void Continue_Button_Click(object sender, RoutedEventArgs e)
        {
            regpassword = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "hash", null);
            if(regpassword == null)
            {
                MessageBox.Show("you need to register first");
                return;
            }
            string hash = MyEncryption.HashPasswordWithSalt(PasswordBox.Text, regpassword.Substring(0, saltSize*2));
            if (hash == regpassword)
            {
                this.hash = hash;
                match = true;
                this.Close();
                return;
            }
            MessageBox.Show("incorrect password try again");
        }


        private void Register_Button_Click(object sender, RoutedEventArgs e)
        {
            regpassword = (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "hash", null);
            // hard coded salt bytes size 8
            if (regpassword == null)
            {
                RegistrationWindow regwin = new RegistrationWindow(saltSize);
                regwin.ShowDialog();
                return;
            }
            MessageBox.Show("already registered");
        }
    }
}
