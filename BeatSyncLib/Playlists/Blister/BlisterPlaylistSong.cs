using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using SongFeedReaders.Data;

namespace BeatSyncLib.Playlists.Blister
{
    [Serializable]
    public class BlisterPlaylistSong : IFeedSong
    {
        public BlisterPlaylistSong()
        {
            _feedSources = new HashSet<string>();
        }

        public BlisterPlaylistSong(IPlaylistSong song)
            : this()
        {
            this.Populate(song);
            if (!string.IsNullOrEmpty(Hash))
                Type = BeatmapType.Hash;
            else if (!string.IsNullOrEmpty(Key))
                Type = BeatmapType.Key;
        }

        public BlisterPlaylistSong(ISong song)
            : this()
        {
            this.Populate(song);
            if (!string.IsNullOrEmpty(Hash))
                Type = BeatmapType.Hash;
            else if (!string.IsNullOrEmpty(Key))
                Type = BeatmapType.Key;
        }

        [NonSerialized]
        private readonly HashSet<string> _feedSources;
        /// <summary>
        /// Beatmap type
        /// </summary>
        [JsonProperty("type")]
        public BeatmapType Type { get; set; }

        /// <summary>
        /// Date this entry was added to the playlist
        /// </summary>
        [JsonProperty("dateAdded")]
        protected DateTime BlisterDateAdded { get; set; }

        /// <summary>
        /// BeatSaver Key
        /// </summary>
        [JsonProperty("key", NullValueHandling = NullValueHandling.Ignore)]
        protected uint? KeyInt { get; set; }

        /// <summary>
        /// Beatmap sha1sum
        /// </summary>
        [JsonProperty("hash", NullValueHandling = NullValueHandling.Ignore)]
        protected byte[] ByteHash
        {
            get => Utilities.Util.StringToByteArray(Hash);
            set { Hash = Utilities.Util.ByteArrayToString(value); }
        }

        /// <summary>
        /// Beatmap zip as bytes
        /// </summary>
        [JsonProperty("bytes", NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Bytes { get; set; }

        /// <summary>
        /// Beatmap level ID
        /// </summary>
        [JsonProperty("levelID", NullValueHandling = NullValueHandling.Ignore)]
        public string LevelID { get; set; }

        public HashSet<string> FeedSources => throw new NotImplementedException();

        [JsonIgnore]
        public string Hash
        {
            get;
            set;
        }
        /// <summary>
        /// Beat Saver key for the song.
        /// </summary>
        /// <exception cref="FormatException">Thrown when setting with a string that can't be converted to a hex number.</exception>
        /// <exception cref="OverflowException">Thrown when setting with a string whose hex value is greater than uint.MaxValue or has fractional digits.</exception>
        [JsonIgnore]
        public string Key
        {
            get => KeyInt?.ToString("X");
            set
            {
                if (value == null)
                {
                    KeyInt = null;
                    return;
                }
                KeyInt = uint.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }
        }
        [JsonProperty("LevelAuthorName", NullValueHandling = NullValueHandling.Ignore)]
        public string LevelAuthorName { get; set; }

        [JsonProperty("SongName", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
        [JsonIgnore]
        public DateTime? DateAdded
        {
            get => BlisterDateAdded;
            set
            {
                BlisterDateAdded = value ?? DateTime.MinValue;
            }
        }

        public void AddFeedSource(string sourceName)
        {
            if (!_feedSources.Contains(sourceName))
                _feedSources.Add(sourceName);
        }

        public bool Equals(IPlaylistSong other)
        {
            if (Hash != null && other.Hash != null)
                return Hash.Equals(other.Hash, StringComparison.OrdinalIgnoreCase);
            else if (Key != null && other.Key != null)
                return Key == other.Key;
            return false;
        }
    }
}
