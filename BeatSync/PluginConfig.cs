namespace BeatSync
{
    internal class PluginConfig
    {
        public bool RegenerateConfig { get; set; }
        public string BeastSaberUsername { get; set; }
        public bool SyncCuratorRecommendedFeed { get; set; }
        public bool SyncBookmarksFeed { get; set; }
        public bool SyncFollowingsFeed { get; set; }
        public bool SyncTopPPFeed { get; set; }
        public bool SyncFavoriteMappersFeed { get; set; }
        public int MaxCuratorRecommendedPages { get; set; }
        public int MaxBookmarksPages { get; set; }
        public int MaxFollowingsPages { get; set; }
        public int MaxScoreSaberSongs { get; set; }
        public int MaxBeatSaverPages { get; set; }
        // public bool DeleteOldVersions { get; set; } not yet supported
        // public bool DeleteDuplicateSongs { get; set; }
        public int DownloadTimeout { get; set; }
        public int MaxConcurrentDownloads { get; set; }
        public int MaxConcurrentPageChecks { get; set; }
        public int RecentPlaylistDays { get; set; } // Remember to change SyncSaberService to add date to playlist entry
        public bool CreateMapperPlaylists { get; set; }
    }
}
