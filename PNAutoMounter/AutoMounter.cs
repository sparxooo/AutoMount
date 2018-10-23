using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System.Threading;

namespace PNAutoMounter
{
    public class AutoMounter : Playnite.SDK.Plugins.IGenericPlugin
    {
        internal static ILogger Log;
        internal static IPlayniteAPI API;
        internal static AutoMounter Plugin;

        internal BaseDiskMounter currentImage;

        public UserControl SettingsView
        {
            get => new AutoMounterSettingsView();
        }

        public ISettings Settings { get; set; }

        public AutoMounter(IPlayniteAPI api)
        {
            API = api;
            Plugin = this;
            Settings = new AutoMountSettings(this, API);
        }

        public Guid Id => new Guid("a491781c-5c74-4fd2-bff4-7e34f395eca2");

        public void Dispose()
        {
            // Will unmount disk image if still mounted on disposing of plugin
        }

        public IEnumerable<ExtensionFunction> GetFunctions()
        {
            return null;
        }

        public void OnApplicationStarted()
        {
            // Get Logger
            Log = LogManager.GetLogger();
        }

        public void OnGameInstalled(Game game)
        {

        }

        public void OnGameStarted(Game game)
        {

        }

        public void OnGameStarting(Game game)
        {            
            // Will need to mount disk image in this section.
            // Plugin is dumb at this point, if a disk image is present, platform is PC, and the Game ActionType is File we will mount it...

            // Get Platform. There is probably a better way to do this
            bool isPC = false;
            Platform gamePlatform = API.Database.GetPlatform(game.PlatformId);
            if (gamePlatform != null && gamePlatform.Name == "PC")
            { isPC = true; }

            if (game.PlayAction != null && game.PlayAction.Type == GameActionType.File && isPC)
            {
                LogInfo($"Game {game.Name} starting, is \"File\" ActionType, looking for disk image");
                // Get Game Image Path
                string isoImageName = game.GameImagePath;
                if (isoImageName != null)
                {
                    string fileExtension = Path.GetExtension(isoImageName).ToLower();
                    if (fileExtension == ".iso" || fileExtension == ".cue" || fileExtension == ".bin")
                    {
                        // Found iso image, does File exist?
                        if (File.Exists(isoImageName))
                        {
                            // Exists
                            LogInfo($"Mounting ISO for {game.Name}");
                            MountGameImage(game.GameImagePath);
                        }
                        else
                        {
                            LogInfo($"{game.Name} has an ISO listed, but image doesn't appear to exist on disk");
                        }
                    }
                }
                else
                {
                    LogInfo($"No disk image found for Game: {game.Name}");
                }
            }
            else
            {
                LogInfo(String.Format("AutoMounter: Not mounting image for Game {0}", game.Name));

            }
        }

        public void OnGameStopped(Game game, long ellapsedSeconds)
        {
            if (currentImage != null && currentImage.IsCurrentDiskImageMounted() == true)
            {
                UnmountGameImage();
            }
        }

        public void OnGameUninstalled(Game game)
        {

        }

        public void MountGameImage(string imagePath)
        {
            if (currentImage != null && currentImage.IsCurrentDiskImageMounted())
            {
                UnmountGameImage();
            }

            CDMountingEngine engine = ( (AutoMountSettings)Settings ).Engine;
            switch (engine)
            {
                case CDMountingEngine.WinCDEmu:
                    currentImage = new WCDEDisk(imagePath);
                    break;
                case CDMountingEngine.WinVirtDisk:
                    currentImage = new WinVirtDisk(imagePath);
                    break;
            }


            currentImage.RequestedDriveLetter = ( (AutoMountSettings)Settings ).AssignedDriveLetter;
            currentImage.MountDiskImage();
        }

        public void UnmountGameImage()
        {
            if (currentImage != null)
            {
                currentImage.UnmountCurrentDiskImage();
                currentImage = null;
            }
        }

        public void LogInfo(string message)
        {
            Log.Info($"AutoMounter: {message}");
        }

        public void LogError(string message)
        {
            Log.Error($"AutoMounter: {message}");
        }

    }
}
