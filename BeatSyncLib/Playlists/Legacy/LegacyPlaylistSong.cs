using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeatSyncLib.Playlists.Legacy
{
    public class LegacyPlaylistSong : IFeedSong, IEquatable<IPlaylistSong>
    {
        public LegacyPlaylistSong()
        {
            _associatedPlaylists = new List<IPlaylist>();
            _feedSources = new HashSet<string>();
        }

        public LegacyPlaylistSong(IPlaylistSong song)
            : this()
        {
            this.Populate(song);
        }
        public LegacyPlaylistSong(string hash, string songName, string songKey, string mapper)
            : this()
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentNullException(nameof(hash), "Hash cannot be null for a PlaylistSong.");
            Hash = hash;
            Name = songName;
            Key = songKey;
            LevelAuthorName = mapper;
            DateAdded = DateTime.Now;
        }

        [JsonProperty("key", Order = -10)]
        public string Key { get; set; }

        [JsonProperty("hash", Order = -9)]
        public string Hash
        {
            get { return _hash; }
            set
            {
                _hash = value?.ToUpper();
            }
        }

        [JsonProperty("levelAuthorName")]
        public string LevelAuthorName { get; set; }

        [JsonProperty("songName", Order = -8)]
        public string Name { get; set; }

        [JsonProperty("dateAdded", Order = -7)]
        public DateTime? DateAdded { get; set; }
                [JsonProperty("feedSources", Order = -6)]
        protected HashSet<string> _feedSources { get; set; }
        [JsonIgnore]
        private object _feedSourceLock = new object();

        public bool TryAddFeedSource(string sourceName)
        {
            lock (_feedSourceLock)
            {
                if (!_feedSources.Contains(sourceName))
                {
                    _feedSources.Add(sourceName);
                    return true;
                }
            }
            return false;
        }

        [JsonIgnore]
        private string _directoryName;

        [JsonIgnore]
        public string DirectoryName
        {
            get
            {
                if (string.IsNullOrEmpty(_directoryName))
                {
                    _directoryName = Utilities.Util.GetSongDirectoryName(Key, Name, LevelAuthorName);
                }
                return _directoryName;
            }
        }

        [JsonIgnore]
        public IReadOnlyList<IPlaylist> AssociatedPlaylists { get { return _associatedPlaylists.AsReadOnly(); } }

        [JsonIgnore]
        public List<IPlaylist> _associatedPlaylists { get; }

        public HashSet<string> FeedSources => new HashSet<string>(_feedSources);

        /// <summary>
        /// Adds a playlist to this song's AssociatedPlaylists list.
        /// </summary>
        /// <param name="playlist"></param>
        /// <exception cref="ArgumentNullException">Thrown if the provided playlist is null.</exception>
        public void AddPlaylist(IPlaylist playlist)
        {
            if (playlist == null)
                throw new ArgumentNullException(nameof(playlist), "playlist cannot be null for PlaylistSong.AddPlaylist");
            if (string.IsNullOrEmpty(playlist.FilePath))
                throw new ArgumentException("playlist FileName cannot be null or empty for PlaylistSong.AddPlaylist");
            if (!_associatedPlaylists.Any(p => p.FilePath == playlist.FilePath))
                _associatedPlaylists.Add(playlist);
        }

        [JsonIgnore]
        private string _hash;

        public override string ToString()
        {
            var keyPart = string.IsNullOrEmpty(Key) ? string.Empty : $"({Key}) ";
            return $"{keyPart}{Name} by {LevelAuthorName}";
        }

        public bool Equals(IPlaylistSong other)
        {
            if (other == null)
                return false;
            return Hash == other?.Hash;
        }

        public void AddFeedSource(string sourceName)
        {
            if (!_feedSources.Contains(sourceName))
                _feedSources.Add(sourceName);
        }
    }
}
