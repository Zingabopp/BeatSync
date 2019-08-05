using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync
{
    public class PlaylistSong
    {
        public PlaylistSong() { }
        public PlaylistSong(string _hash, string _songIndex, string _songName)
        {
            if (string.IsNullOrEmpty(_hash))
                throw new ArgumentNullException(nameof(_hash), "Hash cannot be null for a PlaylistSong.");
            Hash = _hash;
            Key = _songIndex;
            Name = _songName;
        }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("songName")]
        public string Name { get; set; }
    }
}
