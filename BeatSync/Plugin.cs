using BeatSyncLib.Configs;
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
using BeatSync.Configs;
using IPA.Config.Stores;
using Newtonsoft.Json;

namespace BeatSync
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        // From SongCore
        internal static readonly string CachedHashDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"..\LocalLow\Hyperbolic Magnetism\Beat Saber\SongHashData.dat");
        internal const string PlaylistsPath = "Playlists";
        internal const string CustomLevelsPath = @"Beat Saber_Data\CustomLevels";
        internal static readonly string UserDataPath = Path.Combine(UnityGame.UserDataPath);
        private static string _version;
        public static string PluginVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                    _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                return _version;
            }
        }
        internal static BeatSyncConfig config;
        internal static BeatSyncModConfig modConfig;
        internal static UI.UIController StatusController;
        internal static BeatSync BeatSyncController;
        internal static FileLock CustomLevelsLock = new FileLock(CustomLevelsPath);
        internal static FileLock PlaylistsLock = new FileLock(PlaylistsPath);
        private static CancellationTokenSource CancelAllSource;
        //private bool beatSyncCreated = false;

        [Init]
        public void Init(IPALogger logger)//, [Config.Prefer("json")] Config conf)
        {
            Logger.log = new BeatSyncIPALogger(logger);
            Logger.log?.Debug("Logger initialized.");
            //config = conf.Generated<BeatSyncConfig>();
            config = JsonConvert.DeserializeObject<BeatSyncConfig>(File.ReadAllText(Path.Combine(UserDataPath, "BeatSync.json")));
            var readerLogger = new Logging.BeatSyncFeedReaderLogger(SongFeedReaders.Logging.LoggingController.DefaultLogController);
            SongFeedReaders.Logging.LoggingController.DefaultLogger = readerLogger;
        }

        public void SetEvents(bool enabled)
        {
            if (enabled)
            {

                BS_Utils.Utilities.BSEvents.menuSceneLoadedFresh -= MenuLoadedFresh;
                BS_Utils.Utilities.BSEvents.menuSceneLoadedFresh += MenuLoadedFresh;
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
                SceneManager.activeSceneChanged += OnActiveSceneChanged;
            }
            else
            {
                BS_Utils.Utilities.BSEvents.menuSceneLoadedFresh -= MenuLoadedFresh;
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            }
            Logger.log?.Debug("Finished setting events.");
        }

        [OnEnable]
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
            Logger.log?.Debug($"BeatSync {PluginVersion} OnEnable");
            // Check if CustomUI is installed.
            try
            {
                SetEvents(true);
                // Called to set the WebClient SongFeedReaders uses
                if (!SongFeedReaders.WebUtils.IsInitialized)
                {
                    var userAgent = $"BeatSync/{PluginVersion}";
                    SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
                    SongFeedReaders.WebUtils.WebClient.SetUserAgent(userAgent);
                    SongFeedReaders.WebUtils.WebClient.Timeout = config.DownloadTimeout * 1000;
                    Logger.log?.Debug($"Initialized WebUtils with User-Agent: {userAgent}");
                }
                BeatSync.Paused = false;
                BeatSyncController = new GameObject("BeatSync.BeatSync").AddComponent<BeatSync>();
                BeatSyncController.CancelAllToken = CancelAllSource.Token;
                //StatusController = new GameObject("BeatSync.UIController").AddComponent<UI.UIController>();
                //beatSyncCreated = true;
                GameObject.DontDestroyOnLoad(BeatSyncController);
                GameObject.DontDestroyOnLoad(StatusController);
            }
            catch (Exception ex)
            {
                Logger.log?.Error(ex);
            }
        }

        [OnDisable]
        public void OnDisable()
        {
            CancelAllSource.Cancel();
            CancelAllSource.Dispose();
            CancelAllSource = null;
            Logger.log?.Critical($"Disabling BeatSync...");
            SetEvents(false);
            SharedCoroutineStarter.instance.StartCoroutine(BeatSyncController.DestroyAfterFinishing());
            GameObject.Destroy(StatusController);
            BeatSyncController = null;
            StatusController = null;
#if DEBUG
            TestDestroyed();
#endif
        }

        [OnExit]
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
                config.ResetConfigChanged();
                config.FillDefaults();
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

        /// <summary>
        /// Event handler for when the settings menu is exited (or 'Applied').
        /// If the button pressed isn't 'Cancel', save BeatSync's settings to json.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="finishAction"></param>
        private static void SettingsMenu_didFinishEvent(SettingsFlowCoordinator sender, SettingsFlowCoordinator.FinishAction finishAction)
        {
            try
            {
                if (!config.ConfigChanged && !config.RegenerateConfig) // Don't skip if RegenerateConfig is true
                {
                    return;
                }
                if (finishAction != SettingsFlowCoordinator.FinishAction.Cancel)
                {

                }
            }
            catch (Exception ex)
            {
                Logger.log?.Critical($"Error saving settings.\n{ex.Message}\n{ex.StackTrace}");
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
    }
}
