using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            SetCover(Utilities.Util.StringToByteArray(cover));
        }
        public LegacyPlaylist(string filePath, string title, string author, Lazy<string> coverLoader)
        {
            FilePath = filePath;
            Title = title;
            Author = author;
            CoverLoader = coverLoader;
        }
        private Lazy<string> CoverLoader;
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
        [JsonProperty("playlistDescription", Order = 0)]
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
        protected byte[] Cover
        {
            get 
            {
                if (_coverImage == null && CoverLoader != null)
                    _coverImage = Utilities.Util.StringToByteArray(CoverLoader.Value);
                return _coverImage; 
            }
            set { _coverImage = value; }
        }

        #endregion
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
        public int Count => _songs.Count;

        public bool AllowDuplicates { get; set; }
        public bool IsDirty { get; protected set; }

        public void MarkDirty()
        {
            IsDirty = true;
        }

        public bool TryAdd(string songHash, string songName, string songKey, string mapper)
        {
            throw new NotImplementedException();
        }

        public Stream GetCoverStream()
        {
            throw new NotImplementedException();
        }

        public IPlaylistSong[] GetPlaylistSongs()
        {
            throw new NotImplementedException();
        }

        public void SetCover(byte[] coverImage)
        {
            throw new NotImplementedException();
        }

        public bool TryAdd(IPlaylistSong song)
        {
            throw new NotImplementedException();
        }

        public bool TryRemove(string songHash)
        {
            throw new NotImplementedException();
        }

        public bool TryRemove(IPlaylistSong song)
        {
            throw new NotImplementedException();
        }

        public int RemoveAll(Func<IPlaylistSong, bool> match)
        {
            throw new NotImplementedException();
        }

        public void RemoveDuplicates()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool TryStore()
        {
            throw new NotImplementedException();
        }

        public bool TryStore(out Exception exception)
        {
            throw new NotImplementedException();
        }

        public LegacyPlaylistSong[] GetBeatmaps()
        {
            throw new NotImplementedException();
        }

        public int RemoveAll(Func<LegacyPlaylistSong, bool> match)
        {
            throw new NotImplementedException();
        }

        public bool TryAdd(LegacyPlaylistSong song)
        {
            throw new NotImplementedException();
        }
    }
}
