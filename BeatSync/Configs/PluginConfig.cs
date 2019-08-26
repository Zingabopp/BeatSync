using System;
using Newtonsoft.Json;
using System.Diagnostics;
// Option to delete songs downloaded from certain feeds after x amount of days?
// public bool DeleteOldVersions { get; set; } not yet supported
// public bool DeleteDuplicateSongs { get; set; }
namespace BeatSync.Configs
{
    public class PluginConfig
    {
        public static PluginConfig DefaultConfig = new PluginConfig().SetDefaults();

        [JsonIgnore]
        private bool _regenerateConfig = true;

        [JsonIgnore]
        public bool ConfigChanged { get; set; }
        [JsonIgnore]
        private int _maxConcurrentDownloads = 3;
        [JsonIgnore]
        private int _recentPlaylistDays = 7;

        [JsonProperty(Order = -100)]
        public bool RegenerateConfig { get { return _regenerateConfig; } set { _regenerateConfig = value; } }
        [JsonProperty(Order = -90)]
        public int DownloadTimeout { get; set; }
        [JsonProperty(Order = -80)]
        public int MaxConcurrentDownloads
        {
            get { return _maxConcurrentDownloads; }
            set
            {
                if (value < 1)
                    _maxConcurrentDownloads = 1;
                else if (value > 10)
                    _maxConcurrentDownloads = 10;
                else
                    _maxConcurrentDownloads = value;
            }
        }
        [JsonProperty(Order = -70)]
        public int RecentPlaylistDays
        {
            get { return _recentPlaylistDays; }
            set
            {
                if (value < 0)
                    _recentPlaylistDays = 0;
                else
                    _recentPlaylistDays = value;
            }
        } // Remember to change SyncSaberService to add date to playlist entry

        [JsonProperty(Order = -60)]
        public bool AllBeatSyncSongsPlaylist { get; set; }
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
            MaxConcurrentDownloads = 3;
            AllBeatSyncSongsPlaylist = false;
            RecentPlaylistDays = 7;

            BeatSaver = new BeatSaverConfig()
            {
                Enabled = false,
                MaxConcurrentPageChecks = 5,
                Hot = new BeatSaverHot() { Enabled = false, MaxSongs = 10, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append },
                Downloads = new BeatSaverDownloads() { Enabled = false, MaxSongs = 20, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append },
                // , SeparateMapperPlaylists = false
                FavoriteMappers = new BeatSaverFavoriteMappers() { Enabled = true, MaxSongs = 0, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append }
            };

            BeastSaber = new BeastSaberConfig()
            {
                Enabled = true,
                MaxConcurrentPageChecks = 5,
                Username = "",
                Bookmarks = new BeastSaberBookmarks() { Enabled = true, MaxSongs = 0, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append },
                Follows = new BeastSaberFollowings() { Enabled = true, MaxSongs = 20, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append },
                CuratorRecommended = new BeastSaberCuratorRecommended() { Enabled = false, MaxSongs = 20, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append }
            };

            ScoreSaber = new ScoreSaberConfig()
            {
                Enabled = false,
                TopRanked = new ScoreSaberTopRanked() { Enabled = false, MaxSongs = 20, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Replace },
                LatestRanked = new ScoreSaberLatestRanked() { Enabled = true, MaxSongs = 20, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Replace },
                Trending = new ScoreSaberTrending() { Enabled = true, MaxSongs = 20, RankedOnly = false, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append },
                TopPlayed = new ScoreSaberTopPlayed() { Enabled = false, MaxSongs = 20, RankedOnly = false, CreatePlaylist = true, PlaylistStyle = PlaylistStyle.Append }
            };
            return this;
        }
    }
}
