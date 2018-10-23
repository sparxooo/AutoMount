using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PNAutoMounter
{
    /// <summary>
    ///  This class wraps around the command line tool for WinCDEmu (should be installed for administrator-less mounting
    /// </summary>
    public class WCDEDisk : BaseDiskMounter
    {
        bool isMounted = false;

        public WCDEDisk(string imageFile)
        {
            DiskImage = imageFile;
        }

        public override bool IsCurrentDiskImageMounted()
        {
            return isMounted;
        }

        public override bool IsRequestedDriveLetterAvailable()
        {
            return AutoMountHelpers.IsDriveLetterFree(RequestedDriveLetter);
        }

        /// <summary>
        /// Mount disk image using WinCDEmu and it's batchmnt executable
        /// </summary>
        /// <returns></returns>
        public override bool MountDiskImage()
        {   
            string wcde = ( (AutoMountSettings)AutoMounter.Plugin.Settings ).WinCDEmuLocation;

            if (!File.Exists(wcde))
            {
                AutoMounter.Plugin.LogInfo($"WCDE: Cannot find batchmnt.exe");
                return false;
            }

            if (!File.Exists(DiskImage))
            {
                AutoMounter.Plugin.LogInfo($"WCDE: Cannot find disk image: {DiskImage}");
                return false;
            }
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = wcde;
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(wcde);
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.Arguments = $"\"{DiskImage}\" {RequestedDriveLetter}";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
                string output = p.StandardOutput.ReadToEnd();
                // Write output to log
                AutoMounter.Plugin.LogInfo($"WCDE: {output.Trim()}");
                if (output.Contains("The operation completed successfully"))
                {
                    isMounted = true;
                    AutoMounter.Plugin.LogInfo($"WCDE: Image mounted");
                    return true;
                }
                else
                {
                    isMounted = false;
                    UnmountCurrentDiskImage();
                    return false;
                }
            }
            catch (Exception ex)
            {
                AutoMounter.Plugin.LogInfo($"WCDE: Error mounting disk image. {ex.ToString()}");
            }

            return false;
        }

        public override bool MountNewDiskImage(string imageName)
        {
            if (File.Exists(imageName))
            {
                DiskImage = imageName;
            }
            return MountDiskImage();
        }

        public override bool UnmountCurrentDiskImage()
        {
            string wcde = ( (AutoMountSettings)AutoMounter.Plugin.Settings ).WinCDEmuLocation;

            if (!File.Exists(wcde))
            {
                AutoMounter.Plugin.LogInfo($"WCDE: Cannot find batchmnt.exe");
                return false;
            }

            try
            {
                Process p = new Process();
                p.StartInfo.FileName = wcde;
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(wcde);
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.Arguments = $"/unmount {RequestedDriveLetter}";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit();
                return true;
            }
            catch (Exception ex)
            {
                AutoMounter.Plugin.LogInfo($"WCDE: Error unmounting: {ex.ToString()}");
            }

            return false;
        }
    }

}
