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
        [JsonIgnore]
        public Playlists.BuiltInPlaylist FeedPlaylist { get; protected set; }
    }


}
