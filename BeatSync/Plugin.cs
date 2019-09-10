using BeatSync.Configs;
using BeatSync.Logging;
using IPA;
using IPA.Config;
using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
        private bool beatSyncCreated = false;

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
                    string reason = v.Value == null ? "BeatSync.json was not found." : "RegenerateConfig is true.";
                    Logger.log?.Debug($"Creating new config because {reason}");
                    p.Store(v.Value = new PluginConfig(true));
                    v.Value.ResetConfigChanged();
                }
                else
                {
                    //v.Value.RegenerateConfig = false;
                    v.Value.ResetConfigChanged();
                    v.Value.FillDefaults();
                    if (v.Value.ConfigChanged)
                    {
                        Logger.log?.Debug("Plugin.Init(): Saving settings.");
                        v.Value.ResetConfigChanged();
                        p.Store(v.Value);
                    }
                }
                config = v;
            });
        }



        public void OnApplicationStart()
        {
            Logger.log?.Debug("OnApplicationStart");
            //SongFeedReaders.Util.Logger = new BeatSyncFeedReaderLogger();
            // Check if CustomUI is installed.
            try
            {


                customUIExists = IPA.Loader.PluginManager.AllPlugins.FirstOrDefault(c => c.Metadata.Name == "Custom UI") != null;
                // If Custom UI is installed, create the UI
                if (customUIExists)
                    CustomUI.Utilities.BSEvents.menuSceneLoadedFresh += MenuLoadedFresh;
                // Called to set the WebClient SongFeedReaders uses
                var userAgent = $"BeatSync/{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
                SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
                SongFeedReaders.WebUtils.WebClient.SetUserAgent(userAgent);

                // TODO: Need to make this better, use a LoggerFactory, have the readers only auto-get a logger if null?
                var readerLogger = new Logging.BeatSyncFeedReaderLogger(SongFeedReaders.Logging.LoggingController.DefaultLogController);
                SongFeedReaders.BeastSaberReader.Logger = readerLogger;
                SongFeedReaders.BeatSaverReader.Logger = readerLogger;
                SongFeedReaders.ScoreSaberReader.Logger = readerLogger;
                SongFeedReaders.Utilities.Logger = readerLogger;
                SongFeedReaders.WebUtils.Logger = readerLogger;
                //SongFeedReaders.DataflowAlternative.TransformBlock.Logger = readerLogger;
            }
            catch (Exception ex)
            {
                Logger.log?.Error(ex);
            }

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
            Logger.log?.Debug($"OnActiveSceneChanged: {nextScene.name}");
            try
            {
                if (nextScene.name == "HealthWarning")
                {
                    BeatSync.Paused = false;
                    var beatSync = new GameObject().AddComponent<BeatSync>();
                    beatSyncCreated = true;
                    GameObject.DontDestroyOnLoad(beatSync);
                }
                if (!beatSyncCreated && nextScene.name == "MenuCore")
                {
                    BeatSync.Paused = false;
                    var beatSync = new GameObject().AddComponent<BeatSync>();
                    beatSyncCreated = true;
                    GameObject.DontDestroyOnLoad(beatSync);
                }
                if (nextScene.name == "GameCore")
                    BeatSync.Paused = true;
                else
                    BeatSync.Paused = false;
            }
            catch (Exception ex)
            {
                Logger.log?.Error(ex);
            }
        }

        /// <summary>
        /// Called when BSEvents.menuSceneLoadedFresh is triggered. UI creation is in here instead of
        /// OnSceneLoaded because some settings won't work otherwise.
        /// </summary>
        public void MenuLoadedFresh()
        {
            try
            {
                Logger.log?.Debug("Creating BeatSync's UI");
                UI.BeatSync_UI.CreateUI();
                config.Value.ResetConfigChanged();
                config.Value.FillDefaults();
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
            catch (Exception ex)
            {
                Logger.log?.Error(ex);
            }
        }

        private void SettingsMenu_didFinishEvent(SettingsFlowCoordinator sender, SettingsFlowCoordinator.FinishAction finishAction)
        {
            try
            {
                if (!config.Value.ConfigChanged && !config.Value.RegenerateConfig) // Don't skip if RegenerateConfig is true
                {
                    Logger.log?.Debug($"BeatSync settings not changed.");
                    return;
                }
                if (finishAction != SettingsFlowCoordinator.FinishAction.Cancel)
                {
                    Logger.log?.Debug("Saving settings.");
                    config.Value.RegenerateConfig = false;
                    configProvider.Store(config.Value);
                    config.Value.ResetConfigChanged();
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
