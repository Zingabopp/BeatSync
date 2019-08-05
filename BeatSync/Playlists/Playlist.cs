using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSync
{
    [Serializable]
    public class Playlist
    {

        public Playlist(string playlistFileName, string playlistTitle, string playlistAuthor, string image)
        {
            fileName = playlistFileName;
            if (fileName.ToLower().EndsWith(".json"))
                oldFormat = true;
            Title = playlistTitle;
            Author = playlistAuthor;
            Image = image;
            Songs = new List<PlaylistSong>();
        }

        public void TryAdd(string songHash, string songKey, string songName)
        {
            if (!Songs.Exists(s => !string.IsNullOrEmpty(s.Hash) && s.Hash.ToUpper() == songHash.ToUpper()))
            {
                Songs.Add(new PlaylistSong(songHash, songKey, songName));
                // Remove any duplicate song that doesn't have a hash
                var oldSongs = Songs.Where(s => string.IsNullOrEmpty(s.Hash) && !string.IsNullOrEmpty(s.Key) && s.Key.ToLower() == songKey.ToLower()).ToArray();
                foreach (var song in oldSongs)
                {
                    Songs.Remove(song);
                }
            }
        }

        [JsonProperty("playlistTitle")]
        public string Title;
        [JsonProperty("playlistAuthor")]
        public string Author;

        [JsonProperty("image")]
        public string Image;

        [JsonProperty("songs")]
        public List<PlaylistSong> Songs;

        [JsonIgnore]
        public string fileName;
        [JsonIgnore]
        public bool oldFormat = false;
    }
}
