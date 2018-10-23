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
    class WinVirtDisk : BaseDiskMounter
    {
        SafeFileHandle mountedHandle = null;

        public WinVirtDisk(string imageFile)
        {
            DiskImage = imageFile;
        }

        public override bool MountDiskImage()
        {
            if( Path.GetExtension(DiskImage).ToLower() != ".iso")
            {
                AutoMounter.Plugin.LogInfo($"WinVirtDisk: Failed to open ISO as VirtualDisk, not an ISO - {DiskImage}");
                return false;
            }

            // Check file exists! Probably should return some nice error message...
            if (!File.Exists(DiskImage))
            {
                AutoMounter.Plugin.LogInfo($"WinVirtDisk: Failed to open ISO as VirtualDisk, ISO does not exist on disk {DiskImage}");
                return false;
            }

            if (!AutoMountHelpers.IsDriveLetterFree(RequestedDriveLetter))
            {
                AutoMounter.Plugin.LogInfo($"WinVirtDisk: Failed to open ISO as VirtualDisk, Drive Letter in use {RequestedDriveLetter}");
                AutoMounter.API.Dialogs.ShowErrorMessage("Failed to open ISO as VirtualDisk, Drive Letter is in use", "AutoMounter");
                return false;
            }


            VirtDisk.VIRTUAL_STORAGE_TYPE type = new VirtDisk.VIRTUAL_STORAGE_TYPE(VirtDisk.VIRTUAL_STORAGE_TYPE_DEVICE_TYPE.VIRTUAL_STORAGE_TYPE_DEVICE_ISO);
            Win32Error error = VirtDisk.OpenVirtualDisk(
                                                        ref type,
                                                        DiskImage,
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
                    if (AssignDriveLetter(RequestedDriveLetter, mountedHandle))
                    {
                        return true;
                    }
                    else
                    {
                        AutoMounter.Plugin.LogInfo($"WinVirtDisk: Failed to open ISO as VirtualDisk, Unable to mount to drive letter {RequestedDriveLetter}");
                        AutoMounter.API.Dialogs.ShowErrorMessage("Failed to open ISO as VirtualDisk, Unable to mount to assigned drive letter", "AutoMounter");
                        UnmountCurrentDiskImage();
                        return false;
                    }

                }
                else
                {
                    AutoMounter.Plugin.LogInfo($"WinVirtDisk: Failed to open ISO as VirtualDisk {error.ToString()}, AttachVirtualDisk Fail");
                    AutoMounter.API.Dialogs.ShowErrorMessage("Failed to open ISO as VirtualDisk, AttachVirtualDisk Fail", "AutoMounter");
                    UnmountCurrentDiskImage();
                    return false;
                }

            }
            else
            {
                AutoMounter.Plugin.LogInfo($"WinVirtDisk: Failed to open ISO as VirtualDisk {error.ToString()}, OpenVirtualDisk Fail");
                AutoMounter.API.Dialogs.ShowErrorMessage("Failed to open ISO as VirtualDisk, OpenVirtualDisk Fail", "AutoMounter");
                UnmountCurrentDiskImage();
                return false;
            }
        }

        public override bool IsCurrentDiskImageMounted()
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

            // WinAPI requires backslash on back of drive letter
            if (!driveLetter.Contains("\\"))
                driveLetter += "\\";

            bool mounted = Kernel32.SetVolumeMountPoint(driveLetter, volumeName.Append("\\").ToString());
            if (mounted)
            {
                AutoMounter.Plugin.LogInfo($"WinVirtDisk: Mounted ISO to {driveLetter} on device {volumeName}");
                return true;
            }
            else
            {
                AutoMounter.Log.Info("AutoMounter: Failed setting volume mount point");
                return false;
            }
        }

        public override bool UnmountCurrentDiskImage()
        {
            if (mountedHandle != null)
            {
                Win32Error error = VirtDisk.DetachVirtualDisk(mountedHandle, VirtDisk.DETACH_VIRTUAL_DISK_FLAG.DETACH_VIRTUAL_DISK_FLAG_NONE, 0);

                if (error.Failed)
                {
                    AutoMounter.Plugin.LogInfo($"WinVirtDisk: Failed to unmount disk from system {mountedHandle.ToString()}, {error.ToString()}");
                    AutoMounter.API.Dialogs.ShowErrorMessage("Failed to unmount disk from system", "AutoMounter");
                }
                mountedHandle.Close();
                mountedHandle = null;
            }
            return true;
        }
     
        public override bool MountNewDiskImage(string imageName)
        {
            DiskImage = imageName;
            return MountDiskImage();
        }

        public override bool IsRequestedDriveLetterAvailable()
        {
            return AutoMountHelpers.IsDriveLetterFree(RequestedDriveLetter);
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