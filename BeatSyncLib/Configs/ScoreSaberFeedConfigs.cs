using BeatSyncLib.Playlists;
using Newtonsoft.Json;
using SongFeedReaders.Feeds;
using SongFeedReaders.Feeds.ScoreSaber;

namespace BeatSyncLib.Configs
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class ScoreSaberFeedConfigBase : PagedFeedConfigBase
    {
        #region Defaults
        public virtual bool DefaultIncludeUnstarred => true;
        public virtual float DefaultMaxStars => 0;
        public virtual float DefaultMinStars => 0;
        #endregion
        [JsonIgnore]
        private bool? _includeUnstarred;
        [JsonIgnore]
        private float? _maxStars;
        [JsonIgnore]
        private float? _minStars;

        [JsonProperty(nameof(IncludeUnstarred), Order = 100)]
        public bool IncludeUnstarred
        {
            get
            {
                if (_includeUnstarred == null)
                {
                    _includeUnstarred = DefaultIncludeUnstarred;
                    SetConfigChanged();
                }
                return _includeUnstarred ?? DefaultIncludeUnstarred;
            }
            set
            {
                if (_includeUnstarred == value)
                    return;
                _includeUnstarred = value;
                SetConfigChanged();
            }
        }

        [JsonProperty(nameof(MaxStars), Order = 110)]
        public float MaxStars
        {
            get
            {
                if (_maxStars == null)
                {
                    _maxStars = DefaultMaxStars;
                    SetConfigChanged();
                }
                return _maxStars ?? DefaultMaxStars;
            }
            set
            {
                if (value < 0)
                    value = 0;
                if (_maxStars == value)
                    return;
                _maxStars = value;
                SetConfigChanged();
            }
        }

        [JsonProperty(nameof(MinStars), Order = 120)]
        public float MinStars
        {
            get
            {
                if (_minStars == null)
                {
                    _minStars = DefaultMinStars;
                    SetConfigChanged();
                }
                return _minStars ?? DefaultMinStars;
            }
            set
            {
                if (value < 0)
                    value = 0;
                if (_minStars == value)
                    return;
                _minStars = value;
                SetConfigChanged();
            }
        }

        public override void FillDefaults()
        {
            base.FillDefaults();
            _ = IncludeUnstarred;
            _ = MaxStars;
            _ = MinStars;
        }

        public override IFeedSettings ToFeedSettings()
        {
            ScoreSaberFeedSettings feedSettings = GetSettings();
            feedSettings.StartingPage = StartingPage;
            feedSettings.MaxSongs = MaxSongs;
            if (!IncludeUnstarred || MinStars > 0 || MaxStars > 0)
            {
                feedSettings.Filter = s =>
                {
                    float stars = s.JsonData?.Value<float>("stars") ?? 0;
                    if (stars == 0)
                        return IncludeUnstarred;
                    return stars > MinStars && (stars < MaxStars || MaxStars == 0);
                };
                feedSettings.StoreRawData = true;
            }
            return feedSettings;
        }

        protected abstract ScoreSaberFeedSettings GetSettings();

    }

    public class ScoreSaberTrending : ScoreSaberFeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => false;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.ScoreSaberTrending;

        protected bool DefaultRankedOnly => false;
        #endregion
        [JsonIgnore]
        private bool? _rankedOnly;
        [JsonProperty(nameof(RankedOnly), Order = 10)]
        public bool RankedOnly
        {
            get
            {
                if (_rankedOnly == null)
                {
                    _rankedOnly = DefaultRankedOnly;
                    SetConfigChanged();
                }
                return _rankedOnly ?? DefaultRankedOnly;
            }
            set
            {
                if (_rankedOnly == value)
                    return;
                _rankedOnly = value;
                SetConfigChanged();
            }
        }

        public override void FillDefaults()
        {
            base.FillDefaults();
            bool _ = RankedOnly;
        }

        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is ScoreSaberTrending castOther)
            {
                if (!base.ConfigMatches(castOther))
                    return false;
                if (RankedOnly != castOther.RankedOnly)
                    return false;
            }
            else
                return false;
            return true;
        }

        protected override ScoreSaberFeedSettings GetSettings()
            => new ScoreSaberTrendingSettings() { RankedOnly = RankedOnly };
    }

    public class ScoreSaberLatestRanked : ScoreSaberFeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => true;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Replace;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.ScoreSaberLatestRanked;
        #endregion


        protected override ScoreSaberFeedSettings GetSettings()
            => new ScoreSaberLatestSettings() { RankedOnly = true };
    }

    public class ScoreSaberTopPlayed : ScoreSaberFeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => false;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.ScoreSaberTopPlayed;

        protected bool DefaultRankedOnly => false;
        #endregion
        [JsonIgnore]
        private bool? _rankedOnly;

        [JsonProperty(nameof(RankedOnly), Order = 10)]
        public bool RankedOnly
        {
            get
            {
                if (_rankedOnly == null)
                {
                    _rankedOnly = DefaultRankedOnly;
                    SetConfigChanged();
                }
                return _rankedOnly ?? DefaultRankedOnly;
            }
            set
            {
                if (_rankedOnly == value)
                    return;
                _rankedOnly = value;
                SetConfigChanged();
            }
        }

        public override void FillDefaults()
        {
            base.FillDefaults();
            bool _ = RankedOnly;
        }

        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is ScoreSaberTopPlayed castOther)
            {
                if (!base.ConfigMatches(castOther))
                    return false;
                if (RankedOnly != castOther.RankedOnly)
                    return false;
            }
            else
                return false;
            return true;
        }

        protected override ScoreSaberFeedSettings GetSettings()
            => new ScoreSaberTopPlayedSettings() { RankedOnly = RankedOnly };
    }

    public class ScoreSaberTopRanked : ScoreSaberFeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => true;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Replace;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.ScoreSaberTopRanked;
        #endregion

        protected override ScoreSaberFeedSettings GetSettings()
            => new ScoreSaberTopRankedSettings() { RankedOnly = true };
    }
}
