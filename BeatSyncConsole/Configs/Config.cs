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
        private List<SongLocation> _customSongsPaths;
        [JsonIgnore]
        private BeatSyncConfig _beatSyncConfig;
        #endregion
        #region Public Properties
        public List<SongLocation> CustomSongsPaths
        {
            get
            {
                if(_customSongsPaths == null)
                {
                    _customSongsPaths = new List<SongLocation>();
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

        public BeatSyncConfig BeatSyncConfig
        {
            get
            {
                if (_beatSyncConfig == null)
                {
                    _beatSyncConfig = new BeatSyncConfig();
                    SetConfigChanged();
                }
                return _beatSyncConfig;
            }
            set
            {
                if (_beatSyncConfig == value)
                    return;
                _beatSyncConfig = value;
                SetConfigChanged();
            }
        }

        #endregion
        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is Config config)
            {
                if (CustomSongsPaths.Except(config.CustomSongsPaths).Any() || config.CustomSongsPaths.Except(CustomSongsPaths).Any())
                    return false;
                if (!BeatSyncConfig.ConfigMatches(config.BeatSyncConfig))
                    return false;
                return true;
            }
            else
                return false;
        }

        public override void FillDefaults()
        {
            _ = CustomSongsPaths;
            BeatSyncConfig.FillDefaults();
        }


    }
}
