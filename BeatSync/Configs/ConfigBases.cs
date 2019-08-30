using Newtonsoft.Json;

namespace BeatSync.Configs
{
    public abstract class NotifyOnChange
    {
        [JsonIgnore]
        public virtual bool ConfigChanged { get; protected set; }

        public virtual void SetConfigChanged(bool changed = true)
        {
            ConfigChanged = changed;
        }

        public virtual void ResetConfigChanged()
        {
            if(ConfigChanged)
                SetConfigChanged(false);
        }

        /// <summary>
        /// Sets any properties with a null value to their default.
        /// </summary>
        public abstract void FillDefaults();
    }

    public abstract class SourceConfigBase
        : NotifyOnChange
    {
        [JsonIgnore]
        protected bool? _enabled;
        [JsonIgnore]
        protected abstract bool DefaultEnabled { get; }

        [JsonProperty(Order = -100)]
        public bool Enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = DefaultEnabled;
                    SetConfigChanged();
                }
                return _enabled ?? DefaultEnabled;
            }
            set
            {
                if (_enabled == value)
                    return;
                _enabled = value;
                SetConfigChanged();
            }
        }

        public abstract override void ResetConfigChanged();
        public override void FillDefaults()
        {
            var _ = Enabled;
        }
    }

    public abstract class FeedConfigBase
        : NotifyOnChange
    {

        public override void FillDefaults()
        {
            var _ = Enabled;
            var __ = MaxSongs;
            var ___ = CreatePlaylist;
            var ____ = PlaylistStyle;
            var _____ = FeedPlaylist;
        }

        [JsonIgnore]
        protected bool? _enabled;
        [JsonIgnore]
        protected int? _maxSongs;
        [JsonIgnore]
        protected bool? _createPlaylist;
        [JsonIgnore]
        protected PlaylistStyle? _playlistStyle;
        [JsonIgnore]
        protected Playlists.BuiltInPlaylist? _feedPlaylist;

        [JsonIgnore]
        protected abstract bool DefaultEnabled { get; }
        [JsonIgnore]
        protected abstract int DefaultMaxSongs { get; }
        [JsonIgnore]
        protected abstract bool DefaultCreatePlaylist { get; }
        [JsonIgnore]
        protected abstract PlaylistStyle DefaultPlaylistStyle { get; }
        [JsonIgnore]
        protected abstract Playlists.BuiltInPlaylist DefaultFeedPlaylist { get; }

        [JsonProperty(Order = -100)]
        public bool Enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = DefaultEnabled;
                    SetConfigChanged();
                }
                return _enabled ?? DefaultEnabled;
            }
            set
            {
                if (_enabled == value)
                    return;
                _enabled = value;
                SetConfigChanged();
            }
        }

        [JsonProperty(Order = -90)]
        public int MaxSongs
        {
            get
            {
                if (_maxSongs == null)
                {
                    _maxSongs = DefaultMaxSongs;
                    SetConfigChanged();
                }
                return _maxSongs ?? DefaultMaxSongs;
            }
            set
            {
                int newAdjustedVal = value;
                if (value < 0)
                {
                    newAdjustedVal = 0;
                    SetConfigChanged();
                }                    
                if (_maxSongs == newAdjustedVal)
                    return;
                _maxSongs = newAdjustedVal;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -80)]
        public bool CreatePlaylist
        {
            get
            {
                if (_createPlaylist == null)
                {
                    _createPlaylist = DefaultCreatePlaylist;
                    SetConfigChanged();
                }
                return _createPlaylist ?? DefaultCreatePlaylist;
            }
            set
            {
                if (_createPlaylist == value)
                    return;
                _createPlaylist = value;
                SetConfigChanged();
            }
        }
        [JsonProperty(Order = -70)]
        public PlaylistStyle PlaylistStyle
        {
            get
            {
                if (_playlistStyle == null)
                {
                    _playlistStyle = DefaultPlaylistStyle;
                    SetConfigChanged();
                }
                return _playlistStyle ?? DefaultPlaylistStyle;
            }
            set
            {
                if (_playlistStyle == value)
                    return;
                _playlistStyle = value;
                SetConfigChanged();
            }
        }
        [JsonIgnore]
        public Playlists.BuiltInPlaylist FeedPlaylist
        {
            get
            {
                if (_feedPlaylist == null)
                {
                    _feedPlaylist = DefaultFeedPlaylist;
                    SetConfigChanged();
                }
                return _feedPlaylist ?? DefaultFeedPlaylist;
            }
            set
            {
                if (_feedPlaylist == value)
                    return;
                _feedPlaylist = value;
                SetConfigChanged();
            }
        }

        public abstract SongFeedReaders.IFeedSettings ToFeedSettings();
    }

    public enum PlaylistStyle
    {
        /// <summary>
        /// Songs are added to the existing playlist.
        /// </summary>
        Append,
        /// <summary>
        /// Old playlist is ignored, replaced by the session's scrape.
        /// </summary>
        Replace
    }

    /// <summary>
    /// Decide which songs get added to the BeatSync playlists.
    /// </summary>
    public enum PlaylistFilter
    {
        /// <summary>
        /// All songs read from the feed are added to the feed playlist.
        /// </summary>
        KeepAll,
        /// <summary>
        /// Only songs that are downloaded are added to the feed playlist.
        /// </summary>
        DownloadedOnly
    }
}
