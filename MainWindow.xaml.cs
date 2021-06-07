using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;
using Microsoft.Win32;
using System.Linq;
using System.Windows.Input;

namespace FsFilter1UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll")]
        static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);
        private static string hash;
        public MainWindow()
        {
            InitializeComponent();
            FolderListBox.SelectionChanged += FolderListBox_SelectionChanged;
            PasswordWindow passwordWindow = new PasswordWindow();
            passwordWindow.ShowDialog();
            if (passwordWindow.match == false) this.Close();
            MainWindow.hash = passwordWindow.hash;
            string[] blockedFolders = GetBlockedFolders();
            if (blockedFolders != null)
            {
                foreach (string folder in blockedFolders)
                {
                    FolderListBox.Items.Add(folder);
                }
            }
            //stop the driver
            ProcessStartInfo ps = new ProcessStartInfo("cmd");
            ps.RedirectStandardInput = true;
            ps.Verb = "runas";
            ps.UseShellExecute = false;
            ps.CreateNoWindow = true;

            // Starts the process
            using (Process p = Process.Start(ps))
            {
                p.StandardInput.WriteLine("sc stop fsfilter1");

                // Reads the output to a string
                // Waits for the process to exit must come *after* StandardOutput is "empty"
                // so that we don't deadlock because the intermediate kernel pipe is full.
                p.StandardInput.WriteLine("Exit");
                p.WaitForExit();
            }
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            //start the driver
            ProcessStartInfo ps = new ProcessStartInfo("cmd");
            ps.RedirectStandardInput = true;
            ps.Verb = "runas";
            ps.UseShellExecute = false;
            ps.CreateNoWindow = true;

            //start the driver
            using (Process p = Process.Start(ps))
            {
                p.StandardInput.WriteLine("sc start fsfilter1");

                // Reads the output to a string
                // Waits for the process to exit must come *after* StandardOutput is "empty"
                // so that we don't deadlock because the intermediate kernel pipe is full.
                p.StandardInput.WriteLine("Exit");
                p.WaitForExit();
            }
            Mouse.OverrideCursor = null;
        }

        private string[] GetBlockedFolders()
        {
            string[] regFolders = (string[])Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\Fsfilter1", "BlockFolder", null);
            if (regFolders == null) 
            {
                return null;
            }
            string[] blockedFolders = new string[regFolders.Length];
            for (int i = 0; i < regFolders.Length; i++)
            {
                blockedFolders[i] = DevicePathMapper.FromDevicePath(regFolders[i]);
            }
            return blockedFolders;
        }

        //returns dictionary with the following keys: folders to encrypt, folders to dectrypt, folders to encrypt without their children folders, folders to decrypt without their children folders
        //the values are the paths of the folders
        //private Dictionary<string, string[]> GetEncryptDecryptFolders()
        //{
        //    Dictionary<string, List<string>> EncryptDecrypt = new Dictionary<string, List<string>>();
        //    foreach(var item in FolderListBox.Items)
        //    {
        //        for (int i = 0; i < blockedFolders.Length; i++)
        //        {
        //            if (item.ToString() != blockedFolders[i])
        //            {
        //                //partencrypt folders
        //                if (blockedFolders[i].StartsWith(item.ToString()))
        //                {
        //                    string[] strings = { blockedFolders[i], item.ToString() };
        //                    List<string> encryptPart = GetCommonFolders(strings);
        //                    EncryptDecrypt.Add("PartDecrypt", new List<string>());
        //                    foreach (string folder in encryptPart)
        //                    {
        //                        EncryptDecrypt["PartEncrypt"].Add(folder);
        //                    }
        //                }
        //                //partdecrypt folders
        //                else if (item.ToString().StartsWith(blockedFolders[i]))
        //                {
        //                    string[] strings = { blockedFolders[i], item.ToString() };
        //                    List<string> encryptPart = GetCommonFolders(strings);
        //                    EncryptDecrypt.Add("PartDecrypt", new List<string>());
        //                    foreach (string folder in encryptPart)
        //                    {
        //                        EncryptDecrypt["PartDecrypt"].Add(folder);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //private List<string> GetCommonFolders(string[] strings)
        //{
        //    var commonPrefix = new string(strings.First().Substring(0, strings.Min(s => s.Length)).TakeWhile((c, i) => strings.All(s => s[i] == c)).ToArray());
        //    List<string> encryptPart = new List<string>();
        //    for (int j = commonPrefix.IndexOf('\\'); j > -1; j = commonPrefix.IndexOf('\\', j + 1))
        //    {
        //        // for loop end when i=-1 ('\' not found)
        //        encryptPart.Add(commonPrefix.Substring(0, j + 1));
        //    }
        //    return encryptPart;
        //}

        private void FolderListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FolderListBox.SelectedItem != null) PathTextBox.Text = FolderListBox.SelectedItem.ToString();
            else if (FolderListBox.Items.Count != 0) PathTextBox.Text = FolderListBox.Items[FolderListBox.Items.Count - 1].ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                if (!ExistInList(dialog.SelectedPath))
                {
                    PathTextBox.Text = dialog.SelectedPath;
                    FolderListBox.Items.Add(dialog.SelectedPath);
                    //encrypt all the none encrypted files
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    MyEncryption.EncryptFolder(dialog.SelectedPath, hash, 32, 8);
                    Mouse.OverrideCursor = null;
                }
            }
        }

        //ExistInList returns true if the new path is already covered in the list
        //Also it removes any folder that the new path already covers
        private bool ExistInList(string path)
        {
            List<object> toRemove = new List<object>();
            foreach(var item in FolderListBox.Items)
            {
                if (path.StartsWith(item.ToString())) 
                {
                    MessageBox.Show("You already blocked this folder");
                    return true;
                }
                if (item.ToString().StartsWith(path))
                {
                    toRemove.Add(item);
                }
            }
            foreach(var item in toRemove)
            {
                FolderListBox.Items.Remove(item);
            }
            return false;
        }
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FolderListBox.SelectedItem != null)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                MyEncryption.DecryptFolder(FolderListBox.SelectedItem.ToString(), hash, 32, 8);
                Mouse.OverrideCursor = null;
                FolderListBox.Items.Remove(FolderListBox.SelectedItem);
            }
            //else if (FolderListBox.Items.Count != 0) FolderListBox.Items.Remove(FolderListBox.Items[FolderListBox.Items.Count - 1]);
            //if (FolderListBox.Items.Count == 0) PathTextBox.Text = "";
        }

        public string[] GetListBoxStrings()
        {
            string[] paths = new string[FolderListBox.Items.Count];
            for (int i = 0; i < FolderListBox.Items.Count; i++)
            {
                paths[i] = FolderListBox.Items[i].ToString();
            }
            return paths;
        }
        private static string GetRealPath(string path)
        {
            string realPath = path;
            StringBuilder pathInformation = new StringBuilder(250);
            string driveLetter = System.IO.Path.GetPathRoot(realPath).Replace("\\", "");
            QueryDosDevice(driveLetter, pathInformation, 250);

            // If drive is substed, the result will be in the format of "\??\C:\RealPath\".

            // Strip the \??\ prefix.
            string realRoot = pathInformation.ToString();

            //Combine the paths.
            realPath = System.IO.Path.Combine(realRoot, realPath.Replace(System.IO.Path.GetPathRoot(realPath), ""));

            return realPath;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            //get the real paths of the items (with DEVICE\\HARDDISKVOLUME1\\...)
            // and encrypt the folders
            string[] realPaths = new string[FolderListBox.Items.Count];
            for (int i = 0; i < realPaths.Length; i++)
            {
                realPaths[i] = GetRealPath((string)FolderListBox.Items.GetItemAt(i)).ToUpper();
            }
            // set the registry with the new folders (thats where the driver read the folders to block)
            Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "BlockFolder", realPaths, RegistryValueKind.MultiString);
            Mouse.OverrideCursor = null;
        }
    }
}
