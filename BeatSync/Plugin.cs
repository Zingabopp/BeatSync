using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IPA;
using IPA.Config;
using IPA.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace BeatSync
{
    public class Plugin : IBeatSaberPlugin
    {
        internal static Ref<PluginConfig> config;
        internal static IConfigProvider configProvider;
        private bool customUIExists = false;

        #region Setting Properties
        public static bool ExampleBoolSetting
        {
            get { return config.Value.ExampleBoolSetting; }
            set
            {
                config.Value.ExampleBoolSetting = value;
                configProvider.Store(config.Value);
            }
        }

        public static int ExampleIntSetting
        {
            get { return config.Value.ExampleIntSetting; }
            set
            {
                config.Value.ExampleIntSetting = value;
                configProvider.Store(config.Value);
            }
        }
        public static Color ExampleColorSetting
        {
            get { return config.Value.ExampleColorSetting.ToColor(); }
            set
            {
                // ExampleColorSetting is stored as a float array in the json config file.
                config.Value.ExampleColorSetting = value.ToFloatAry();
                configProvider.Store(config.Value);
            }
        }
        public static int ExampleTextSegment
        {
            get { return config.Value.ExampleTextSegment; }
            set
            {
                config.Value.ExampleTextSegment = value;
                configProvider.Store(config.Value);
            }
        }
        public static string ExampleStringSetting
        {
            get { return config.Value.ExampleStringSetting; }
            set
            {
                config.Value.ExampleStringSetting = value;
                configProvider.Store(config.Value);

            }
        }
        public static float ExampleSliderSetting
        {
            get { return config.Value.ExampleSliderSetting; }
            set
            {
                config.Value.ExampleSliderSetting = value;
                configProvider.Store(config.Value);
            }
        }
        public static float ExampleListSetting
        {
            get { return config.Value.ExampleListSetting; }
            set
            {
                config.Value.ExampleListSetting = value;
                configProvider.Store(config.Value);
            }
        }

        private static float _exampleGameplayListSetting = 0;
        public static float ExampleGameplayListSetting
        {
            get { return _exampleGameplayListSetting; }
            set
            {
                _exampleGameplayListSetting = value;
            }
        }
        #endregion

        public void Init(IPALogger logger, [Config.Prefer("json")] IConfigProvider cfgProvider)
        {
            IPA.Logging.StandardLogger.PrintFilter = IPA.Logging.Logger.LogLevel.All;
            Logger.log = logger;
            Logger.log.Debug("Logger initialied.");

            configProvider = cfgProvider;

            config = configProvider.MakeLink<PluginConfig>((p, v) =>
            {
                // Build new config file if it doesn't exist or RegenerateConfig is true
                if (v.Value == null || v.Value.RegenerateConfig)
                {
                    Logger.log.Debug("Regenerating PluginConfig");
                    p.Store(v.Value = new PluginConfig()
                    {
                        // Set your default settings here.
                        RegenerateConfig = false,
                        ExampleBoolSetting = false,
                        ExampleIntSetting = 5,
                        ExampleColorSetting = UnityEngine.Color.blue.ToFloatAry(),
                        ExampleTextSegment = 0,
                        ExampleStringSetting = "example",
                        ExampleSliderSetting = 2,
                        ExampleListSetting = 3f
                    });
                }
                config = v;
            });
        }

        public void OnApplicationStart()
        {
            Logger.log.Debug("OnApplicationStart");
            // Check if CustomUI is installed.
            customUIExists = IPA.Loader.PluginManager.AllPlugins.FirstOrDefault(c => c.Metadata.Name == "Custom UI") != null;
            // If Custom UI is installed, create the UI
            if (customUIExists)
                CustomUI.Utilities.BSEvents.menuSceneLoadedFresh += MenuLoadedFresh;

        }

        public void OnApplicationQuit()
        {
            Logger.log.Debug("OnApplicationQuit");
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
        /// Called when the active scene is changed.
        /// </summary>
        /// <param name="prevScene">The scene you are transitioning from.</param>
        /// <param name="nextScene">The scene you are transitioning to.</param>
        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {

        }

        /// <summary>
        /// Called when BSEvents.menuSceneLoadedFresh is triggered. UI creation is in here instead of
        /// OnSceneLoaded because some settings won't work otherwise.
        /// </summary>
        public void MenuLoadedFresh()
        {
            {
                Logger.log.Debug("Creating plugin's UI");
                UI.BeatSync_UI.CreateUI();
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
