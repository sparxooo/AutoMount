# AutoMount
PlayNite Automount Plugin

This plugin was a quick mockup to see how easy it would be to add disk image auto mounting to PlayNite (https://github.com/JosefNemec/Playnite/)

The plugin can use either WinCDEmu installed (http://wincdemu.sysprogs.org/), or the Windows Virtual Disk API to mount the ISO file.
* WinCDEmu if installed with the option "Require UAC Administrator" unchecked can mount files without being run as admin.
* Windows API will only function on **Windows 8 and above**. Unfortunately the Windows API also **restricts ISO mounting to programs run as UAC Adminstrator** and also can only mount ISO files.

The plugin will attempt to mount a game image for any game that has a "Platform" set as PC, and an Action of "Executable", and it attempts to always mount the disk to the set drive letter (in settings). If the drive letter is not free, mounting will fail. This plugin may not work correctly at present for all games depending upon how quickly they launch. WinCDEmu is MUCH faster at mounting an image than the Windows API.

# Usage
* Install WinCDEmu (if you wish to use this). Ensure "require administrator to mount image" during setup is not enabled.
* Install the plugin (create directory in Playnite's Extension folder), copy the PNAutoMounter.dll, extension.yaml, and the appropriate Vanara libraries from the build directory. 
* **(Only required if not using WinCDEmu) Run Playnite as an administrator**
* Games that have images you want to be auto mounted should have Platform set to PC, Launch action set to Executable, and and game image file listed for them (Windows API limited to *.iso, WinCDEmu can mount *.iso, *.bin and *.cue).
* Assigned drive letter can be changed in Settings > Plugins > AutoMounter
* WinCDEmu location can be changed in Settings > Plugins > AutoMounter
* Disk image can be manually mounted in Settings > Plugins > AutoMounter (i.e. to install a game from the correct drive letter)

# Future ideas
* This may not work for all games at present, there is no feature in the Playnite SDK to pause execution of a game until a CD image is mounted.
* Per-game settings (either plugin based, or per a future feature in the Playnite SDK)
* Removal of Windows API image mounting (as SLOW and unreliable/hacky).

# Compiling
Download the source and open in Visual Studio 2017, you will need to download the required Nuget packages
The project will automatically attempt to copy the whole build directory to %AppData%\Playnite\Extensions\AutoMounter, so create that directory first or alter the Post Build Event. 


# References
* Uses the Vanara PInvoke library (https://github.com/dahall/Vanara/)
* Majority of WinAPI ISO mounting code (and the workaround to find and manually assign a drive letter) derived from Pixy's answer (https://stackoverflow.com/questions/24396644/)
* Uses WinCDEmu if desired (http://wincdemu.sysprogs.org/)
