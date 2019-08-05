using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BeatSync;

namespace BeatSync.Playlists
{
    [Serializable]
    public class Playlist
    {
        public Playlist() { }
        public Playlist(string playlistFileName, string playlistTitle, string playlistAuthor, string image)
        {
            FileName = playlistFileName;
            Title = playlistTitle;
            Author = playlistAuthor;
            Image = image;
            Songs = new List<PlaylistSong>();
        }

        public bool TryAdd(PlaylistSong song)
        {
            if (!Songs.Contains(song))
            {
                Songs.Add(song);
                return true;
            }
            return false;
        }

        public bool TryAdd(string songHash, string songName, string songKey = "")
        {
            if (!Songs.Exists(s => !string.IsNullOrEmpty(s.Hash) && s.Hash.ToUpper() == songHash.ToUpper()))
            {
                Songs.Add(new PlaylistSong(songHash, songName, songKey));
                return true;
                // Remove any duplicate song that doesn't have a hash
            }
            return false;
        }

        public void RemoveDuplicates()
        {
            //var oldSongs = Songs.Where(s => string.IsNullOrEmpty(s.Hash) && !string.IsNullOrEmpty(s.Key) && s.Key.ToLower() == songKey.ToLower()).ToArray();
            //foreach (var song in oldSongs)
            //{
            //    Songs.Remove(song);
            //}
            Songs = Songs.Distinct().ToList();
        }

        public void RemoveInvalidSongs()
        {
            var oldSongs = Songs.Where(s => string.IsNullOrEmpty(s.Hash));
            foreach (var song in oldSongs)
            {
                Songs.Remove(song);
            }
        }

        [JsonProperty("playlistTitle")]
        public string Title { get; set; }
        [JsonProperty("playlistAuthor")]
        public string Author { get; set; }
        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("songs")]
        public List<PlaylistSong> Songs
        {
            get
            {
                if (_songs == null)
                    _songs = new List<PlaylistSong>();
                return _songs;
            }
            set { _songs = value; }
        }

        [JsonIgnore]
        private string _fileName;
        [JsonIgnore]
        public string FileName
        {
            get { return _fileName; }
            set
            {
                //if (string.IsNullOrEmpty(value))
                //{
                //    _fileName = value;
                //    return;
                //}
                if (value.Contains("\\"))
                    _fileName = value.Substring(value.LastIndexOf('\\') + 1);
                else
                    _fileName = value;
            }
        }
        [JsonIgnore]
        private List<PlaylistSong> _songs;

    }
}
