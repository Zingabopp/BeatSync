using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BeatSyncLib.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSyncConsole.Configs
{
    public class Config : ConfigBase
    {
        #region Private Fields
        [JsonIgnore]
        private List<BeatSaberInstallLocation>? _beatSaberInstallLocations;
        [JsonIgnore]
        private List<CustomSongLocation>? _customSongsPaths;
        public static Config GetDefaultConfig()
        {
            Config config = new Config();
            config.FillDefaults();
            return config;
        }
        #endregion
        #region Public Properties
        [JsonProperty(nameof(BeatSaberInstallLocations), Order = 0)]
        public List<BeatSaberInstallLocation> BeatSaberInstallLocations
        {
            get
            {
                if (_beatSaberInstallLocations == null)
                {
                    _beatSaberInstallLocations = new List<BeatSaberInstallLocation>() { BeatSaberInstallLocation.CreateEmptyLocation() };
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

        [JsonProperty(nameof(CustomSongsPaths), Order = 10)]
        public List<CustomSongLocation> CustomSongsPaths
        {
            get
            {
                if(_customSongsPaths == null)
                {
                    _customSongsPaths = new List<CustomSongLocation>() { CustomSongLocation.CreateEmptyLocation() };
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
        public BeatSyncConfig? BeatSyncConfig { get; set; }

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
            if (BeatSaberInstallLocations.Count == 0)
                BeatSaberInstallLocations.Add(BeatSaberInstallLocation.CreateEmptyLocation());
            if (CustomSongsPaths.Count == 0)
                CustomSongsPaths.Add(CustomSongLocation.CreateEmptyLocation());
            if (BeatSyncConfig == null)
                BeatSyncConfig = new BeatSyncConfig(true);
            else
                BeatSyncConfig.FillDefaults();
        }


    }
}
