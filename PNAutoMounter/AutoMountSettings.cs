using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Playnite.SDK;

namespace PNAutoMounter
{
    class AutoMountSettings : Playnite.SDK.ISettings
    {
        /// <summary>
        /// Default drive letter, Z:\ as furthest away from auto-assigned letters
        /// Should contain ending slash
        /// </summary>

        public string AssignedDriveLetter { get; set; } = "Z:";
        public CDMountingEngine Engine { get; set; } = CDMountingEngine.WinCDEmu; // Use WinCDEmu as default
        public string WinCDEmuLocation { get; set; } = @"C:\Program Files(x86)\wincdemu\batchmnt.exe";

        private IPlayniteAPI api;
        private AutoMounter plugin;

        public AutoMountSettings()
        {
        }

        public AutoMountSettings(AutoMounter plugin, IPlayniteAPI api)
        {
            // Main Playnite API instance injected into original instance of your plugin.
            this.api = api;

            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = api.LoadPluginSettings<AutoMountSettings>(plugin);

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                AssignedDriveLetter = savedSettings.AssignedDriveLetter;
                Engine = savedSettings.Engine;
                WinCDEmuLocation = savedSettings.WinCDEmuLocation;
            }
        }


        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.

            api.SavePluginSettings(plugin, this);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.

            // Check if Drive letter is free, should not already be assigned
            // Should probably check if any image is mounted before we do this, and if so, unmount
            errors = new List<string>();

            // Unmount game image
            AutoMounter.Plugin.UnmountGameImage();

            if (!AutoMountHelpers.IsDriveLetterFree(AssignedDriveLetter))
            {
                errors.Add("Assigned Drive Letter is not free...");
            }

            if (Engine == CDMountingEngine.WinCDEmu && !File.Exists(WinCDEmuLocation))
            {
                errors.Add("Location of WinCDEmu incorrect");
            }


            if (errors.Count() > 0)
                return false;
            else
                return true;
        }
    }
}
