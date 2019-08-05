using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Playlists
{
    public class PlaylistSong : IEquatable<PlaylistSong>
    {
        public PlaylistSong() { }
        public PlaylistSong(string hash, string songName, string songKey = "")
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentNullException(nameof(hash), "Hash cannot be null for a PlaylistSong.");
            Hash = hash;
            Name = songName;
            Key = songKey;
        }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("hash")]
        public string Hash
        {
            get { return _hash; }
            set
            {
                _hash = value?.ToUpper();
            }
        }

        [JsonProperty("songName")]
        public string Name { get; set; }

        [JsonIgnore]
        private string _hash;

        public bool Equals(PlaylistSong other)
        {
            if (other == null)
                return false;
            return Hash == other?.Hash;
        }
    }
}
