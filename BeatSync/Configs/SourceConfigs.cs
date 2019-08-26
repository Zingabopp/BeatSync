using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Configs
{
    public class BeatSaverConfig : SourceConfigBase
    {
        [JsonProperty(Order = -60)]
        public int MaxConcurrentPageChecks { get; set; }
        [JsonProperty(Order = -50)]
        public BeatSaverFavoriteMappers FavoriteMappers { get; set; }
        [JsonProperty(Order = -40)]
        public BeatSaverHot Hot { get; set; }
        [JsonProperty(Order = -30)]
        public BeatSaverDownloads Downloads { get; set; }
        
    }

    public class BeastSaberConfig : SourceConfigBase
    {
        [JsonProperty(Order = -60)]
        public string Username { get; set; }
        [JsonProperty(Order = -50)]
        public int MaxConcurrentPageChecks { get; set; }
        [JsonProperty(Order = -40)]
        public BeastSaberBookmarks Bookmarks { get; set; }
        [JsonProperty(Order = -30)]
        public BeastSaberFollowings Follows { get; set; }
        [JsonProperty(Order = -20)]
        public BeastSaberCuratorRecommended CuratorRecommended { get; set; }
    }

    public class ScoreSaberConfig : SourceConfigBase
    {
        [JsonProperty(Order = -60)]
        public ScoreSaberTopRanked TopRanked { get; set; }
        [JsonProperty(Order = -50)]
        public ScoreSaberLatestRanked LatestRanked { get; set; }
        [JsonProperty(Order = -40)]
        public ScoreSaberTrending Trending { get; set; }
        [JsonProperty(Order = -30)]
        public ScoreSaberTopPlayed TopPlayed { get; set; }
    }
}
