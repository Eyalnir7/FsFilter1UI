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
using System.Collections.ObjectModel;

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
        private ObservableCollection<Folder> folders = new ObservableCollection<Folder>();
        private ObservableCollection<Folder> initialFolders = new ObservableCollection<Folder>();
        public MainWindow()
        {
            InitializeComponent();
            //if opened from context menu navigate to the different app
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                
            }
            //FolderListBox.SelectedIndexChanged += FolderListBox_SelectedIndexChanged; ;
            PasswordWindow passwordWindow = new PasswordWindow();
            passwordWindow.ShowDialog();
            if (passwordWindow.match == false) this.Close();
            MainWindow.hash = passwordWindow.hash;
            string[] folders = (string[])Registry.GetValue("hkey_local_machine\\system\\controlset001\\services\\fsfilter1", "BlockFolder", null);
            if(folders == null)
            {
                Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "BlockFolder", new string[] { "" }, RegistryValueKind.MultiString);
            }

            PopulateFolderTable();
            //folders.CollectionChanged += Folders_CollectionChanged;

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

        //private void Folders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        //{
        //    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
        //    {
        //        Folder oldItem = (Folder)e.OldItems[0];
        //        if ( oldItem.encrypt!= ((Folder)e.NewItems[0]).encrypt)
        //        {
        //            if (oldItem.encrypt) MyEncryption.DecryptFolder(oldItem.path, hash, 32, 8);
        //            else MyEncryption.EncryptFolder(oldItem.path, hash, 32, 8);
        //        }
        //    }
        //}

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

        //private string[] GetBlockedFolders()
        //{
        //    string[] regFolders = (string[])Registry.GetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\Fsfilter1", "BlockFolder", null);
        //    if (regFolders == null) 
        //    {
        //        return null;
        //    }
        //    string[] blockedFolders = new string[regFolders.Length];
        //    for (int i = 0; i < regFolders.Length; i++)
        //    {
        //        blockedFolders[i] = DevicePathMapper.FromDevicePath(regFolders[i]);
        //    }
        //    return blockedFolders;
        //}

        private void PopulateFolderTable()
        {
            string[] folderObjects = (string[])Registry.GetValue("hkey_local_machine\\system\\controlset001\\services\\fsfilter1", "FolderObjects", null);
            if (folderObjects == null)
            {
                return;
            }
            for (int i = 0; i < folderObjects.Length; i++)
            {
                string[] objString = folderObjects[i].Split(" ");
                Folder folder = new Folder(objString[0], bool.Parse(objString[1]), bool.Parse(objString[2]), bool.Parse(objString[1]));
                folders.Add(folder);
                initialFolders.Add(folder);
            }
            FolderDataGrid.ItemsSource = folders;
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

        //private void FolderListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (FolderListBox.SelectedItem != null) PathTextBox.Text = FolderListBox.SelectedItem.ToString();
        //    else if (FolderListBox.Items.Count != 0) PathTextBox.Text = FolderListBox.Items[FolderListBox.Items.Count - 1].ToString();
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                if (!ExistInList(dialog.SelectedPath))
                {
                    //  PathTextBox.Text = dialog.SelectedPath;
                    //FolderListBox.Items.Add(dialog.SelectedPath);
                    Folder folder = new Folder(dialog.SelectedPath, false, false, false);
                    folders.Add(folder);
                    FolderDataGrid.ItemsSource = folders;
                    //encrypt all the none encrypted files
                    //if (folder.encrypt == true) 
                    //{
                    //    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    //    MyEncryption.EncryptFolder(dialog.SelectedPath, hash, 32, 8);
                    //    Mouse.OverrideCursor = null;
                    //}
                }
            }
        }

        private void RemoveFolderFromRegistry(Folder folder)
        {
            string[] oldFolderObjects = (string[])Registry.GetValue("hkey_local_machine\\system\\controlset001\\services\\fsfilter1", "FolderObjects", null);
            List<string> folderObjects = new List<string>();
            if (oldFolderObjects == null)
            {
                return;
            }
            for (int i = 0; i < oldFolderObjects.Length; i++)
            {
                if (!oldFolderObjects[i].StartsWith(folder.path)) folderObjects.Add(oldFolderObjects[i]);
            }
            string[] folderObjectsarr = new string[folderObjects.Count];
            for (int i = 0; i < folderObjectsarr.Length; i++)
            {
                folderObjectsarr[i] = folderObjects[i];
            }
            Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "FolderObjects", folderObjectsarr, RegistryValueKind.MultiString);
        }

        //ExistInList returns true if the new path is already covered in the list
        //Also it removes any folder that the new path already covers
        private bool ExistInList(string path)
        {
            List<Folder> toRemove = new List<Folder>();
            foreach(Folder item in folders)
            {
                if (path.StartsWith(item.path)) 
                {
                    MessageBox.Show("You already blocked this folder with " + path);
                    return true;
                }
                if (item.path.StartsWith(path))
                {
                    toRemove.Add(item);
                }
            }
            foreach(Folder item in toRemove)
            {
                RemoveFolder(item);
            }
            return false;
        }

        private void RemoveFolder(Folder folder)
        {
            if (folder.encrypted)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                MyEncryption.DecryptFolder(folder.path, hash, 32, 8);
                Mouse.OverrideCursor = null;
            }
            folders.Remove(folder);
            RemoveFolderFromRegistry(folder);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FolderDataGrid.SelectedItems != null)
            {
                foreach(var item in FolderDataGrid.SelectedItems) 
                {
                    RemoveFolder((Folder)item);
                    if (FolderDataGrid.SelectedItem == null) return;
                }
            }
            //else if (FolderListBox.Items.Count != 0) FolderListBox.Items.Remove(FolderListBox.Items[FolderListBox.Items.Count - 1]);
            //if (FolderListBox.Items.Count == 0) PathTextBox.Text = "";
        }

        //public string[] GetListBoxStrings()
        //{
        //    string[] paths = new string[FolderListBox.Items.Count];
        //    for (int i = 0; i < FolderListBox.Items.Count; i++)
        //    {
        //        paths[i] = FolderListBox.Items[i].ToString();
        //    }
        //    return paths;
        //}
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
            string[] folderObjects = new string[FolderDataGrid.Items.Count];
            List<string> realPathsList = new List<string>();
            for (int i = 0; i < folderObjects.Length; i++)
            {
                Folder folderObj = folders[i];

                if(folderObj.encrypt != folderObj.encrypted)
                {
                    if (folderObj.encrypt)
                    {
                        MyEncryption.EncryptFolder(folderObj.path, hash, 32, 8);
                        folderObj.encrypted = true;
                    }
                    else
                    {
                        MyEncryption.DecryptFolder(folderObj.path, hash, 32, 8);
                        folderObj.encrypted = false;
                    }
                }

                // this is for the driver
                if (folderObj.block) realPathsList.Add(GetRealPath(folderObj.path).ToUpper());
                //this is for the app
                folderObjects[i] = folderObj.path + " " + folderObj.encrypt.ToString() + " " + folderObj.block.ToString();
            }
            string[] realPaths = new string[realPathsList.Count];
            for (int i = 0; i < realPathsList.Count; i++)
            {
                realPaths[i] = realPathsList[i];
            }

            // set the registry with the new folders (thats where the driver read the folders to block)
            if(realPaths.Length > 0) Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "BlockFolder", realPaths, RegistryValueKind.MultiString);
            else Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "BlockFolder", new string[] {""}, RegistryValueKind.MultiString);
            Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\ControlSet001\\Services\\FsFilter1", "FolderObjects", folderObjects, RegistryValueKind.MultiString);
            Mouse.OverrideCursor = null;
        }
    }
}
