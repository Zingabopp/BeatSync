using BeatSync.Configs;
using BeatSync.Logging;
using IPA;
using IPA.Config;
using IPA.Utilities;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace BeatSync
{
    public class Plugin : IBeatSaberPlugin
    {
        // From SongCore
        internal static readonly string CachedHashDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"..\LocalLow\Hyperbolic Magnetism\Beat Saber\SongHashData.dat");
        internal const string PlaylistsPath = "Playlists";
        internal const string CustomLevelsPath = @"Beat Saber_Data\CustomLevels";
        internal const string UserDataPath = "UserData";

        internal static Ref<PluginConfig> config;
        internal static IConfigProvider configProvider;
        private bool customUIExists = false;

        #region Setting Properties

        #endregion

        public void Init(IPALogger logger, [Config.Prefer("json")] IConfigProvider cfgProvider)
        {
            IPA.Logging.StandardLogger.PrintFilter = IPA.Logging.Logger.LogLevel.All;
            Logger.log = new BeatSyncIPALogger(logger);
            Logger.log?.Debug("Logger initialied.");
            configProvider = cfgProvider;
            config = configProvider.MakeLink<PluginConfig>((p, v) =>
            {
                // Build new config file if it doesn't exist or RegenerateConfig is true
                if (v.Value == null || v.Value.RegenerateConfig)
                {
                    p.Store(v.Value = new PluginConfig().SetDefaults());
                }
                config = v;
            });
        }



        public void OnApplicationStart()
        {
            Logger.log?.Debug("OnApplicationStart");
            //SongFeedReaders.Util.Logger = new BeatSyncFeedReaderLogger();
            // Check if CustomUI is installed.
            customUIExists = IPA.Loader.PluginManager.AllPlugins.FirstOrDefault(c => c.Metadata.Name == "Custom UI") != null;
            // If Custom UI is installed, create the UI
            if (customUIExists)
                CustomUI.Utilities.BSEvents.menuSceneLoadedFresh += MenuLoadedFresh;
            // Called to set the WebClient SongFeedReaders uses
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());

            // TODO: Need to make this better, use a LoggerFactory, have the readers only auto-get a logger if null?
            var readerLogger = new Logging.BeatSyncFeedReaderLogger(SongFeedReaders.Logging.LoggingController.DefaultLogController);
            SongFeedReaders.BeastSaberReader.Logger = readerLogger;
            SongFeedReaders.BeatSaverReader.Logger = readerLogger;
            SongFeedReaders.ScoreSaberReader.Logger = readerLogger;
            SongFeedReaders.Utilities.Logger = readerLogger;
            SongFeedReaders.WebUtils.Logger = readerLogger;
            //SongFeedReaders.DataflowAlternative.TransformBlock.Logger = readerLogger;

        }

        public void OnApplicationQuit()
        {
            Logger.log?.Debug("OnApplicationQuit");
        }

        /// <summary>
        /// Called when the active scene is changed.
        /// </summary>
        /// <param name="prevScene">The scene you are transitioning from.</param>
        /// <param name="nextScene">The scene you are transitioning to.</param>
        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
            if (nextScene.name == "HealthWarning")
            {
                BeatSync.Paused = false;
                var beatSync = new GameObject().AddComponent<BeatSync>();
                GameObject.DontDestroyOnLoad(beatSync);
            }
            if (nextScene.name == "GameCore")
                BeatSync.Paused = true;
            else
                BeatSync.Paused = false;
        }

        /// <summary>
        /// Called when BSEvents.menuSceneLoadedFresh is triggered. UI creation is in here instead of
        /// OnSceneLoaded because some settings won't work otherwise.
        /// </summary>
        public void MenuLoadedFresh()
        {
            {
                Logger.log?.Debug("Creating BeatSync's UI");
                UI.BeatSync_UI.CreateUI();
                var settingsMenu = GameObject.FindObjectOfType<SettingsFlowCoordinator>();
                try
                {
                    settingsMenu.didFinishEvent -= SettingsMenu_didFinishEvent;
                    settingsMenu.didFinishEvent += SettingsMenu_didFinishEvent;
                }
                catch (Exception ex)
                {
                    Logger.log?.Critical("Could not find the SettingsFlowCoordinator. BeatSync settings will not be able to save.");
                }
            }

        }

        private void SettingsMenu_didFinishEvent(SettingsFlowCoordinator sender, SettingsFlowCoordinator.FinishAction finishAction)
        {
            try
            {
                if (finishAction != SettingsFlowCoordinator.FinishAction.Cancel)
                {
                    Logger.log?.Debug("Saving settings.");
                    configProvider.Store(config.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Critical($"Error saving settings.\n{ex.Message}\n{ex.StackTrace}");
            }
        }


        /// <summary>
        /// Runs at a fixed intervalue, generally used for physics calculations. 
        /// </summary>
        public void OnFixedUpdate()
        {

        }

        /// <summary>
        /// This is called every frame.
        /// </summary>
        public void OnUpdate()
        {

        }


        /// <summary>
        /// Called when the a scene's assets are loaded.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="sceneMode"></param>
        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {



        }

        public void OnSceneUnloaded(Scene scene)
        {

        }
    }
}
