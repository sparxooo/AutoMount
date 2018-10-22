using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PNAutoMounter
{
    class AutoMountHelpers
    {
        /// <summary>
        /// Get a list of free drive letters, including A: B: (as these may be useful if you have an extraordinary amount of mounted drives)
        /// </summary>
        /// <returns>Returns a List<string> of available drive letters</returns>
        internal static List<string> GetFreeDriveLetters()
        {
            // There may be a better way to do this...
            List<string> freeDriveLetters = new List<string>();
            // Add all drive letters to list
            for(int i = 65; i < 91; i++)
            {
                freeDriveLetters.Add(String.Format("{0}:\\", Convert.ToChar(i)));
            }

            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach(DriveInfo drive in drives)
            {
                // Search through drives, removing those from the list that already exist
                freeDriveLetters.Remove(drive.Name);
            }

            return freeDriveLetters;            
        }

        /// <summary>
        /// Checks if a drive letter is free
        /// </summary>
        /// <param name="drive">Drive Letter</param>
        /// <returns>
        /// True: Drive Letter is not in use
        /// False: Drive Letter is in use
        /// </returns>
        internal static bool IsDriveLetterFree(string drive)
        {
            List<String> freeDriveLetters = GetFreeDriveLetters();
            if (freeDriveLetters.Contains(drive))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
