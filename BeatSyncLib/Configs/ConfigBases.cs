using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using SongFeedReaders.Readers;
using BeatSyncLib.Playlists;
using BeatSyncLib.Configs.Converters;
using System.ComponentModel;

namespace BeatSyncLib.Configs
{
    public abstract class ConfigBase
    {
        [JsonIgnore]
        private List<string> _changedValues = new List<string>();
        [JsonIgnore]
        public string[] ChangedValues { get { return _changedValues.ToArray(); } }
        [JsonIgnore]
        private List<string> _fixedInvalidInputs = new List<string>();
        [JsonIgnore]
        public string[] InvalidInputs { get { return _fixedInvalidInputs.ToArray(); } }
        [JsonIgnore]
        private bool _configChanged;
        [JsonIgnore]
        public virtual bool ConfigChanged
        {
            get { return _configChanged || InvalidInputFixed == true; }
            protected set
            {
                _configChanged = value;
            }
        }
        [JsonIgnore]
        public virtual bool InvalidInputFixed { get; protected set; }
        public virtual void SetConfigChanged(bool changed = true, [CallerMemberName] string member = "")
        {
            if (!string.IsNullOrEmpty(member))
            {
                //Logger.log?.Info($"Setting ConfigChanged in {this.GetType().ToString()} due to {member}");
                var fullName = $"{this.GetType()}:{member}";
                if (!_changedValues.Contains(fullName))
                    _changedValues.Add(fullName);
            }
            ConfigChanged = changed;
        }

        public virtual void SetInvalidInputFixed(bool invalidFixed = true, [CallerMemberName] string member = "")
        {
            if (!string.IsNullOrEmpty(member))
            {
                //Logger.log?.Info($"Setting ConfigChanged in {this.GetType().ToString()} due to {member}");
                _fixedInvalidInputs.Add($"{this.GetType()}:{member}");
            }
            InvalidInputFixed = invalidFixed;
        }

        public virtual void ResetConfigChanged()
        {
            _changedValues.Clear();
            if (ConfigChanged)
                SetConfigChanged(false, "");
        }

        public virtual void ResetInvalidInputsFixed()
        {
            _fixedInvalidInputs.Clear();
            if (InvalidInputFixed)
                SetInvalidInputFixed(false, "");
        }

        public virtual void ResetFlags()
        {
            ResetConfigChanged();
            ResetInvalidInputsFixed();
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            ResetConfigChanged();
        }

        /// <summary>
        /// Sets any properties with a null value to their default.
        /// </summary>
        public abstract void FillDefaults();

        public abstract bool ConfigMatches(ConfigBase other);
    }

    public abstract class SourceConfigBase
        : ConfigBase
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

        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is SourceConfigBase castOther)
            {
                if (Enabled != castOther.Enabled)
                    return false;
            }
            else
                return false;
            return true;
        }
    }

    public abstract class FeedConfigBase
        : ConfigBase
    {

        public override void FillDefaults()
        {
            var _ = Enabled;
            var __ = StartingPage;
            var ___ = MaxSongs;
            var ____ = CreatePlaylist;
            var _____ = PlaylistStyle;
            var ______ = FeedPlaylist;
        }

        [JsonIgnore]
        private bool? _enabled;
        [JsonIgnore]
        private int? _startingPage;
        [JsonIgnore]
        private int? _maxSongs;
        [JsonIgnore]
        private bool? _createPlaylist;
        [JsonIgnore]
        private PlaylistStyle? _playlistStyle;
        [JsonIgnore]
        private BuiltInPlaylist? _feedPlaylist;

        [JsonIgnore]
        protected abstract bool DefaultEnabled { get; }
        [JsonIgnore]
        protected int DefaultStartingPage { get; set; } = 1;
        [JsonIgnore]
        protected abstract int DefaultMaxSongs { get; }
        [JsonIgnore]
        protected abstract bool DefaultCreatePlaylist { get; }
        [JsonIgnore]
        protected abstract PlaylistStyle DefaultPlaylistStyle { get; }
        [JsonIgnore]
        protected abstract BuiltInPlaylist DefaultFeedPlaylist { get; }

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

        [JsonProperty(Order = -95, NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue(1)]
        public int StartingPage
        {
            get
            {
                return _startingPage ?? DefaultStartingPage;
            }
            set
            {
                int newAdjustedVal = value;
                if (value < 1)
                {
                    newAdjustedVal = DefaultStartingPage;
                    SetInvalidInputFixed();
                }
                if (_startingPage == newAdjustedVal)
                    return;
                _startingPage = newAdjustedVal;
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
                    newAdjustedVal = DefaultMaxSongs;
                    SetInvalidInputFixed();
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
        [JsonConverter(typeof(PlaylistStyleConverter))]
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
        public BuiltInPlaylist FeedPlaylist
        {
            get
            {
                if (_feedPlaylist == null)
                {
                    _feedPlaylist = DefaultFeedPlaylist;
                    //SetConfigChanged();
                }
                return _feedPlaylist ?? DefaultFeedPlaylist;
            }
            set
            {
                if (_feedPlaylist == value)
                    return;
                _feedPlaylist = value;
                //SetConfigChanged();
            }
        }

        [JsonIgnore]
        public virtual bool StoreRawData { get; protected set; } = false;
        public abstract IFeedSettings ToFeedSettings();

        public override bool ConfigMatches(ConfigBase other)
        {
            if (other is FeedConfigBase castOther)
            {
                if (Enabled != castOther.Enabled)
                    return false;
                if (MaxSongs != castOther.MaxSongs)
                    return false;
                if (CreatePlaylist != castOther.CreatePlaylist)
                    return false;
                if (PlaylistStyle != castOther.PlaylistStyle)
                    return false;
            }
            else
                return false;
            return true;
        }
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
