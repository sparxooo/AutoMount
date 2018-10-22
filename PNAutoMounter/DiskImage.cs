using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Vanara.PInvoke;

namespace PNAutoMounter
{
    /// <summary>
    /// Class to manage mounting of disk images
    /// </summary>
    class DiskImage : IDisposable
    {
        string imageFile = "";
        SafeFileHandle mountedHandle = null;

        public DiskImage(string imageFile)
        {
            this.imageFile = imageFile;
        }

        public bool Mount(string driveLetter)
        {
            // Check file exists! Probably should return some nice error message...
            if (!File.Exists(imageFile))
            {
                AutoMounter.Log.Info(String.Format("AutoMounter: Failed to open ISO as VirtualDisk, ISO does not exist on disk {0}", imageFile));
                return false;
            }

            if (!AutoMountHelpers.IsDriveLetterFree(driveLetter))
            {
                AutoMounter.Log.Info(String.Format("AutoMounter: Failed to open ISO as VirtualDisk, Drive Letter in use {0}", driveLetter));
                AutoMounter.API.Dialogs.ShowErrorMessage("Failed to open ISO as VirtualDisk, Drive Letter is in use", "AutoMounter");
                return false;
            }


            VirtDisk.VIRTUAL_STORAGE_TYPE type = new VirtDisk.VIRTUAL_STORAGE_TYPE(VirtDisk.VIRTUAL_STORAGE_TYPE_DEVICE_TYPE.VIRTUAL_STORAGE_TYPE_DEVICE_ISO);
            Win32Error error = VirtDisk.OpenVirtualDisk(
                                                        ref type,
                                                        imageFile,
                                                        VirtDisk.VIRTUAL_DISK_ACCESS_MASK.VIRTUAL_DISK_ACCESS_READ,
                                                        VirtDisk.OPEN_VIRTUAL_DISK_FLAG.OPEN_VIRTUAL_DISK_FLAG_NONE,
                                                        null,
                                                       out mountedHandle);
            // Opened successfully
            if (error.Succeeded)
            {
                VirtDisk.ATTACH_VIRTUAL_DISK_PARAMETERS param = VirtDisk.ATTACH_VIRTUAL_DISK_PARAMETERS.Default;

                // Attach disk (no drive letter)
                error = VirtDisk.AttachVirtualDisk(
                    mountedHandle,
                    NullSafeHandle.Null(),
                    VirtDisk.ATTACH_VIRTUAL_DISK_FLAG.ATTACH_VIRTUAL_DISK_FLAG_NO_DRIVE_LETTER |
                    VirtDisk.ATTACH_VIRTUAL_DISK_FLAG.ATTACH_VIRTUAL_DISK_FLAG_READ_ONLY, // NO_DRIVE_LETTER at a later date
                    0,
                    ref param,
                    IntPtr.Zero);

                if (error.Succeeded)
                {
                    // Try and assign the requested drive letter to the mounted image
                    if (AssignDriveLetter(driveLetter, mountedHandle))
                    {
                        return true;
                    }
                    else
                    {
                        AutoMounter.Log.Info(String.Format("AutoMounter: Failed to open ISO as VirtualDisk, Unable to mount to drive letter {0}", driveLetter));
                        AutoMounter.API.Dialogs.ShowErrorMessage("Failed to open ISO as VirtualDisk, Unable to mount to assigned drive letter", "AutoMounter");
                        Unmount();
                        return false;
                    }

                }
                else
                {
                    AutoMounter.Log.Info(String.Format("AutoMounter: Failed to open ISO as VirtualDisk {0}, AttachVirtualDisk Fail", error.ToString()));
                    AutoMounter.API.Dialogs.ShowErrorMessage("Failed to open ISO as VirtualDisk, AttachVirtualDisk Fail", "AutoMounter");
                    Unmount();
                    return false;
                }

            }
            else
            {
                AutoMounter.Log.Info(String.Format("AutoMounter: Failed to open ISO as VirtualDisk {0}, OpenVirtualDisk Fail", error.ToString()));
                AutoMounter.API.Dialogs.ShowErrorMessage("Failed to open ISO as VirtualDisk, OpenVirtualDisk Fail", "AutoMounter");
                Unmount();
                return false;
            }
        }

        internal bool IsMounted()
        {
            if (mountedHandle == null)
                return false;
            return true;
        }

        private bool AssignDriveLetter(string driveLetter, SafeFileHandle mountedHandle)
        {
            int buffsize = 128;
            StringBuilder physicalPath = new StringBuilder(128);
            Win32Error error = VirtDisk.GetVirtualDiskPhysicalPath(mountedHandle, ref buffsize, physicalPath);
            // Find number on end of physical drive string \\.\CDROM1
            int driveNumber = Convert.ToInt32(Regex.Match(physicalPath.ToString(), @"\d+").Value);
            STORAGE_DEVICE_NUMBER deviceNumber = new STORAGE_DEVICE_NUMBER();
            // driveNumber should now contain the virtual CDROM drive number

            StringBuilder volumeName = new StringBuilder(128);
            
            Kernel32.SafeVolumeSearchHandle srchHandle = Kernel32.FindFirstVolume(volumeName, (uint)128);


            bool found = false;
            while (!found)
            {
                if (volumeName[volumeName.Length - 1] == '\\')
                    volumeName.Remove(volumeName.Length - 1, 1);

                SafeFileHandle fileHandle = Kernel32.CreateFile(volumeName.ToString(), Kernel32.FileAccess.FILE_LIST_DIRECTORY, FileShare.Read, null, FileMode.Open, 0);
                if (!fileHandle.IsInvalid)
                {
                    // Valid
                    Kernel32.DeviceIoControl<STORAGE_DEVICE_NUMBER>(fileHandle, Kernel32.IOControlCode.IOCTL_STORAGE_GET_DEVICE_NUMBER, out deviceNumber);
                    // Check device stats, should be a CDROM drive, then we should be ok to assume the disk number is ours...
                    if (deviceNumber.deviceType == 2)
                    {
                        // Is a CDROM drive
                        if (deviceNumber.deviceNumber == driveNumber)
                        {
                            // Got it!
                            break;
                        }
                    }

                }

                if (!Kernel32.FindNextVolume(srchHandle, volumeName, (uint)128))
                    break;
            }

            srchHandle.Close();
            // volumeName is now our mounted CDROM drive



            bool mounted = Kernel32.SetVolumeMountPoint(driveLetter, volumeName.Append("\\").ToString());
            if (mounted)
            {
                AutoMounter.Log.Info(String.Format("AutoMounter: Mounted ISO to {0} on device {1}", driveLetter, volumeName));
                return true;
            }
            else
            {
                AutoMounter.Log.Info("AutoMounter: Failed setting volume mount point");
                return false;
            }
        }

        public bool Unmount()
        {
            if (mountedHandle != null)
            {
                Win32Error error = VirtDisk.DetachVirtualDisk(mountedHandle, VirtDisk.DETACH_VIRTUAL_DISK_FLAG.DETACH_VIRTUAL_DISK_FLAG_NONE, 0);

                if (error.Failed)
                {
                    AutoMounter.Log.Info(String.Format("AutoMounter: Failed to unmount disk from system {0}, {1}", mountedHandle.ToString(), error.ToString()));
                    AutoMounter.API.Dialogs.ShowErrorMessage("Failed to unmount disk from system", "AutoMounter");
                }
                mountedHandle.Close();
                mountedHandle = null;
            }
            return true;
        }

        public void Dispose()
        {
            // Ensure unmount before disposal
            Unmount();
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STORAGE_DEVICE_NUMBER
    {
        public Int32 deviceType;
        public Int32 deviceNumber;
        public Int32 partitionNumber;
    }

    // Hack...!
    class NullSafeHandle : SafeHandle
    {
        public NullSafeHandle() : base(new IntPtr(-1), true)
        {
            this.SetHandle(IntPtr.Zero);
        }
        public override bool IsInvalid => false;

        public static NullSafeHandle Null()
        {
            return new NullSafeHandle();
        }

        protected override bool ReleaseHandle()
        {
            // not Required
            return true;
        }
    }
}