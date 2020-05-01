using Newtonsoft.Json;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeatSyncPlaylists.Legacy
{
    public class LegacyPlaylistSong : IFeedSong, IEquatable<IPlaylistSong>
    {
        public LegacyPlaylistSong()
        {
            _feedSources = new HashSet<string>();
        }

        public LegacyPlaylistSong(IPlaylistSong song)
            : this()
        {
            this.Populate(song);
        }
        public LegacyPlaylistSong(string hash, string? songName, string? songKey, string? mapper)
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

        public LegacyPlaylistSong(ISong song)
            : this()
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), $"song cannot be null for a new {nameof(LegacyPlaylistSong)}.");
            Hash = song.Hash ?? throw new ArgumentException("Hash cannot be null in the song.", nameof(song));
            Name = song.Name;
            Key = song.Key;
            LevelAuthorName = song.LevelAuthorName;
            DateAdded = DateTime.Now;
        }

        [JsonProperty("key", Order = -10)]
        public string? Key { get; set; }

        [JsonProperty("hash", Order = -9)]
        public string Hash
        {
            get { return _hash; }
            set
            {
                if(value == null)
                    throw new ArgumentException("Hash cannot be null in the song.", nameof(Hash));
                _hash = value.ToUpper();
            }
        }

        [JsonProperty("levelAuthorName")]
        public string? LevelAuthorName { get; set; }

        [JsonProperty("songName", Order = -8)]
        public string? Name { get; set; }

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
        public HashSet<string> FeedSources => new HashSet<string>(_feedSources);

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
