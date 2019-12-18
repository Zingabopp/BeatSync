using BeatSync.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using static BeatSync.Utilities.Util;
using static IPA.Utilities.Utils;

namespace BeatSync.Playlists
{
    [Serializable]
    public class Playlist
    {
        [JsonIgnore]
        public bool IsDirty { get; private set; }

        public Blister.Types.Playlist BlisterPlaylist;
        public Playlist() { }
        public Playlist(string playlistFileName, string playlistTitle, string playlistAuthor, string image)
        {
            FileName = playlistFileName;
            Title = playlistTitle;
            Author = playlistAuthor;
            Cover = Convert.FromBase64String(image);
            Beatmaps = new List<Blister.Types.Beatmap>();
            IsDirty = true;
        }

        public Playlist(string playlistFileName, string playlistTitle, string playlistAuthor, Lazy<string> imageLoader)
        {
            FileName = playlistFileName;
            Title = playlistTitle;
            Author = playlistAuthor;
            //ImageLoader = imageLoader;
            Cover = Convert.FromBase64String(imageLoader.Value);
            Beatmaps = new List<Blister.Types.Beatmap>();
            IsDirty = true;
        }

        /// <summary>
        /// Adds a PlaylistSong to the list if a song with the same hash doesn't already exist.
        /// </summary>
        /// <param name="song"></param>
        /// <returns>True if the song was added.</returns>
        public bool TryAdd(Blister.Types.Beatmap song)
        {
            string songHash = ByteArrayToString(song.Hash);
            if (!Beatmaps.Any(s => ByteArrayToString(s.Hash).Equals(songHash, StringComparison.OrdinalIgnoreCase)))
            {
                //Logger.log.Info($"Adding song with hash {songHash}");
                Beatmaps.Add(song);
                IsDirty = true;
                return true;
            }
            //Logger.log?.Warn($"Unable to add duplicate song hash {songHash}");
            return false;
        }

        public bool TryRemove(string songHash)
        {
            songHash = songHash.ToUpper();
            if (Beatmaps.Any(s => ByteArrayToString(s.Hash).Equals(songHash, StringComparison.OrdinalIgnoreCase)))
            {
                int songsRemoved = Beatmaps.RemoveAll(s => ByteArrayToString(s.Hash).Equals(songHash, StringComparison.OrdinalIgnoreCase));
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
            songKey = ParseKey(songKey);
            uint? keyInt = null;
            if(!string.IsNullOrEmpty(songKey))
                keyInt = Convert.ToUInt32(songKey, 16);
            return TryAdd(new Blister.Types.Beatmap 
            { Hash = StringToByteArray(songHash),  DateAdded = DateTime.Now, Type = Blister.Types.BeatmapType.Hash, Key = keyInt});
        }

        public bool TryAdd(PlaylistSong song)
        {
            var songKey = ParseKey(song.Key);
            uint? keyInt = null;
            if (!string.IsNullOrEmpty(songKey))
                keyInt = Convert.ToUInt32(songKey, 16);
            return TryAdd(new Blister.Types.Beatmap
            { Hash = StringToByteArray(song.Hash), DateAdded = song.DateAdded ?? DateTime.Now, Type = Blister.Types.BeatmapType.Hash, Key = keyInt });
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
            var count = Beatmaps.Count;
            Beatmaps = Beatmaps.Distinct().ToList();
            if (count != Beatmaps.Count)
            {
                Logger.log?.Warn($"Duplicate songs detected in playlist {Title}.");
                IsDirty = true;
            }
        }

        /// <summary>
        /// Removes all songs from the playlist.
        /// </summary>
        public void Clear()
        {
            if (Beatmaps.Count == 0)
                return;
            Beatmaps.Clear();
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
            if (Beatmaps.Count == 0)
            {
                exception = new InvalidOperationException($"Playlist {Title} has no songs.");
                return false;
            }
            //Logger.log?.Error($"Writing {FileName} to file.");
            exception = null;
            try
            {
                Beatmaps = Beatmaps.OrderByDescending(s => s.DateAdded).ToList();
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
        public string Title
        {
            get { return BlisterPlaylist.Title; }
            set
            {
                if (BlisterPlaylist.Title == value) return;
                BlisterPlaylist.Title = value;
                IsDirty = true;
            }
        }
        [JsonProperty("playlistAuthor", Order = -5)]
        public string Author
        {
            get { return BlisterPlaylist.Author; }
            set
            {
                if (BlisterPlaylist.Author == value) return;
                BlisterPlaylist.Author = value;
                IsDirty = true;
            }
        }

        public string Description
        {
            get { return BlisterPlaylist.Description; }
            set
            {
                if (BlisterPlaylist.Description == value) return;
                BlisterPlaylist.Description = value;
                IsDirty = true;
            }
        }

        [JsonProperty("image", Order = 10)]
        public byte[] Cover
        {
            get { return BlisterPlaylist.Cover; }
            set
            {
                BlisterPlaylist.Cover = value;
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

        public bool AddSong(Blister.Types.Beatmap beatmap)
        {
            if (Beatmaps.Any(b => b.Hash == beatmap.Hash))
                return false;
            Beatmaps.Add(beatmap);
            IsDirty = true;
            return true;
        }

        public int RemoveAll(Predicate<Blister.Types.Beatmap> match)
        {
            int numRemoved = Beatmaps.RemoveAll(match);
            if (numRemoved > 0)
                IsDirty = true;
            return numRemoved;
        }

        public int Count => Beatmaps.Count;

        [JsonIgnore]
        public List<Blister.Types.Beatmap> Beatmaps
        {
            get 
            {
                if (BlisterPlaylist.Maps == null)
                    BlisterPlaylist.Maps = new List<Blister.Types.Beatmap>();
                return BlisterPlaylist.Maps; 
            }
            set { BlisterPlaylist.Maps = value; }
        }

    }
}
