using System;
using Newtonsoft.Json;
// Option to delete songs downloaded from certain feeds after x amount of days?
// Maybe a flag for the AllBeatSyncSongs playlist?
// public bool DeleteOldVersions { get; set; } not yet supported
// public bool DeleteDuplicateSongs { get; set; }
namespace BeatSync.Configs
{
    internal class PluginConfig
    {
        [JsonIgnore]
        private bool _regenerateConfig = true;
        [JsonProperty(Order = -100)]
        public bool RegenerateConfig { get { return _regenerateConfig; } set { _regenerateConfig = value; } }
        [JsonProperty(Order = -90)]
        public int DownloadTimeout { get; set; }
        [JsonProperty(Order = -80)]
        public int MaxConcurrentDownloads { get; set; }
        [JsonProperty(Order = -70)]
        public int RecentPlaylistDays { get; set; } // Remember to change SyncSaberService to add date to playlist entry
        [JsonProperty(Order = -60)]
        public BeastSaberConfig BeastSaber { get; set; }
        [JsonProperty(Order = -50)]
        public BeatSaverConfig BeatSaver { get; set; }
        [JsonProperty(Order = -40)]
        public ScoreSaberConfig ScoreSaber { get; set; }

        

        public PluginConfig SetDefaults()
        {
            RegenerateConfig = false;
            DownloadTimeout = 30;
            RecentPlaylistDays = 7;

            BeatSaver = new BeatSaverConfig()
            {
                Enabled = true,
                MaxConcurrentPageChecks = 5,
                Hot = new BeatSaverHot() { Enabled = false, MaxSongs = 10, CreatePlaylist = true },
                Downloads = new BeatSaverDownloads() { Enabled = false, MaxSongs = 20, CreatePlaylist = true },
                FavoriteMappers = new BeatSaverFavoriteMappers() { Enabled = true, MaxSongs = 0, SeparateMapperPlaylists = false, CreatePlaylist = true }
            };

            BeastSaber = new BeastSaberConfig()
            {
                Enabled = true,
                MaxConcurrentPageChecks = 5,
                Username = "",
                Bookmarks = new BeastSaberBookmarks() { Enabled = true, MaxSongs = 0, CreatePlaylist = true },
                Followings = new BeastSaberFollowings() { Enabled = true, MaxSongs = 0, CreatePlaylist = true },
                CuratorRecommended = new BeastSaberCuratorRecommended() { Enabled = true, MaxSongs = 20, CreatePlaylist = true }
            };

            ScoreSaber = new ScoreSaberConfig()
            {
                Enabled = false,
                Trending = new ScoreSaberTrending() { Enabled = true, MaxSongs = 20, RankedOnly = false, CreatePlaylist = true },
                TopRanked = new ScoreSaberTopRanked() { Enabled = false, MaxSongs = 20, CreatePlaylist = true },
                LatestRanked = new ScoreSaberLatestRanked() { Enabled = true, MaxSongs = 20, CreatePlaylist = true },
                TopPlayed = new ScoreSaberTopPlayed() { Enabled = false, MaxSongs = 20, RankedOnly = false, CreatePlaylist = true }
            };
            return this;
        }
    }





}
