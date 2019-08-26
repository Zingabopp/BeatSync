using Newtonsoft.Json;

namespace BeatSync.Configs
{
    public abstract class SourceConfigBase
    {
        [JsonProperty(Order = -100)]
        public bool Enabled { get; set; }
    }

    public abstract class FeedConfigBase
    {
        [JsonProperty(Order = -100)]
        public bool Enabled { get; set; }
        [JsonProperty(Order = -90)]
        public int MaxSongs { get; set; }
        [JsonProperty(Order = -80)]
        public bool CreatePlaylist { get; set; }
        [JsonProperty(Order = -70)]
        public PlaylistStyle PlaylistStyle { get; set; }
        [JsonIgnore]
        public Playlists.BuiltInPlaylist FeedPlaylist { get; protected set; }
    }

    public enum PlaylistStyle
    {
        /// <summary>
        /// Songs are added to the existing playlist.
        /// </summary>
        Append,
        /// <summary>
        /// Old playlist is ignored, replaced by the session's scrape.
        /// </summary>
        Replace
    }

    /// <summary>
    /// Decide which songs get added to the BeatSync playlists.
    /// </summary>
    public enum PlaylistFilter
    {
        /// <summary>
        /// All songs read from the feed are added to the feed playlist.
        /// </summary>
        KeepAll,
        /// <summary>
        /// Only songs that are downloaded are added to the feed playlist.
        /// </summary>
        DownloadedOnly
    }
}
