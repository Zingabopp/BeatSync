using BeatSyncLib.Utilities;
using Newtonsoft.Json;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BeatSyncLib.Playlists.Legacy
{
    [Serializable]
    public class LegacyPlaylist : IPlaylist<LegacyPlaylistSong>
    {
        public LegacyPlaylist() { }
        public LegacyPlaylist(string filePath, string title, string author, string cover)
        {
            FilePath = filePath;
            Title = title;
            Author = author;
            SetCover(cover);
            Beatmaps = new List<LegacyPlaylistSong>();
        }
        public LegacyPlaylist(string filePath, string title, string author, Lazy<string> coverLoader)
        {
            FilePath = filePath;
            Title = title;
            Author = author;
            CoverLoader = coverLoader;
            Beatmaps = new List<LegacyPlaylistSong>();
        }
        public static readonly string[] FileExtensions = new string[] { "bplist" };
        #region Private Fields
        [NonSerialized]
        private string _title;
        [NonSerialized]
        private string _author;
        [NonSerialized]
        private string _description;
        [NonSerialized]
        private string _fileName;
        [NonSerialized]
        private byte[] _coverImage;
        [NonSerialized]
        private List<LegacyPlaylistSong> _songs;
        [NonSerialized]
        private Lazy<string> CoverLoader;
        #endregion

        #region Serializable Properties
        [JsonProperty("playlistTitle", Order = -10)]
        public string Title
        {
            get { return _title; }
            set
            {
                if (_title == value)
                    return;
                _title = value;
                MarkDirty();
            }
        }

        [JsonProperty("playlistAuthor", Order = -5)]
        public string Author
        {
            get { return _author; }
            set
            {
                if (_author == value)
                    return;
                _author = value;
                MarkDirty();
            }
        }
        [JsonProperty("playlistDescription", Order = 0, NullValueHandling = NullValueHandling.Ignore)]
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description == value)
                    return;
                _description = value;
                MarkDirty();
            }
        }

        [JsonProperty("songs", Order = 5)]
        protected List<LegacyPlaylistSong> Beatmaps
        {
            get { return _songs; }
            set { _songs = value; }
        }

        [JsonProperty("image", Order = 10)]
        protected string CoverStr
        {
            get => Utilities.Util.ByteArrayToBase64(Cover);

        }
        [JsonIgnore]
        protected byte[] Cover
        {
            get
            {
                if (_coverImage == null && CoverLoader != null)
                {
                    SetCover(CoverLoader.Value);
                    CoverLoader = null;
                }
                return _coverImage;
            }
            set
            {
                if (_coverImage == value)
                    return;
                _coverImage = value;
                MarkDirty();
            }
        }

        #endregion
        [JsonIgnore]
        public string FilePath
        {
            get { return _fileName; }
            set
            {
                if (_fileName == value)
                    return;
                _fileName = value;
                MarkDirty();
            }
        }
        [JsonIgnore]
        public int Count => _songs?.Count ?? 0;

        public bool AllowDuplicates { get; set; }
        [JsonIgnore]
        public bool IsDirty { get; protected set; }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public Stream GetCoverStream()
        {
            if (Cover == null)
                return null;
            return new MemoryStream(Cover);
        }

        public IPlaylistSong[] GetPlaylistSongs()
        {
            return Beatmaps?.ToArray() ?? Array.Empty<LegacyPlaylistSong>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="base64Str"></param>
        /// <exception cref="FormatException">String is not a valid Base64 string.</exception>
        public void SetCover(string base64Str)
        {
            Cover = Utilities.Util.Base64ToByteArray(ref base64Str);
        }

        public void SetCover(byte[] coverImage)
        {
            Cover = coverImage;
        }

        /// <summary>
        /// Sets the cover image using a Stream. May throw exceptions when reading the provided Stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        public void SetCover(Stream stream)
        {
            if (stream == null)
                return;
            long streamLength = 0;
            try
            {
                streamLength = stream.Length;
            }
            catch { }
            MemoryStream ms;
            if (streamLength > 0)
            {
                ms = new MemoryStream((int)streamLength);
            }
            else
                ms = new MemoryStream();
            stream.CopyTo(ms);
            Cover = ms.ToArray();
            ms.Dispose();
        }

        public bool TryAdd(IPlaylistSong song)
        {
            if (song is LegacyPlaylistSong legacySong)
                return TryAdd(legacySong);
            else
                return TryAdd(new LegacyPlaylistSong(song));
        }
        public bool TryAdd(ISong song)
        {
            if (Beatmaps == null)
                Beatmaps = new List<LegacyPlaylistSong>();
            else if (Beatmaps.Exists(m => m.Hash == song.Hash || (!string.IsNullOrEmpty(m.Key) && m.Key == song.Key)))
                return false;
            return TryAdd(new LegacyPlaylistSong(song));
        }

        public bool TryAdd(LegacyPlaylistSong song)
        {
            if (Beatmaps == null)
                Beatmaps = new List<LegacyPlaylistSong>();
            else if (!AllowDuplicates && Beatmaps.FirstOrDefault(m => m.Equals(song)) != null)
                return false;
            Beatmaps.Add(song);
            return true;
        }

        public bool TryAdd(string songHash, string songName, string songKey, string mapper)
        {
            if (Beatmaps == null)
                Beatmaps = new List<LegacyPlaylistSong>();
            else if (Beatmaps.Exists(m => m.Hash == songHash || (!string.IsNullOrEmpty(m.Key) && m.Key == songKey)))
                return false;
            return TryAdd(new LegacyPlaylistSong(songHash, songName, songKey, mapper));
        }

        public bool TryRemove(string songHashOrKey)
        {
            if (Beatmaps == null)
                return false;
            if (string.IsNullOrEmpty(songHashOrKey))
                return false;
            int numRemoved = Beatmaps.RemoveAll(m => songHashOrKey.Equals(m.Hash, StringComparison.OrdinalIgnoreCase) || songHashOrKey.Equals(m.Key, StringComparison.OrdinalIgnoreCase));
            return numRemoved > 0;
        }

        public bool TryRemove(IPlaylistSong song)
        {
            if (song == null || Beatmaps == null)
                return false;
            bool songRemoved = TryRemove(song.Hash);
            songRemoved = TryRemove(song.Key) || songRemoved;
            return songRemoved;
        }

        public int RemoveAll(Func<LegacyPlaylistSong, bool> match)
        {
            if (Beatmaps == null)
                return 0;
            int songsRemoved = Beatmaps.RemoveAll(m => match(m));
            if (songsRemoved > 0)
                MarkDirty();
            return songsRemoved;
        }

        public void RemoveDuplicates()
        {
            if (Beatmaps == null)
                return;
            int previousCount = Beatmaps.Count;
            Beatmaps = Beatmaps.Distinct().ToList();
            if (Beatmaps.Count != previousCount)
                MarkDirty();
        }
        public void Clear()
        {
            if (Count > 0)
            {
                Beatmaps?.Clear();
                MarkDirty();
            }
        }

        public bool TryStore()
        {
            return TryStore(out _);
        }

        public bool TryStore(out Exception exception)
        {
            exception = null;
            try
            {
                if (File.Exists(FilePath))
                {
                    File.Copy(FilePath, FilePath + ".bak", true);
                    File.Delete(FilePath);
                }
            }
            catch { }
            try
            {
                using (var sw = File.CreateText(FilePath))
                {
                    var serializer = new JsonSerializer() { Formatting = Formatting.Indented };
                    serializer.Serialize(sw, this);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
            try
            {
                File.Delete(FilePath + ".bak");
            }
            catch { }
            return true;
        }

        public void PopulateFromFile(string path, bool updateFilePath = true)
        {
            if (string.IsNullOrEmpty(path))
                path = FilePath;
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path), "A path must be provided for playlists that don't have one.");
            if (!File.Exists(path))
                throw new InvalidOperationException($"The file '{path}' does not exist.");
            JsonConvert.PopulateObject(FileIO.LoadStringFromFile(path), this);
        }

        public LegacyPlaylistSong[] GetBeatmaps() => Beatmaps?.ToArray() ?? Array.Empty<LegacyPlaylistSong>();

    }
}
