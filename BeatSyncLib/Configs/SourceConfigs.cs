using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSyncLib.Configs
{
    public class BeatSaverConfig : SourceConfigBase
    {
        [JsonIgnore]
        private BeatSaverFavoriteMappers? _favoriteMappers;
        [JsonIgnore]
        private BeatSaverHot? _hot;
        [JsonIgnore]
        private BeatSaverDownloads? _downloads;
        [JsonIgnore]
        private BeatSaverLatest? _latest;

        [JsonProperty(Order = -50)]
        public BeatSaverFavoriteMappers FavoriteMappers
        {
            get
            {
                if (_favoriteMappers == null)
                {
                    _favoriteMappers = new BeatSaverFavoriteMappers();
                    SetConfigChanged();
                }
                return _favoriteMappers;
            }
            set
            {
                if (_favoriteMappers == value)
                    return;
                _favoriteMappers = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -40)]
        public BeatSaverHot Hot
        {
            get
            {
                if (_hot == null)
                {
                    _hot = new BeatSaverHot();
                    SetConfigChanged();
                }
                return _hot;
            }
            set
            {
                if (_hot == value)
                    return;
                _hot = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -30)]
        public BeatSaverDownloads Downloads
        {
            get
            {
                if (_downloads == null)
                {
                    _downloads = new BeatSaverDownloads();
                    SetConfigChanged();
                }
                return _downloads;
            }
            set
            {
                if (_downloads == value)
                    return;
                _downloads = value;
                SetConfigChanged();
            }
        }

        [JsonProperty(Order = -20)]
        public BeatSaverLatest Latest
        {
            get
            {
                if (_latest == null)
                {
                    _latest = new BeatSaverLatest();
                    SetConfigChanged();
                }
                return _latest;
            }
            set
            {
                if (_latest == value)
                    return;
                _latest = value;
                SetConfigChanged();
            }
        }
        [JsonIgnore]
        protected override bool DefaultEnabled => true;

        [JsonIgnore]
        public override bool ConfigChanged
        {
            get
            {
                return (base.ConfigChanged || FavoriteMappers.ConfigChanged || Hot.ConfigChanged || Downloads.ConfigChanged);
            }
            protected set => base.ConfigChanged = value;
        }

        public override void ResetConfigChanged()
        {
            FavoriteMappers.ResetConfigChanged();
            Hot.ResetConfigChanged();
            Downloads.ResetConfigChanged();
            ConfigChanged = false;
        }

        public override void ResetFlags()
        {
            FavoriteMappers.ResetFlags();
            Hot.ResetFlags();
            Downloads.ResetFlags();
            base.ResetFlags();
        }

        public override void FillDefaults()
        {
            FavoriteMappers.FillDefaults();
            Hot.FillDefaults();
            Downloads.FillDefaults();
            Latest.FillDefaults();
            base.FillDefaults();
        }

        public override bool ConfigMatches(ConfigBase other)
        {
            if(other is BeatSaverConfig castOther)
            {
                if (!base.ConfigMatches(castOther))
                    return false;
                if (!FavoriteMappers.ConfigMatches(castOther.FavoriteMappers))
                    return false;
                if (!Hot.ConfigMatches(castOther.Hot))
                    return false;
                if (!Downloads.ConfigMatches(castOther.Downloads))
                    return false;
            }
            else
            {
                return false;
            }
            return true;
        }
    }

    public class BeastSaberConfig : SourceConfigBase
    {

        [JsonIgnore]
        protected override bool DefaultEnabled => true;

        [JsonIgnore]
        private BeastSaberBookmarks? _bookmarks;
        [JsonIgnore]
        private BeastSaberFollowings? _follows;
        [JsonIgnore]
        private BeastSaberCuratorRecommended? _curatorRecommended;
        [JsonIgnore]
        private string? _username;
        [JsonIgnore]
        private int? _maxConcurrentPageChecks;

        [JsonProperty(Order = -60)]
        public string Username
        {
            get
            {
                if (_username == null)
                {
                    _username = string.Empty;
                    SetConfigChanged();
                }
                return _username ?? string.Empty;
            }
            set
            {
                if (_username == value)
                    return;
                _username = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -50)]
        public int MaxConcurrentPageChecks
        {
            get
            {
                if (_maxConcurrentPageChecks == null)
                {
                    _maxConcurrentPageChecks = 5;
                    SetConfigChanged();
                }
                return _maxConcurrentPageChecks ?? 5;
            }
            set
            {
                int newAdjustedVal = Math.Min(10, value);
                if (value < 0)
                {
                    newAdjustedVal = 1;
                    SetInvalidInputFixed();
                }
                if (_maxConcurrentPageChecks == newAdjustedVal)
                    return;
                _maxConcurrentPageChecks = newAdjustedVal;                    
                SetConfigChanged();
            }
        }

        [JsonProperty(Order = -40)]
        public BeastSaberBookmarks Bookmarks
        {
            get
            {
                if (_bookmarks == null)
                {
                    _bookmarks = new BeastSaberBookmarks();
                    SetConfigChanged();
                }
                return _bookmarks;
            }
            set
            {
                if (_bookmarks == value)
                    return;
                _bookmarks = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -30)]
        public BeastSaberFollowings Follows
        {
            get
            {
                if (_follows == null)
                {
                    _follows = new BeastSaberFollowings();
                    SetConfigChanged();
                }
                return _follows;
            }
            set
            {
                if (_follows == value)
                    return;
                _follows = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -20)]
        public BeastSaberCuratorRecommended CuratorRecommended
        {
            get
            {
                if (_curatorRecommended == null)
                {
                    _curatorRecommended = new BeastSaberCuratorRecommended();
                    SetConfigChanged();
                }
                return _curatorRecommended;
            }
            set
            {
                if (_curatorRecommended == value)
                    return;
                _curatorRecommended = value;
                SetConfigChanged();
            }
        }

        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is BeastSaberConfig castOther)
            {
                if (!base.ConfigMatches(castOther))
                    return false;
                if (MaxConcurrentPageChecks != castOther.MaxConcurrentPageChecks)
                    return false;
                if (Username != castOther.Username)
                    return false;
                if (!Bookmarks.ConfigMatches(castOther.Bookmarks))
                    return false;
                if (!Follows.ConfigMatches(castOther.Follows))
                    return false;
                if (!CuratorRecommended.ConfigMatches(castOther.CuratorRecommended))
                    return false;
            }
            else
            {
                return false;
            }
            return true;
        }

        [JsonIgnore]
        public override bool ConfigChanged
        {
            get
            {
                return (base.ConfigChanged || Bookmarks.ConfigChanged || Follows.ConfigChanged || CuratorRecommended.ConfigChanged);
            }
            protected set => base.ConfigChanged = value;
        }

        public override void ResetConfigChanged()
        {
            Bookmarks.ResetConfigChanged();
            Follows.ResetConfigChanged();
            CuratorRecommended.ResetConfigChanged();
            base.ConfigChanged = false;
        }

        public override void ResetFlags()
        {
            Bookmarks.ResetFlags();
            Follows.ResetFlags();
            CuratorRecommended.ResetFlags();
            base.ResetFlags();
        }

        public override void FillDefaults()
        {
            Bookmarks.FillDefaults();
            Follows.FillDefaults();
            CuratorRecommended.FillDefaults();
            base.FillDefaults();
            var __ = MaxConcurrentPageChecks;
            var ___ = Username;
        }
    }

    public class ScoreSaberConfig : SourceConfigBase
    {
        [JsonIgnore]
        private ScoreSaberTopRanked? _topRanked;
        [JsonIgnore]
        private ScoreSaberLatestRanked? _latestRanked;
        [JsonIgnore]
        private ScoreSaberTrending? _trending;
        [JsonIgnore]
        private ScoreSaberTopPlayed? _topPlayed;

        [JsonProperty(Order = -60)]
        public ScoreSaberTopRanked TopRanked
        {
            get
            {
                if (_topRanked == null)
                {
                    _topRanked = new ScoreSaberTopRanked();
                    SetConfigChanged();
                }
                return _topRanked;
            }
            set
            {
                if (_topRanked == value)
                    return;
                _topRanked = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -50)]
        public ScoreSaberLatestRanked LatestRanked
        {
            get
            {
                if (_latestRanked == null)
                {
                    _latestRanked = new ScoreSaberLatestRanked();
                    SetConfigChanged();
                }
                return _latestRanked;
            }
            set
            {
                if (_latestRanked == value)
                    return;
                _latestRanked = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -40)]
        public ScoreSaberTrending Trending
        {
            get
            {
                if (_trending == null)
                {
                    _trending = new ScoreSaberTrending();
                    SetConfigChanged();
                }
                return _trending;
            }
            set
            {
                if (_trending == value)
                    return;
                _trending = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -30)]
        public ScoreSaberTopPlayed TopPlayed
        {
            get
            {
                if (_topPlayed == null)
                {
                    _topPlayed = new ScoreSaberTopPlayed();
                    SetConfigChanged();
                }
                return _topPlayed;
            }
            set
            {
                if (_topPlayed == value)
                    return;
                _topPlayed = value;
                SetConfigChanged();
            }
        }

        [JsonIgnore]
        protected override bool DefaultEnabled => false;

        [JsonIgnore]
        public override bool ConfigChanged
        {
            get
            {
                return (base.ConfigChanged || TopRanked.ConfigChanged || LatestRanked.ConfigChanged || Trending.ConfigChanged || TopPlayed.ConfigChanged);
            }
            protected set => base.ConfigChanged = value;
        }

        public override void ResetConfigChanged()
        {
            TopRanked.ResetConfigChanged();
            LatestRanked.ResetConfigChanged();
            Trending.ResetConfigChanged();
            TopPlayed.ResetConfigChanged();
            ConfigChanged = false;
        }

        public override void ResetFlags()
        {
            TopRanked.ResetFlags();
            LatestRanked.ResetFlags();
            Trending.ResetFlags();
            TopPlayed.ResetFlags();
            base.ResetFlags();
        }

        public override void FillDefaults()
        {
            TopRanked.FillDefaults();
            LatestRanked.FillDefaults();
            Trending.FillDefaults();
            TopPlayed.FillDefaults();
            base.FillDefaults();
        }

        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is ScoreSaberConfig castOther)
            {
                if (!base.ConfigMatches(castOther))
                    return false;
                if (!TopRanked.ConfigMatches(castOther.TopRanked))
                    return false;
                if (!LatestRanked.ConfigMatches(castOther.LatestRanked))
                    return false;
                if (!Trending.ConfigMatches(castOther.Trending))
                    return false;
                if (!TopPlayed.ConfigMatches(castOther.TopPlayed))
                    return false;
            }
            else
            {
                return false;
            }
            return true;
        }
    }
}
