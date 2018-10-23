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
            // Populate the free drive list - this should work right?...
            List<string> freeDriveLetters = AutoMountHelpers.GetFreeDriveLetters();
            // Clear existing items
            ComboAssignedDrive.Items.Clear();
            // Get current assigned drive letter
            string assignedDriveLetter = ((AutoMountSettings)DataContext).AssignedDriveLetter;

            foreach (string drive in freeDriveLetters)
            {
                int index = ComboAssignedDrive.Items.Add(drive);

            }

            ComboAssignedDrive.Text = assignedDriveLetter;
        }

        private void ComboAssignedDrive_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This is a bit random really, the View is loaded before ever being shown, then when shown it Loads again.
            // But, on further investigation, if it's not a user selection change, then "RemovedItems" will be empty.
            if( ComboAssignedDrive.SelectedIndex > -1  && e.RemovedItems.Count > 0 )
            {
                // We have a selection here
                string selection = (string)ComboAssignedDrive.SelectedItem;

                if (selection.Length > 2 && selection.Contains(":\\") )  // Check it's a valid drive letter...
                {
                    ((AutoMountSettings)DataContext).AssignedDriveLetter = (string)ComboAssignedDrive.SelectedItem;
                }               
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Open file browse dialog
            string imageFile = AutoMounter.API.Dialogs.SelectFile("ISO Image|*.iso");
            if(imageFile != "")
            {
                AutoMounter.Plugin.MountGameImage(imageFile);
            }
            else
            {
                AutoMounter.API.Dialogs.ShowErrorMessage("No image file selected", "No Image Selected");
            }
        }
    }
}
