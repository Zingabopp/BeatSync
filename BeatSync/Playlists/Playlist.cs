using BeatSync.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BeatSync.Playlists
{
    [Serializable]
    public class Playlist
    {
        [JsonIgnore]
        public bool IsDirty { get; private set; }
        public Playlist() { }
        public Playlist(string playlistFileName, string playlistTitle, string playlistAuthor, string image)
        {
            FileName = playlistFileName;
            Title = playlistTitle;
            Author = playlistAuthor;
            Image = image;
            Songs = new List<PlaylistSong>();
            IsDirty = true;
        }

        /// <summary>
        /// Adds a PlaylistSong to the list if a song with the same hash doesn't already exist.
        /// </summary>
        /// <param name="song"></param>
        /// <returns>True if the song was added.</returns>
        public bool TryAdd(PlaylistSong song)
        {
            if (!Songs.Any(s => s.Hash.Equals(song.Hash)))
            {
                Songs.Add(song);
                IsDirty = true;
                return true;
            }
            return false;
        }

        public bool TryRemove(string songHash)
        {
            songHash = songHash.ToUpper();
            if (Songs.Any(s => songHash.Equals(s.Hash)))
            {
                int songsRemoved = Songs.RemoveAll(s => songHash.Equals(s.Hash));
                IsDirty = true;
                return true;
            }
            return false;
        }

        public bool TryRemove(PlaylistSong song)
        {
            return TryRemove(song.Hash);
        }

        /// <summary>
        /// Adds a new PlaylistSong to the list if a song with the same hash doesn't already exist.
        /// </summary>
        /// <param name="songHash"></param>
        /// <param name="songName"></param>
        /// <param name="songKey"></param>
        /// <returns>True if the song was added.</returns>
        public bool TryAdd(string songHash, string songName, string songKey, string mapper)
        {
            return TryAdd(new PlaylistSong(songHash, songName, songKey, mapper));
        }

        /// <summary>
        /// Removes songs with the same hash from the Songs list.
        /// </summary>
        public void RemoveDuplicates()
        {
            //var oldSongs = Songs.Where(s => string.IsNullOrEmpty(s.Hash) && !string.IsNullOrEmpty(s.Key) && s.Key.ToLower() == songKey.ToLower()).ToArray();
            //foreach (var song in oldSongs)
            //{
            //    Songs.Remove(song);
            //}
            var count = Songs.Count;
            Songs = Songs.Distinct().ToList();
            if (count != Songs.Count)
            {
                Logger.log?.Warn($"Duplicate songs detected in playlist {Title}.");
                IsDirty = true;
            }
        }

        /// <summary>
        /// Removes songs that don't have a Hash from the playlist.
        /// </summary>
        public void RemoveInvalidSongs()
        {
            var oldSongs = Songs.Where(s => string.IsNullOrEmpty(s.Hash));
            foreach (var song in oldSongs)
            {
                Songs.Remove(song);
                IsDirty = true;
            }
        }

        /// <summary>
        /// Removes all songs from the playlist.
        /// </summary>
        public void Clear()
        {
            if (Songs.Count == 0)
                return;
            Songs.Clear();
            IsDirty = true;
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            IsDirty = false;
        }

        /// <summary>
        /// Tries to write the playlist to a file. Before the file is written, the songs are ordered by DateAdded in descending order.
        /// If the write fails, returns false and provides the exception in the out parameter.
        /// </summary>
        /// <param name="exception">The exception that is thrown if there's an error.</param>
        /// <returns></returns>
        public bool TryWriteFile(out Exception exception)
        {
            if (Songs.Count == 0)
            {
                exception = new InvalidOperationException($"Playlist {Title} has no songs.");
                return false;
            }
            //Logger.log?.Error($"Writing {FileName} to file.");
            exception = null;
            try
            {
                Songs = Songs.OrderByDescending(s => s.DateAdded).ToList();
                FileIO.WritePlaylist(this);
                IsDirty = false;
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        /// <summary>
        /// Tries to write the Playlist to a file. Returns false if it was unsuccessful.
        /// </summary>
        /// <returns></returns>
        public bool TryWriteFile()
        {
            var retVal = TryWriteFile(out var ex);
            if (ex != null)
                Logger.log?.Error($"Error writing playlist {FileName}: {ex.Message}");
            return retVal;
        }

        [JsonProperty("playlistTitle", Order = -10)]
        public string Title { get; set; }
        [JsonProperty("playlistAuthor", Order = -5)]
        public string Author { get; set; }
        [JsonProperty("image", Order = 10)]
        public string Image { get; set; }

        [JsonProperty("songs")]
        public List<PlaylistSong> Songs
        {
            get
            {
                if (_songs == null)
                {
                    _songs = new List<PlaylistSong>();
                    IsDirty = true;
                }
                return _songs;
            }
            set
            {
                if (_songs == value)
                    return;
                _songs = value;
                IsDirty = true;
            }
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
