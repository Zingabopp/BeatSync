using System;

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
        // Option to delete songs downloaded from certain feeds after x amount of days?
        // Maybe a flag for the AllBeatSyncSongs playlist?

        public void CloneFrom(PluginConfig source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), "source cannot be null for PluginConfig.CloneFrom");
            RegenerateConfig = source.RegenerateConfig;
            BeastSaberUsername = source.BeastSaberUsername;
            SyncCuratorRecommendedFeed = source.SyncCuratorRecommendedFeed;
            SyncBookmarksFeed = source.SyncBookmarksFeed;
            SyncFollowingsFeed = source.SyncFollowingsFeed;
            SyncTopPPFeed = source.SyncTopPPFeed;
            SyncFavoriteMappersFeed = source.SyncFavoriteMappersFeed;
            MaxCuratorRecommendedPages = source.MaxCuratorRecommendedPages;
            MaxBookmarksPages = source.MaxBookmarksPages;
            MaxFollowingsPages = source.MaxFollowingsPages;
            MaxScoreSaberSongs = source.MaxScoreSaberSongs;
            MaxBeatSaverPages = source.MaxBeatSaverPages;
            //  DeleteOldVersions = source.DeleteOldVersions;// not yet supported
            //  DeleteDuplicateSongs = source.DeleteDuplicateSongs;//
            DownloadTimeout = source.DownloadTimeout;
            MaxConcurrentDownloads = source.MaxConcurrentDownloads;
            MaxConcurrentPageChecks = source.MaxConcurrentPageChecks;
            RecentPlaylistDays = source.RecentPlaylistDays;
            CreateMapperPlaylists = source.CreateMapperPlaylists;
        }
    }

}
