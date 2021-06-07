using Microsoft.Win32;
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

namespace FsFilter1UI
{
    /// <summary>
    /// Interaction logic for RegistrationWindow.xaml
    /// </summary>
    public partial class RegistrationWindow : Window
    {
        //how many wide chars before add
        //for example if it is 8 you will get 16 byte long salt when converted to string
        public int saltSize;
        public RegistrationWindow(int saltSize)
        {
            InitializeComponent();
            this.saltSize = saltSize;
            Registry.ClassesRoot.CreateSubKey(@"Directory\shell\FolderBlocker");
            Registry.ClassesRoot.CreateSubKey(@"Directory\shell\FolderBlocker\command");
            Registry.SetValue(@"HKEY_CLASSES_ROOT\Directory\shell\FolderBlocker\command", "(Default)", System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void registerButton_Click(object sender, RoutedEventArgs e)
        {
            if (Password.Text.Length == 0)
            {
                MessageBox.Show("Please enter password");
                return;
            }
            string hash = MyEncryption.HashPasswordWithSalt(Password.Text, this.saltSize);
            Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "hash", hash, RegistryValueKind.String);
            this.Close();
        }
    }
}
