using BeatSyncLib.Playlists;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.ScoreSaber;

namespace BeatSyncLib.Configs
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class ScoreSaberFeedConfigBase : FeedConfigBase
    {
        public abstract ScoreSaberFeedName ScoreSaberFeed { get; }
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
            ScoreSaberFeedSettings feedSettings = new ScoreSaberFeedSettings(ScoreSaberFeed);
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

    }

    public class ScoreSaberTrending : ScoreSaberFeedConfigBase
    {
        public override ScoreSaberFeedName ScoreSaberFeed => ScoreSaberFeedName.Trending;
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
            var _ = RankedOnly;
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

        public override IFeedSettings ToFeedSettings()
        {
            ScoreSaberFeedSettings baseSettings = base.ToFeedSettings() as ScoreSaberFeedSettings ?? new ScoreSaberFeedSettings((int)ScoreSaberFeed);
            baseSettings.MaxSongs = this.MaxSongs;
            baseSettings.RankedOnly = this.RankedOnly;
            return baseSettings;
        }
    }

    public class ScoreSaberLatestRanked : ScoreSaberFeedConfigBase
    {
        public override ScoreSaberFeedName ScoreSaberFeed => ScoreSaberFeedName.LatestRanked;
        #region Defaults
        protected override bool DefaultEnabled => true;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Replace;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.ScoreSaberLatestRanked;
        #endregion

        public override IFeedSettings ToFeedSettings()
        {
            ScoreSaberFeedSettings baseSettings = base.ToFeedSettings() as ScoreSaberFeedSettings ?? new ScoreSaberFeedSettings((int)ScoreSaberFeed);
            baseSettings.MaxSongs = this.MaxSongs;
            baseSettings.RankedOnly = true;
            return baseSettings;
        }
    }

    public class ScoreSaberTopPlayed : ScoreSaberFeedConfigBase
    {
        public override ScoreSaberFeedName ScoreSaberFeed => ScoreSaberFeedName.TopPlayed;
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
            var _ = RankedOnly;
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

        public override IFeedSettings ToFeedSettings()
        {

            ScoreSaberFeedSettings baseSettings = base.ToFeedSettings() as ScoreSaberFeedSettings ?? new ScoreSaberFeedSettings((int)ScoreSaberFeed);
            baseSettings.MaxSongs = this.MaxSongs;
            baseSettings.RankedOnly = this.RankedOnly;
            return baseSettings;
        }
    }

    public class ScoreSaberTopRanked : ScoreSaberFeedConfigBase
    {
        public override ScoreSaberFeedName ScoreSaberFeed => ScoreSaberFeedName.TopRanked;
        #region Defaults
        protected override bool DefaultEnabled => true;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Replace;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.ScoreSaberTopRanked;
        #endregion

        public override IFeedSettings ToFeedSettings()
        {

            ScoreSaberFeedSettings baseSettings = base.ToFeedSettings() as ScoreSaberFeedSettings ?? new ScoreSaberFeedSettings((int)ScoreSaberFeed);
            baseSettings.MaxSongs = this.MaxSongs;
            baseSettings.RankedOnly = true;
            return baseSettings;
        }
    }
}
