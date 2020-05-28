using Newtonsoft.Json;
using SongFeedReaders.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace BeatSyncPlaylists.Legacy
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class LegacyPlaylist : Playlist<LegacyPlaylistSong>
    {
        [DataMember]
        [JsonProperty("songs", Order = 5)]
        protected override IList<LegacyPlaylistSong> _songs { get; set; } = new List<LegacyPlaylistSong>();
        private Lazy<string>? ImageLoader;
        protected LegacyPlaylist()
        { }

        protected LegacyPlaylist(string fileName, string title, string? author)
        {
            Filename = fileName;
            Title = title;
            Author = author;
        }

        public LegacyPlaylist(string fileName, string title, string? author, Lazy<string> imageLoader)
            : this(fileName, title, author)
        {
            ImageLoader = imageLoader;
        }

        public LegacyPlaylist(string fileName, string title, string? author, string? coverImage)
            : this(fileName, title, author)
        {
            SetCover(coverImage);
        }

        protected override LegacyPlaylistSong ConvertFrom(ISong song)
        {
            if (song is LegacyPlaylistSong legacySong)
                return legacySong;
            return new LegacyPlaylistSong(song);
        }

        [DataMember]
        [JsonProperty("playlistTitle", Order = -10)]
        public override string Title { get; set; } = string.Empty;
        [DataMember]
        [JsonProperty("playlistAuthor", Order = -5)]
        public override string? Author { get; set; }
        [DataMember]
        [JsonProperty("playlistDescription", Order = 0, NullValueHandling = NullValueHandling.Ignore)]
        public override string? Description { get; set; }

        private string _coverString;
        [DataMember]
        [JsonProperty("image", Order = 10)]
        protected string coverString
        {
            get
            {
                if (_coverString == null)
                    _coverString = ImageLoader?.Value ?? string.Empty;
                return _coverString;
            }
            set
            {
                _coverString = value;
            }
        }
        public override string Filename { get; set; } = string.Empty;

        public override bool IsReadOnly => false;
        public override int RemoveAll(Func<LegacyPlaylistSong, bool> match)
        {
            int removedSongs = 0;
            if (match != null)
                removedSongs = ((List<LegacyPlaylistSong>)_songs).RemoveAll(s => match(s));
            return removedSongs;
        }

        public override Stream GetCoverStream()
        {
            throw new NotImplementedException();
        }

        public override void SetCover(byte[] coverImage)
        {
            throw new NotImplementedException();
        }

        public override void SetCover(string? coverImageStr)
        {
            if (coverImageStr == null)
                return;
            ImageLoader = new Lazy<string>(() => coverImageStr);
        }

        public override void SetCover(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
