# AutoMount
PlayNite Automount Plugin

This plugin was a quick mockup to see how easy it would be to add disk image auto mounting to PlayNite (https://github.com/JosefNemec/Playnite/)

The plugin uses the Windows Virtual Disk API to mount the ISO file, so will only function on **Windows 8 and above**. Unfortunately the Windows API also **restricts ISO mounting to programs run as UAC Adminstrator. **

The plugin will attempt to mount an ISO for any game that has a "Platform" set as PC, and an Action of "Executable", and it attempts to always mount the disk to the set drive letter (in settings). If the drive letter is not free, mounting will fail.

# Usage
* Install the plugin (create directory in Playnite's Extension folder), copy the PNAutoMounter.dll, extension.yaml, and the appropriate Vanara libraries from the build directory.
* Run Playnite as an administrator
* Games that have images you want to be auto mounted should have Platform set to PC, Launch action set to Executable, and and ISO file listed for them (with .iso extension).

* Assigned drive letter can be changed in Settings > Plugins > AutoMounter
* ISO image can be manually mounted in Settings > Plugins > AutoMounter

# Future ideas
Am looking to explore integration of third party open source disk mounting (WinCDEmu?) or other software (VirtualCloneDrive) to add Cue/Bin support. This may remove the administrator requirement and inflexibility of the windows API. I will also be exploring integrating this functionality into the Playnite source as a side project. As it currently stands with using the Windows API, maybe registering a system service to negate the need to run as administrator would be an option.

# Compiling
Download the source and open in Visual Studio 2017, you will need to download the required Nuget packages
The project will automatically attempt to copy the whole build directory to %AppData%\Playnite\Extensions\AutoMounter, so create that directory first or alter the Post Build Event. To debug the project you either need to load Visual Studio as an administrator, or attach to a Playnite instance running as an administrator.


# References
Uses the Vanara PInvoke library (https://github.com/dahall/Vanara/)
Majority of ISO mounting code (and the workaround to find and manually assign a drive letter) derived from Pixy's answer (https://stackoverflow.com/questions/24396644/)
