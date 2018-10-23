using System;
using System.IO;

namespace PNAutoMounter
{
    /// <summary>
    ///  Base class to use for disk mounting
    /// </summary>
    public abstract class BaseDiskMounter : IDisposable
    {
        #region Properties
        /// <summary>
        /// Get or set the requested drive letter to mount the image (letter without colon)
        /// </summary>
        public virtual string RequestedDriveLetter { get; set; }

        /// <summary>
        /// Disk image to mount
        /// </summary>
        public virtual string DiskImage { get; set; }

        /// <summary>
        /// Return the DiskMounter name
        /// </summary>
        public virtual string DiskMounterName { get; }
        #endregion

        /// <summary>
        /// Mount the specified disk image
        /// </summary>
        /// <returns>Mount successful</returns>
        public abstract bool MountDiskImage();

        /// <summary>
        /// Mount a new disk image (to an already mounted drive), or mount a specified disk image
        /// </summary>
        /// <param name="imageName"></param>
        /// <returns>Mount successful</returns>
        public abstract bool MountNewDiskImage(string imageName);

        /// <summary>
        /// Unmount the currently mounted disk image
        /// </summary>
        /// <returns>Unmount successful</returns>
        public abstract bool UnmountCurrentDiskImage();

        /// <summary>
        /// Returns true if the current requested drive letter is available
        /// </summary>
        public abstract bool IsRequestedDriveLetterAvailable();

        /// <summary>
        /// Returns true if the current disk image is mounted
        /// </summary>
        public abstract bool IsCurrentDiskImageMounted();


        /// <summary>
        /// Important, when garbage collected, unmount the disk image to prevent persistence once application unloaded
        /// </summary>
        public void Dispose()
        {
            if (IsCurrentDiskImageMounted())
            {
                try
                {
                    UnmountCurrentDiskImage();
                }
                catch { }
            }
        }
    }
}