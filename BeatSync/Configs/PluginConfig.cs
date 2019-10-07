using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
// Option to delete songs downloaded from certain feeds after x amount of days?
// public bool DeleteOldVersions { get; set; } not yet supported
// public bool DeleteDuplicateSongs { get; set; }
namespace BeatSync.Configs
{
    public class PluginConfig
        : ConfigBase
    {
        public static PluginConfig DefaultConfig = new PluginConfig().SetDefaults();

        public PluginConfig() { }
        public PluginConfig(bool fillDefaults)
            : this()
        {
            if (fillDefaults)
            {
                FillDefaults();
            }
        }
        #region Private Fields
        [JsonIgnore]
        private bool? _regenerateConfig;
        [JsonIgnore]
        private int? _downloadTimeout;
        [JsonIgnore]
        private int? _maxConcurrentDownloads;
        [JsonIgnore]
        private int? _recentPlaylistDays;
        [JsonIgnore]
        private bool? _allBeatSyncSongsPlaylist;
        [JsonIgnore]
        private StatusUiConfig _statusUi;
        [JsonIgnore]
        private SyncIntervalConfig _timeBetweenSyncs;
        [JsonIgnore]
        private BeastSaberConfig _beastSaber;
        [JsonIgnore]
        private BeatSaverConfig _beatSaver;
        [JsonIgnore]
        private ScoreSaberConfig _scoreSaber;
        [JsonIgnore]
        private DateTime? _lastRun;

        #endregion
        #region Public Properties
        [JsonProperty(Order = -100)]
        public bool RegenerateConfig
        {
            get
            {
                if (_regenerateConfig == null)
                {
                    _regenerateConfig = false;
                    SetConfigChanged();
                }
                return _regenerateConfig ?? false;
            }
            set
            {
                if (_regenerateConfig == value)
                    return;
                _regenerateConfig = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -90)]
        public int DownloadTimeout
        {
            get
            {
                if (_downloadTimeout == null)
                {
                    _downloadTimeout = 30;
                    SetConfigChanged();
                }
                return _downloadTimeout ?? 30;
            }
            set
            {
                int newAdjustedVal = value;
                if (value < 1)
                    newAdjustedVal = 30;
                if (value != newAdjustedVal)
                    SetInvalidInputFixed();
                if (_downloadTimeout == newAdjustedVal)
                    return;
                _downloadTimeout = newAdjustedVal;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -80)]
        public int MaxConcurrentDownloads
        {
            get
            {
                if (_maxConcurrentDownloads == null)
                {
                    _maxConcurrentDownloads = 3;
                    SetConfigChanged();
                }
                return _maxConcurrentDownloads ?? 3;
            }
            set
            {
                int newAdjustedVal = value;
                if (value < 1)
                    newAdjustedVal = 1;
                else if (value > 10)
                    newAdjustedVal = 10;
                if (value != newAdjustedVal)
                    SetInvalidInputFixed();
                if (_maxConcurrentDownloads == newAdjustedVal)
                    return;
                _maxConcurrentDownloads = newAdjustedVal;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -70)]
        public int RecentPlaylistDays
        {
            get
            {
                if (_recentPlaylistDays == null)
                {
                    _recentPlaylistDays = 7;
                    SetConfigChanged();
                }
                return _recentPlaylistDays ?? 7;
            }
            set
            {
                int newAdjustedVal = value;
                if (value < 0)
                    newAdjustedVal = 0;
                if (value != newAdjustedVal)
                    SetInvalidInputFixed();
                if (_recentPlaylistDays == newAdjustedVal)
                    return;
                _recentPlaylistDays = newAdjustedVal;
                SetConfigChanged();
            }
        } // Remember to change SyncSaberService to add date to playlist entry

        [JsonProperty(Order = -65)]
        public bool AllBeatSyncSongsPlaylist
        {
            get
            {
                if (_allBeatSyncSongsPlaylist == null)
                {
                    _allBeatSyncSongsPlaylist = false;
                    SetConfigChanged();
                }
                return _allBeatSyncSongsPlaylist ?? false;
            }
            set
            {
                if (_allBeatSyncSongsPlaylist == value)
                    return;
                _allBeatSyncSongsPlaylist = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -63)]
        public StatusUiConfig StatusUI
        {
            get
            {
                if (_statusUi == null)
                {
                    _statusUi = new StatusUiConfig();
                    SetConfigChanged();
                }
                return _statusUi;
            }
            set
            {
                if (_statusUi == value)
                    return;
                if (_statusUi.ConfigMatches(value))
                {
                    _statusUi = value;
                    return;
                }
                _statusUi = value;
                SetConfigChanged();
            }
        }

        [JsonProperty(Order = -61)]
        public SyncIntervalConfig TimeBetweenSyncs
        {
            get
            {
                if (_timeBetweenSyncs == null)
                {
                    _timeBetweenSyncs = new SyncIntervalConfig();
                    SetConfigChanged();
                }
                return _timeBetweenSyncs;
            }
            set
            {
                if (_timeBetweenSyncs == value)
                    return;
                if (_timeBetweenSyncs.ConfigMatches(value))
                {
                    _timeBetweenSyncs = value;
                    return;
                }
                _timeBetweenSyncs = value;
                SetConfigChanged();
            }
        }

        [JsonProperty(Order = -60)]
        public BeastSaberConfig BeastSaber
        {
            get
            {
                if (_beastSaber == null)
                {
                    _beastSaber = new BeastSaberConfig();
                    SetConfigChanged();
                }
                return _beastSaber;
            }
            set
            {
                if (_beastSaber == value)
                    return;
                _beastSaber = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -50)]
        public BeatSaverConfig BeatSaver
        {
            get
            {
                if (_beatSaver == null)
                {
                    _beatSaver = new BeatSaverConfig();
                    SetConfigChanged();
                }
                return _beatSaver;
            }
            set
            {
                if (_beatSaver == value)
                    return;
                _beatSaver = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -40)]
        public ScoreSaberConfig ScoreSaber
        {
            get
            {
                if (_scoreSaber == null)
                {
                    _scoreSaber = new ScoreSaberConfig();
                    SetConfigChanged();
                }
                return _scoreSaber;
            }
            set
            {
                if (_scoreSaber == value)
                    return;
                _scoreSaber = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = 100)]
        public DateTime LastRun
        {
            get
            {
                if (_lastRun == null || _lastRun > DateTime.Now)
                {
                    _lastRun = DateTime.MinValue;
                    SetConfigChanged();
                }
                return _lastRun ?? DateTime.MinValue;
            }
            set
            {
                if (_lastRun == value)
                    return;
                _lastRun = value;
                SetConfigChanged();
            }
        }
        #endregion

        [JsonIgnore]
        public override bool ConfigChanged
        {
            get
            {
                //var reasons = base.ChangedValues.Concat(BeatSaver.ChangedValues).Concat(BeastSaber.ChangedValues).Concat(ScoreSaber.ChangedValues);
                //Logger.log?.Info($"ChangedValues: {string.Join(", ", reasons)}");
                return (base.ConfigChanged 
                    || StatusUI.ConfigChanged 
                    || TimeBetweenSyncs.ConfigChanged 
                    || BeatSaver.ConfigChanged 
                    || BeastSaber.ConfigChanged 
                    || ScoreSaber.ConfigChanged);
            }
            protected set => base.ConfigChanged = value;
        }

        public override void ResetConfigChanged()
        {
            StatusUI.ResetConfigChanged();
            TimeBetweenSyncs.ResetConfigChanged();
            BeastSaber.ResetConfigChanged();
            BeatSaver.ResetConfigChanged();
            ScoreSaber.ResetConfigChanged();
            base.ResetConfigChanged();
        }

        public override void ResetFlags()
        {
            StatusUI.ResetFlags();
            TimeBetweenSyncs.ResetFlags();
            BeastSaber.ResetFlags();
            BeatSaver.ResetFlags();
            ScoreSaber.ResetFlags();
            base.ResetFlags();
        }

        public override void FillDefaults()
        {
            var _ = RegenerateConfig;
            var __ = DownloadTimeout;
            var ___ = MaxConcurrentDownloads;
            var ____ = AllBeatSyncSongsPlaylist;
            var _____ = RecentPlaylistDays;
            var ______ = LastRun;
            StatusUI.FillDefaults();
            TimeBetweenSyncs.FillDefaults();
            BeatSaver.FillDefaults();
            BeastSaber.FillDefaults();
            ScoreSaber.FillDefaults();
        }

        public PluginConfig Clone()
        {
            return JsonConvert.DeserializeObject<PluginConfig>(JsonConvert.SerializeObject(this));
        }

        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is PluginConfig castOther)
            {
                if (DownloadTimeout != castOther.DownloadTimeout)
                    return false;
                if (MaxConcurrentDownloads != castOther.MaxConcurrentDownloads)
                    return false;
                if (RecentPlaylistDays != castOther.RecentPlaylistDays)
                    return false;
                if (!StatusUI.ConfigMatches(castOther.StatusUI))
                    return false;
                if (!TimeBetweenSyncs.ConfigMatches(castOther.TimeBetweenSyncs))
                    return false;
                if (AllBeatSyncSongsPlaylist != castOther.AllBeatSyncSongsPlaylist)
                    return false;
                if (!BeastSaber.ConfigMatches(castOther.BeastSaber))
                    return false;
                if (!BeatSaver.ConfigMatches(castOther.BeatSaver))
                    return false;
                if (!ScoreSaber.ConfigMatches(castOther.ScoreSaber))
                    return false;
            }
            else
                return false;
            return true;
        }

        public PluginConfig SetDefaults()
        {
            FillDefaults();
            //RegenerateConfig = false;
            //DownloadTimeout = 30;
            //MaxConcurrentDownloads = 3;
            //AllBeatSyncSongsPlaylist = false;
            //RecentPlaylistDays = 7;

            //BeatSaver = new BeatSaverConfig()
            //{
            //    Enabled = false,
            //    MaxConcurrentPageChecks = 5,
            //    Hot = new BeatSaverHot() { Enabled = false, MaxSongs = 10, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append },
            //    Downloads = new BeatSaverDownloads() { Enabled = false, MaxSongs = 20, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append },
            //    // , SeparateMapperPlaylists = false
            //    FavoriteMappers = new BeatSaverFavoriteMappers() { Enabled = true, MaxSongs = 0, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append }
            //};

            //BeastSaber = new BeastSaberConfig()
            //{
            //    Enabled = true,
            //    MaxConcurrentPageChecks = 5,
            //    Username = "",
            //    Bookmarks = new BeastSaberBookmarks() { Enabled = true, MaxSongs = 0, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append },
            //    Follows = new BeastSaberFollowings() { Enabled = true, MaxSongs = 20, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append },
            //    CuratorRecommended = new BeastSaberCuratorRecommended() { Enabled = false, MaxSongs = 20, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append }
            //};

            //ScoreSaber = new ScoreSaberConfig()
            //{
            //    Enabled = false,
            //    TopRanked = new ScoreSaberTopRanked() { Enabled = false, MaxSongs = 20, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Replace },
            //    LatestRanked = new ScoreSaberLatestRanked() { Enabled = true, MaxSongs = 20, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Replace },
            //    Trending = new ScoreSaberTrending() { Enabled = true, MaxSongs = 20, RankedOnly = false, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append },
            //    TopPlayed = new ScoreSaberTopPlayed() { Enabled = false, MaxSongs = 20, RankedOnly = false, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append }
            //};
            return this;
        }

        //[OnDeserialized]
        //internal void OnDeserialized(StreamingContext context)
        //{
        //    //ResetConfigChanged();
        //}

    }

   
}
