using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BeatSyncLib.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSyncConsole.Configs
{
    public class Config : ConfigBase
    {
        private static readonly string DefaultBeatSyncConfigPath = Path.Combine("%CONFIG%", "BeatSync.json");
        #region Private Fields
        [JsonIgnore]
        private BeatSyncConfig? _beatSyncConfig;
        [JsonIgnore]
        private List<BeatSaberInstallLocation>? _beatSaberInstallLocations;
        [JsonIgnore]
        private List<CustomSongLocation>? _customSongsPaths;
        private string? _beatSyncConfigPath;

        public static Config GetDefaultConfig()
        {
            Config config = new Config();
            config.FillDefaults();
            return config;
        }
        #endregion
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

        [JsonProperty(nameof(CustomSongsPaths), Order = 20)]
        public List<CustomSongLocation> CustomSongsPaths
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
        [JsonIgnore]
        public BeatSyncConfig BeatSyncConfig
        {
            get => _beatSyncConfig ??= new BeatSyncConfig(true);
            set => _beatSyncConfig = value;
        }

        #endregion
        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is Config config)
            {
                if (CustomSongsPaths.Except(config.CustomSongsPaths).Any() || config.CustomSongsPaths.Except(CustomSongsPaths).Any())
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
            if (CustomSongsPaths.Count == 0)
                CustomSongsPaths.Add(CustomSongLocation.CreateEmptyLocation());
            BeatSyncConfig.FillDefaults();
        }


    }
}
