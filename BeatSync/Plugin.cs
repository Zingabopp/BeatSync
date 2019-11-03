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
using BeatSync.Utilities;
using IPALogger = IPA.Logging.Logger;
using System.Threading;

namespace BeatSync
{
    public class Plugin : IBeatSaberPlugin, IDisablablePlugin
    {
        // From SongCore
        internal static readonly string CachedHashDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"..\LocalLow\Hyperbolic Magnetism\Beat Saber\SongHashData.dat");
        internal const string PlaylistsPath = "Playlists";
        internal const string CustomLevelsPath = @"Beat Saber_Data\CustomLevels";
        internal const string UserDataPath = "UserData";

        internal static Ref<PluginConfig> config;
        internal static IConfigProvider configProvider;
        internal static UI.UIController StatusController;
        internal static BeatSync BeatSyncController;
        internal static FileLock CustomLevelsLock = new FileLock(CustomLevelsPath);
        internal static FileLock PlaylistsLock = new FileLock(PlaylistsPath);
        private static CancellationTokenSource CancelAllSource;

        private static bool customUIExists = false;
        //private bool beatSyncCreated = false;

        public void Init(IPALogger logger, [Config.Prefer("json")] IConfigProvider cfgProvider)
        {
            IPA.Logging.StandardLogger.PrintFilter = IPA.Logging.Logger.LogLevel.InfoUp;
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
                    v.Value.ResetFlags();
                }
                else
                {
                    //v.Value.RegenerateConfig = false;
                    v.Value.ResetConfigChanged();
                    v.Value.FillDefaults();
                    if (v.Value.ConfigChanged)
                    {
                        Logger.log?.Debug("Plugin.Init(): Saving settings.");
                        p.Store(v.Value);
                        v.Value.ResetFlags();
                    }
                }
                config = v;
                StatusController?.UpdateSettings();
            });

            var readerLogger = new Logging.BeatSyncFeedReaderLogger(SongFeedReaders.Logging.LoggingController.DefaultLogController);
            SongFeedReaders.Logging.LoggingController.DefaultLogger = readerLogger;
        }

        public void OnApplicationStart()
        {


        }


        public void OnEnable()
        {
            if (CancelAllSource != null)
            {
                try
                {
                    CancelAllSource.Cancel();
                    CancelAllSource.Dispose();
                }
                catch (Exception) { }
            }
            CancelAllSource = new CancellationTokenSource();
            var beatSyncVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Logger.log?.Debug($"BeatSync {beatSyncVersion} OnEnable");
            // Check if CustomUI is installed.
            try
            {
                if (!customUIExists)
                    customUIExists = IPA.Loader.PluginManager.AllPlugins.FirstOrDefault(c => c.Metadata.Name == "Custom UI") != null;
                // If Custom UI is installed, create the UI
                if (customUIExists)
                {
                    CustomUI.Utilities.BSEvents.menuSceneLoadedFresh -= MenuLoadedFresh;
                    CustomUI.Utilities.BSEvents.menuSceneLoadedFresh += MenuLoadedFresh;
                }
                // Called to set the WebClient SongFeedReaders uses
                if (!SongFeedReaders.WebUtils.IsInitialized)
                {
                    var userAgent = $"BeatSync/{beatSyncVersion}";
                    SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
                    SongFeedReaders.WebUtils.WebClient.SetUserAgent(userAgent);
                    SongFeedReaders.WebUtils.WebClient.Timeout = config.Value.DownloadTimeout * 1000;
                }
                BeatSync.Paused = false;
                BeatSyncController = new GameObject("BeatSync.BeatSync").AddComponent<BeatSync>();
                BeatSyncController.CancelAllToken = CancelAllSource.Token;
                StatusController = new GameObject("BeatSync.UIController").AddComponent<UI.UIController>();
                //beatSyncCreated = true;
                GameObject.DontDestroyOnLoad(BeatSyncController);
                GameObject.DontDestroyOnLoad(StatusController);
            }
            catch (Exception ex)
            {
                Logger.log?.Error(ex);
            }
        }

        public void OnDisable()
        {
            CancelAllSource.Cancel();
            CancelAllSource.Dispose();
            CancelAllSource = null;
            Logger.log?.Critical($"Disabling BeatSync...");
            SharedCoroutineStarter.instance.StartCoroutine(BeatSyncController.DestroyAfterFinishing());
            GameObject.Destroy(StatusController);
            BeatSyncController = null;
            StatusController = null;
#if DEBUG
            TestDestroyed();
#endif
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
                /*
                if (nextScene.name == "HealthWarning")
                {
                    BeatSync.Paused = false;
                    BeatSyncController = new GameObject("BeatSync.BeatSync").AddComponent<BeatSync>();
                    StatusController = new GameObject("BeatSync.UIController").AddComponent<UI.UIController>();
                    beatSyncCreated = true;
                    GameObject.DontDestroyOnLoad(BeatSyncController);
                    GameObject.DontDestroyOnLoad(StatusController);
                }
                if (!beatSyncCreated && nextScene.name == "MenuCore")
                {
                    BeatSync.Paused = false;
                    BeatSyncController = new GameObject("BeatSync.BeatSync").AddComponent<BeatSync>();
                    beatSyncCreated = true;
                    GameObject.DontDestroyOnLoad(BeatSyncController);
                }
                */
                if (nextScene.name == "GameCore")
                {
                    BeatSync.Paused = true;
                    if (StatusController != null && StatusController.isActiveAndEnabled)
                        StatusController?.gameObject.SetActive(false);
                }
                else
                {
                    BeatSync.Paused = false;
                    if (StatusController != null && !StatusController.isActiveAndEnabled)
                        StatusController?.gameObject.SetActive(true);
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error in Plugin.OnActiveSceneChanged:\n{ex}");
            }
        }

        /// <summary>
        /// Called when BSEvents.menuSceneLoadedFresh is triggered. UI creation is in here instead of
        /// OnSceneLoaded because some settings won't work otherwise.
        /// </summary>
        public static void MenuLoadedFresh()
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
                catch (Exception)
                {
                    Logger.log?.Critical("Could not find the SettingsFlowCoordinator. BeatSync settings will not be able to save.");
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Error(ex);
            }
        }

        private static void SettingsMenu_didFinishEvent(SettingsFlowCoordinator sender, SettingsFlowCoordinator.FinishAction finishAction)
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
                    config.Value.ResetFlags();
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
            if (Input.GetKeyDown(KeyCode.L))
            {
                GameObject.FindObjectsOfType<GameObject>()
                    .Where(g => g.name.Contains("BeatSync"))
                    .ToList()
                    .ForEach(g =>
                {
                    Logger.log?.Warn(g.name);
                });
                //StatusController?.gameObject.SetActive(false);
                //SharedCoroutineStarter.instance.StartCoroutine(TestDestroyed());
            }
        }

        public static IEnumerator<WaitForSeconds> TestDestroyed()
        {
            yield return new WaitForSeconds(1);
            var notDestroyed = Resources.FindObjectsOfTypeAll<UI.TextMeshList>();
            foreach (var item in notDestroyed)
            {
                if (item != null)
                    Logger.log?.Warn($"Not destroyed: {item.name}");
            }

            var otherNotDestroyed = Resources.FindObjectsOfTypeAll<UI.FloatingText>();
            foreach (var item in otherNotDestroyed)
            {
                if (item != null)
                    Logger.log?.Warn($"Not destroyed: {item.name}");
            }
            GameObject.Destroy(StatusController.gameObject);
            yield return new WaitForSeconds(1);
            notDestroyed = Resources.FindObjectsOfTypeAll<UI.TextMeshList>();
            if (notDestroyed.Length > 0 || notDestroyed.All(f => f == null))
                Logger.log?.Warn("All TextMeshLists destroyed");
            foreach (var item in notDestroyed)
            {
                if (item != null)
                    Logger.log?.Warn($"Not destroyed: {item.name}");
            }

            otherNotDestroyed = Resources.FindObjectsOfTypeAll<UI.FloatingText>();
            if (otherNotDestroyed.Length > 0 || otherNotDestroyed.All(f => f == null))
                Logger.log?.Warn("All FloatingTexts destroyed");
            foreach (var item in otherNotDestroyed)
            {
                if (item != null)
                    Logger.log?.Warn($"Not destroyed: {item.name}");
            }
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
