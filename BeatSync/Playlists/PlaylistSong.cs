using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BeatSync.Playlists
{
    public class PlaylistSong : IEquatable<PlaylistSong>
    {
        public PlaylistSong()
        {
            _associatedPlaylists = new List<Playlist>();
        }
        public PlaylistSong(string hash, string songName, string songKey, string mapper)
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

        [JsonIgnore]
        private string _directoryName;

        [JsonIgnore]
        public string DirectoryName
        {
            get
            {
                if (string.IsNullOrEmpty(_directoryName))
                {
                    // BeatSaverDownloader's method of naming the directory.
                    string basePath = Key + " (" + Name + " - " + LevelAuthorName + ")";
                    basePath = string.Join("", basePath.Trim().Split((Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray())));
                    _directoryName = basePath;
                }
                return _directoryName;
            }
        }

        [JsonIgnore]
        public IReadOnlyList<Playlist> AssociatedPlaylists { get { return _associatedPlaylists.AsReadOnly(); } }

        [JsonIgnore]
        public List<Playlist> _associatedPlaylists { get; }

        /// <summary>
        /// Adds a playlist to this song's AssociatedPlaylists list.
        /// </summary>
        /// <param name="playlist"></param>
        /// <exception cref="ArgumentNullException">Thrown if the provided playlist is null.</exception>
        public void AddPlaylist(Playlist playlist)
        {
            if (playlist == null)
                throw new ArgumentNullException(nameof(playlist), "playlist cannot be null for PlaylistSong.AddPlaylist");
            if (string.IsNullOrEmpty(playlist.FileName))
                throw new ArgumentException("playlist FileName cannot be null or empty for PlaylistSong.AddPlaylist");
            if (!_associatedPlaylists.Any(p => p.FileName == playlist.FileName))
                _associatedPlaylists.Add(playlist);
        }

        [JsonIgnore]
        private string _hash;

        public override string ToString()
        {
            return $"({Key}) {Name} by {LevelAuthorName}";
        }

        public bool Equals(PlaylistSong other)
        {
            if (other == null)
                return false;
            return Hash == other?.Hash;
        }
    }
}
