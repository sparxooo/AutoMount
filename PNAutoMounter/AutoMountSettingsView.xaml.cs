using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PNAutoMounter
{
    /// <summary>
    /// Interaction logic for AutoMounterSettingsView.xaml
    /// </summary>
    public partial class AutoMounterSettingsView : UserControl
    {
        public AutoMounterSettingsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure no cd images mounted
            if (AutoMounter.Plugin.currentImage != null)
            {
                AutoMounter.Plugin.UnmountGameImage();
            }

            // Populate the free drive list - this should work right?...
            List<string> freeDriveLetters = AutoMountHelpers.GetFreeDriveLetters();
            // Clear existing items
            ComboAssignedDrive.Items.Clear();
            // Get current assigned drive letter
            string assignedDriveLetter = ( (AutoMountSettings)DataContext ).AssignedDriveLetter;

            foreach (string drive in freeDriveLetters)
            {
                int index = ComboAssignedDrive.Items.Add(drive);

            }

            ComboAssignedDrive.Text = assignedDriveLetter;

            // Populate CD mounting engine list
            ComboEngine.Items.Clear();
            Array enumVals = Enum.GetValues(typeof(CDMountingEngine));
            foreach (CDMountingEngine cd in enumVals)
            {
                ComboEngine.Items.Add(cd);
            }

            // Set selected item to current setting
            ComboEngine.SelectedItem = ( (AutoMountSettings)DataContext ).Engine;


        }

        private void ComboAssignedDrive_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This is a bit random really, the View is loaded before ever being shown, then when shown it Loads again.
            // On further investigation, if it's not a user selection change, then "RemovedItems" will be empty.
            if (ComboAssignedDrive.SelectedIndex > -1 && e.RemovedItems.Count > 0)
            {
                // We have a selection here
                string selection = (string)ComboAssignedDrive.SelectedItem;

                if (selection.Length > 1 && selection.Contains(":"))  // Check it's a valid drive letter...
                {
                    ( (AutoMountSettings)DataContext ).AssignedDriveLetter = (string)ComboAssignedDrive.SelectedItem;
                }
            }
        }

        private void ComboEngine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboEngine.SelectedIndex > -1 && e.RemovedItems.Count > 0)
            {
                // We insert the items into the combobox by enumerating the enum, there should never be invalid items...
                try
                {
                    CDMountingEngine selection = (CDMountingEngine)ComboEngine.SelectedItem;
                    ( (AutoMountSettings)DataContext ).Engine = selection;
                }
                catch { throw new Exception("Had invalid option for Engine selection"); }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Open file browse dialog
            string imageFile = AutoMounter.API.Dialogs.SelectFile("ISO Image|*.iso;Cue File|*.cue;Bin File|*.bin");
            if (imageFile != "")
            {
                AutoMounter.Plugin.MountGameImage(imageFile);
            }
            else
            {
                AutoMounter.API.Dialogs.ShowErrorMessage("No image file selected", "No Image Selected");
            }
        }

        private void ButtonWinCDEmuBrowse_Click(object sender, RoutedEventArgs e)
        {
            // Show file browse dialog
            AutoMounter.API.Dialogs.ShowMessage("Please find the batchmnt.exe executable in WinCDEmu's program folder");

            string WCDEBatchLoc = AutoMounter.API.Dialogs.SelectFile("Batch Mounter (batchmnt.exe)|batchmnt.exe");

            if (WCDEBatchLoc.Trim() != "")
            {
                try
                {
                    // Attempt to launch application
                    ProcessStartInfo pInfo = new ProcessStartInfo
                    {
                        FileName = WCDEBatchLoc,
                        WorkingDirectory = Path.GetDirectoryName(WCDEBatchLoc),
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                };
                    Process p = new Process
                    {
                        StartInfo = pInfo
                    };
                    p.Start();

                    // Read to end of output
                    string output = p.StandardOutput.ReadToEnd();

                    // Check for expected output (batchmnt.exe - WinCDEmu batch mounter.)
                    if( output.Contains("batchmnt.exe - WinCDEmu batch mounter.") )
                    {
                        // Accept setting
                        ( (AutoMountSettings)AutoMounter.Plugin.Settings ).WinCDEmuLocation = WCDEBatchLoc;
                    }
                }
                catch (Exception ex)
                {
                    AutoMounter.API.Dialogs.ShowErrorMessage("Unable to launch batchmnt.exe", "Error");
                    AutoMounter.Plugin.LogInfo($"Settings: {ex.ToString()}");
                }

            }
        }
    }
}
