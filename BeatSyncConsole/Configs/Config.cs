using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BeatSyncConsole.Configs.Converters;
using BeatSyncLib.Configs;
using SongFeedReaders.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSyncConsole.Configs
{
    public class Config : ConfigBase
    {
        #region Public Properties
        [JsonProperty(nameof(BeatSyncConfigPath), Order = 10)]
        public string BeatSyncConfigPath
        {
            get
            {
                if (_beatSyncConfigPath == null || _beatSyncConfigPath.Length == 0)
                    return DefaultBeatSyncConfigPath;
                return _beatSyncConfigPath;
            }
            set
            {
                if (_beatSyncConfigPath == value)
                    return;
                _beatSyncConfigPath = value;
                SetConfigChanged();
            }
        }

        [JsonProperty(nameof(BeatSaberInstallLocations), Order = 10)]
        public List<BeatSaberInstallLocation> BeatSaberInstallLocations
        {
            get
            {
                if (_beatSaberInstallLocations == null)
                {
                    _beatSaberInstallLocations = new List<BeatSaberInstallLocation>();
                    SetConfigChanged();
                }
                return _beatSaberInstallLocations;
            }
            set
            {
                if (_beatSaberInstallLocations == value)
                    return;
                _beatSaberInstallLocations = value;
                SetConfigChanged();
            }
        }

        [JsonProperty("CustomSongsPaths")]
        private List<CustomSongLocation> legacySongLocations
        {
            set
            {
                AlternateSongsPaths = value;
                legacyValueChanged = true;
                SetConfigChanged();
            }
        }

        [JsonProperty(nameof(AlternateSongsPaths), Order = 20)]
        public List<CustomSongLocation> AlternateSongsPaths
        {
            get
            {
                if (_customSongsPaths == null)
                {
                    _customSongsPaths = new List<CustomSongLocation>();
                    SetConfigChanged();
                }
                return _customSongsPaths;
            }
            set
            {
                if (_customSongsPaths == value)
                    return;
                _customSongsPaths = value;
                SetConfigChanged();
            }
        }

        [JsonProperty(nameof(CloseWhenFinished), Order = 30)]
        public bool CloseWhenFinished
        {
            get
            {
                if(_closeWhenFinished == null)
                {
                    _closeWhenFinished = false;
                    SetConfigChanged();
                }
                return _closeWhenFinished ?? false;
            }
            set
            {
                if (_closeWhenFinished == value)
                    return;
                _closeWhenFinished = value;
                SetConfigChanged();
            }
        }

        [JsonProperty("UseSystemTemp", Order = 35)]
        public bool UseSystemTemp {
            get
            {
                if (_useSystemTemp == null)
                {
                    _useSystemTemp = false;
                    SetConfigChanged();
                }
                return _useSystemTemp ?? false;
            }
            set
            {
                if (_useSystemTemp == value)
                    return;
                _useSystemTemp = value;
                SetConfigChanged();
            }
        }

        [JsonProperty(nameof(ConsoleLogLevel), Order = 100)]
        [JsonConverter(typeof(LogLevelConverter))]
        public LogLevel ConsoleLogLevel
        {

            get
            {
                if (_consoleLogLevel == null)
                {
                    _consoleLogLevel = LogLevel.Info;
                    SetConfigChanged();
                }
                return _consoleLogLevel ?? LogLevel.Info;
            }
            set
            {
                if (_consoleLogLevel == value)
                    return;
                _consoleLogLevel = value;
                SetConfigChanged();
            }
        }

        [JsonIgnore]
        public BeatSyncConfig? BeatSyncConfig
        {
            get => _beatSyncConfig;
            set => _beatSyncConfig = value;
        }

        #endregion

        public static Config GetDefaultConfig()
        {
            Config config = new Config();
            config.FillDefaults();
            return config;
        }

        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is Config config)
            {
                if (AlternateSongsPaths.Except(config.AlternateSongsPaths).Any() 
                    || config.AlternateSongsPaths.Except(AlternateSongsPaths).Any()
                    || config.CloseWhenFinished != CloseWhenFinished)
                    return false;
                return true;
            }
            else
                return false;
        }

        public override void FillDefaults()
        {
            if (string.IsNullOrEmpty(BeatSyncConfigPath))
                BeatSyncConfigPath = Path.Combine("%CONFIG%", "BeatSync.json");
            if (BeatSaberInstallLocations.Count == 0)
                BeatSaberInstallLocations.Add(BeatSaberInstallLocation.CreateEmptyLocation());
            if (AlternateSongsPaths.Count == 0)
                AlternateSongsPaths.Add(CustomSongLocation.CreateEmptyLocation());
            if (BeatSyncConfig != null)
                BeatSyncConfig.FillDefaults();
            _ = CloseWhenFinished;
        }


        private static readonly string DefaultBeatSyncConfigPath = Path.Combine("%CONFIG%", "BeatSync.json");
        #region Private Fields
        [JsonIgnore]
        private BeatSyncConfig? _beatSyncConfig;
        [JsonIgnore]
        private List<BeatSaberInstallLocation>? _beatSaberInstallLocations;
        [JsonIgnore]
        private List<CustomSongLocation>? _customSongsPaths;
        [JsonIgnore]
        private string? _beatSyncConfigPath;
        [JsonIgnore]
        internal bool legacyValueChanged = false;
        private LogLevel? _consoleLogLevel;
        private bool? _useSystemTemp;
        private bool? _closeWhenFinished;

        #endregion
    }
}
