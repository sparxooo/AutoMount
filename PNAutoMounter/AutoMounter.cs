using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace PNAutoMounter
{
    public class AutoMounter : Playnite.SDK.Plugins.IGenericPlugin
    {
        internal static ILogger Log;
        internal static IPlayniteAPI API;
        internal static AutoMounter Plugin;
        internal DiskImage currentImage;
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
            // Plugin is dumb at this point, and if an ISO image is present and the Game ActionType is File we will mount it...
            if (game.PlayAction != null && game.PlayAction.Type == GameActionType.File)
            {
                Log.Info(String.Format("AutoMounter: Game {0} starting, is \"File\" ActionType, looking for ISO", game.Name));
                // Get Game Image Path
                string isoImageName = game.GameImagePath;
                if (isoImageName != null && Path.GetExtension(isoImageName) == ".iso")
                {
                    // Found iso image, does File exist?
                    if (File.Exists(isoImageName))
                    {
                        // Exists
                        Log.Info(String.Format("AutoMounter: Mounting ISO for {0}", game.Name));
                        MountGameImage(game);
                    }
                    else
                    {
                        Log.Info(String.Format("AutoMounter: {0} has an ISO listed, but image doesn't appear to exist on disk", game.Name));
                    }
                }
                else
                {
                    Log.Info(String.Format("AutoMounter: No ISO found for Game: {0}", game.Name));
                }
            }
            else
            {
                Log.Info(String.Format("AutoMounter: Not mounting image for Game {0}", game.Name));

            }
        }

        public void OnGameStopped(Game game, long ellapsedSeconds)
        {
            if (currentImage != null && currentImage.IsMounted() == true)
            {
                UnmountGameImage(game);
            }
        }

        public void OnGameUninstalled(Game game)
        {

        }

        private void MountGameImage(Game game)
        {
            if (currentImage == null || currentImage.IsMounted())
            {
                currentImage = new DiskImage(game.GameImagePath);
                currentImage.Mount(((AutoMountSettings)Settings).AssignedDriveLetter);
            }
        }

        private void UnmountGameImage(Game game)
        {
            currentImage.Unmount();
            currentImage = null;
        }

    }
}
