using System;
using System.Collections.Generic;
using System.Linq;

using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSync.Configs;
using BeatSyncLib.Configs;

namespace BeatSync.UI
{
    internal class SettingsUI
    {
        public BeatSyncConfig BeatSyncConfig;
        public BeatSyncModConfig BeatSyncModConfig;

        public SettingsUI(BeatSyncModConfig modConfig)
        {
            BeatSyncModConfig = modConfig ?? throw new ArgumentNullException(nameof(modConfig));
            BeatSyncConfig = modConfig.BeatSyncConfig ?? throw new ArgumentNullException(nameof(modConfig));
        }


        [UIAction("#apply")]
        public void OnApply()
        {
            if (BeatSyncConfig.ConfigChanged || BeatSyncModConfig.ConfigChanged)
            {
                Plugin.ConfigManager.SaveConfig();
            }
        }


        public static readonly List<object> MaxSongOptions = new List<object>()
        {
            0, 5, 10, 20, 30, 50, 100, 200, 300, 500
        };

        public static readonly List<object> PlaylistTypeOptions = new List<object>()
        {
            PlaylistStyle.Append,
            PlaylistStyle.Replace
        };

        [UIValue("MaxSongOptions")]
        private List<object> _maxSongOptions = MaxSongOptions;
        [UIValue("PlaylistStyleOptions")]
        private List<object> _playlistTypeOptions = PlaylistTypeOptions;

        #region BeatSyncModConfig

        #endregion
        #region BeatSyncConfig
        [UIValue("DownloadTimeout")]
        public int DownloadTimeout
        {
            get => BeatSyncConfig.DownloadTimeout;
            set => BeatSyncConfig.DownloadTimeout = value;
        }
        [UIValue("MaxConcurrentDownloads")]
        public int MaxConcurrentDownloads
        {
            get => BeatSyncConfig.MaxConcurrentDownloads;
            set => BeatSyncConfig.MaxConcurrentDownloads = value;
        }
        [UIValue("RecentPlaylistDays")]
        public int RecentPlaylistDays
        {
            get => BeatSyncConfig.RecentPlaylistDays;
            set => BeatSyncConfig.RecentPlaylistDays = value;
        }
        [UIValue("AllBeatSyncSongsPlaylist")]
        public bool AllBeatSyncSongsPlaylist
        {
            get => BeatSyncConfig.AllBeatSyncSongsPlaylist;
            set => BeatSyncConfig.AllBeatSyncSongsPlaylist = value;
        }
        #region BeastSaber
        [UIValue("BeastSaber.Enabled")]
        public bool BeastSaber_Enabled
        {
            get => BeatSyncConfig.BeastSaber.Enabled;
            set => BeatSyncConfig.BeastSaber.Enabled = value;
        }
        [UIValue("BeastSaber.Username")]
        public string BeastSaber_Username
        {
            get => BeatSyncConfig.BeastSaber.Username;
            set => BeatSyncConfig.BeastSaber.Username = value;
        }
        [UIValue("BeastSaber.MaxConcurrentPageChecks")]
        public int BeastSaber_MaxConcurrentPageChecks
        {
            get => BeatSyncConfig.BeastSaber.MaxConcurrentPageChecks;
            set => BeatSyncConfig.BeastSaber.MaxConcurrentPageChecks = value;
        }

        #region Bookmarks
        [UIValue("BeastSaber.Bookmarks.Enabled")]
        public bool BeastSaber_Bookmarks_Enabled
        {
            get => BeatSyncConfig.BeastSaber.Bookmarks.Enabled;
            set => BeatSyncConfig.BeastSaber.Bookmarks.Enabled = value;
        }
        [UIValue("BeastSaber.Bookmarks.MaxSongs")]
        public int BeastSaber_Bookmarks_MaxSongs
        {
            get => BeatSyncConfig.BeastSaber.Bookmarks.MaxSongs;
            set => BeatSyncConfig.BeastSaber.Bookmarks.MaxSongs = value;
        }
        [UIValue("BeastSaber.Bookmarks.CreatePlaylist")]
        public bool BeastSaber_Bookmarks_CreatePlaylist
        {
            get => BeatSyncConfig.BeastSaber.Bookmarks.CreatePlaylist;
            set => BeatSyncConfig.BeastSaber.Bookmarks.CreatePlaylist = value;
        }
        [UIValue("BeastSaber.Bookmarks.PlaylistStyle")]
        public PlaylistStyle BeastSaber_Bookmarks_PlaylistStyle
        {
            get => BeatSyncConfig.BeastSaber.Bookmarks.PlaylistStyle;
            set => BeatSyncConfig.BeastSaber.Bookmarks.PlaylistStyle = value;
        }
        #endregion
        #region Follows
        [UIValue("BeastSaber.Follows.Enabled")]
        public bool BeastSaber_Follows_Enabled
        {
            get => BeatSyncConfig.BeastSaber.Follows.Enabled;
            set => BeatSyncConfig.BeastSaber.Follows.Enabled = value;
        }
        [UIValue("BeastSaber.Follows.MaxSongs")]
        public int BeastSaber_Follows_MaxSongs
        {
            get => BeatSyncConfig.BeastSaber.Follows.MaxSongs;
            set => BeatSyncConfig.BeastSaber.Follows.MaxSongs = value;
        }
        [UIValue("BeastSaber.Follows.CreatePlaylist")]
        public bool BeastSaber_Follows_CreatePlaylist
        {
            get => BeatSyncConfig.BeastSaber.Follows.CreatePlaylist;
            set => BeatSyncConfig.BeastSaber.Follows.CreatePlaylist = value;
        }
        [UIValue("BeastSaber.Follows.PlaylistStyle")]
        public PlaylistStyle BeastSaber_Follows_PlaylistStyle
        {
            get => BeatSyncConfig.BeastSaber.Follows.PlaylistStyle;
            set => BeatSyncConfig.BeastSaber.Follows.PlaylistStyle = value;
        }
        #endregion
        #region CuratorRecommended
        [UIValue("BeastSaber.CuratorRecommended.Enabled")]
        public bool BeastSaber_CuratorRecommended_Enabled
        {
            get => BeatSyncConfig.BeastSaber.CuratorRecommended.Enabled;
            set => BeatSyncConfig.BeastSaber.CuratorRecommended.Enabled = value;
        }
        [UIValue("BeastSaber.CuratorRecommended.MaxSongs")]
        public int BeastSaber_CuratorRecommended_MaxSongs
        {
            get => BeatSyncConfig.BeastSaber.CuratorRecommended.MaxSongs;
            set => BeatSyncConfig.BeastSaber.CuratorRecommended.MaxSongs = value;
        }
        [UIValue("BeastSaber.CuratorRecommended.CreatePlaylist")]
        public bool BeastSaber_CuratorRecommended_CreatePlaylist
        {
            get => BeatSyncConfig.BeastSaber.CuratorRecommended.CreatePlaylist;
            set => BeatSyncConfig.BeastSaber.CuratorRecommended.CreatePlaylist = value;
        }
        [UIValue("BeastSaber.CuratorRecommended.PlaylistStyle")]
        public PlaylistStyle BeastSaber_CuratorRecommended_PlaylistStyle
        {
            get => BeatSyncConfig.BeastSaber.CuratorRecommended.PlaylistStyle;
            set => BeatSyncConfig.BeastSaber.CuratorRecommended.PlaylistStyle = value;
        }
        #endregion
        #endregion

        #region BeatSaver
        [UIValue("BeatSaver.Enabled")]
        public bool BeatSaver_Enabled
        {
            get => BeatSyncConfig.BeatSaver.Enabled;
            set => BeatSyncConfig.BeatSaver.Enabled = value;
        }
        [UIValue("BeatSaver.MaxConcurrentPageChecks")]
        public int BeatSaver_MaxConcurrentPageChecks
        {
            get => BeatSyncConfig.BeatSaver.MaxConcurrentPageChecks;
            set => BeatSyncConfig.BeatSaver.MaxConcurrentPageChecks = value;
        }
        #region FavoriteMappers
        [UIValue("BeatSaver.FavoriteMappers.Enabled")]
        public bool BeatSaver_FavoriteMappers_Enabled
        {
            get => BeatSyncConfig.BeatSaver.FavoriteMappers.Enabled;
            set => BeatSyncConfig.BeatSaver.FavoriteMappers.Enabled = value;
        }
        [UIValue("BeatSaver.FavoriteMappers.MaxSongs")]
        public int BeatSaver_FavoriteMappers_MaxSongs
        {
            get => BeatSyncConfig.BeatSaver.FavoriteMappers.MaxSongs;
            set => BeatSyncConfig.BeatSaver.FavoriteMappers.MaxSongs = value;
        }
        [UIValue("BeatSaver.FavoriteMappers.CreatePlaylist")]
        public bool BeatSaver_FavoriteMappers_CreatePlaylist
        {
            get => BeatSyncConfig.BeatSaver.FavoriteMappers.CreatePlaylist;
            set => BeatSyncConfig.BeatSaver.FavoriteMappers.CreatePlaylist = value;
        }
        [UIValue("BeatSaver.FavoriteMappers.PlaylistStyle")]
        public PlaylistStyle BeatSaver_FavoriteMappers_PlaylistStyle
        {
            get => BeatSyncConfig.BeatSaver.FavoriteMappers.PlaylistStyle;
            set => BeatSyncConfig.BeatSaver.FavoriteMappers.PlaylistStyle = value;
        }
        [UIValue("BeatSaver.FavoriteMappers.SeparateMapperPlaylists")]
        public bool BeatSaver_FavoriteMappers_SeparateMapperPlaylists
        {
            get => BeatSyncConfig.BeatSaver.FavoriteMappers.SeparateMapperPlaylists;
            set => BeatSyncConfig.BeatSaver.FavoriteMappers.SeparateMapperPlaylists = value;
        }
        #endregion
        #region Hot
        [UIValue("BeatSaver.Hot.Enabled")]
        public bool BeatSaver_Hot_Enabled
        {
            get => BeatSyncConfig.BeatSaver.Hot.Enabled;
            set => BeatSyncConfig.BeatSaver.Hot.Enabled = value;
        }
        [UIValue("BeatSaver.Hot.MaxSongs")]
        public int BeatSaver_Hot_MaxSongs
        {
            get => BeatSyncConfig.BeatSaver.Hot.MaxSongs;
            set => BeatSyncConfig.BeatSaver.Hot.MaxSongs = value;
        }
        [UIValue("BeatSaver.Hot.CreatePlaylist")]
        public bool BeatSaver_Hot_CreatePlaylist
        {
            get => BeatSyncConfig.BeatSaver.Hot.CreatePlaylist;
            set => BeatSyncConfig.BeatSaver.Hot.CreatePlaylist = value;
        }
        [UIValue("BeatSaver.Hot.PlaylistStyle")]
        public PlaylistStyle BeatSaver_Hot_PlaylistStyle
        {
            get => BeatSyncConfig.BeatSaver.Hot.PlaylistStyle;
            set => BeatSyncConfig.BeatSaver.Hot.PlaylistStyle = value;
        }
        #endregion
        #region Downloads
        [UIValue("BeatSaver.Downloads.Enabled")]
        public bool BeatSaver_Downloads_Enabled
        {
            get => BeatSyncConfig.BeatSaver.Downloads.Enabled;
            set => BeatSyncConfig.BeatSaver.Downloads.Enabled = value;
        }
        [UIValue("BeatSaver.Downloads.MaxSongs")]
        public int BeatSaver_Downloads_MaxSongs
        {
            get => BeatSyncConfig.BeatSaver.Downloads.MaxSongs;
            set => BeatSyncConfig.BeatSaver.Downloads.MaxSongs = value;
        }
        [UIValue("BeatSaver.Downloads.CreatePlaylist")]
        public bool BeatSaver_Downloads_CreatePlaylist
        {
            get => BeatSyncConfig.BeatSaver.Downloads.CreatePlaylist;
            set => BeatSyncConfig.BeatSaver.Downloads.CreatePlaylist = value;
        }
        [UIValue("BeatSaver.Downloads.PlaylistStyle")]
        public PlaylistStyle BeatSaver_Downloads_PlaylistStyle
        {
            get => BeatSyncConfig.BeatSaver.Downloads.PlaylistStyle;
            set => BeatSyncConfig.BeatSaver.Downloads.PlaylistStyle = value;
        }
        #endregion
        #region Latest
        [UIValue("BeatSaver.Latest.Enabled")]
        public bool BeatSaver_Latest_Enabled
        {
            get => BeatSyncConfig.BeatSaver.Latest.Enabled;
            set => BeatSyncConfig.BeatSaver.Latest.Enabled = value;
        }
        [UIValue("BeatSaver.Latest.MaxSongs")]
        public int BeatSaver_Latest_MaxSongs
        {
            get => BeatSyncConfig.BeatSaver.Latest.MaxSongs;
            set => BeatSyncConfig.BeatSaver.Latest.MaxSongs = value;
        }
        [UIValue("BeatSaver.Latest.CreatePlaylist")]
        public bool BeatSaver_Latest_CreatePlaylist
        {
            get => BeatSyncConfig.BeatSaver.Latest.CreatePlaylist;
            set => BeatSyncConfig.BeatSaver.Latest.CreatePlaylist = value;
        }
        [UIValue("BeatSaver.Latest.PlaylistStyle")]
        public PlaylistStyle BeatSaver_Latest_PlaylistStyle
        {
            get => BeatSyncConfig.BeatSaver.Latest.PlaylistStyle;
            set => BeatSyncConfig.BeatSaver.Latest.PlaylistStyle = value;
        }
        #endregion
        #endregion

        #region ScoreSaber

        [UIValue("ScoreSaber.Enabled")]
        public bool ScoreSaber_Enabled
        {
            get => BeatSyncConfig.ScoreSaber.Enabled;
            set => BeatSyncConfig.ScoreSaber.Enabled = value;
        }
        #region TopRanked
        [UIValue("ScoreSaber.TopRanked.Enabled")]
        public bool ScoreSaber_TopRanked_Enabled
        {
            get => BeatSyncConfig.ScoreSaber.TopRanked.Enabled;
            set => BeatSyncConfig.ScoreSaber.TopRanked.Enabled = value;
        }
        [UIValue("ScoreSaber.TopRanked.MaxSongs")]
        public int ScoreSaber_TopRanked_MaxSongs
        {
            get => BeatSyncConfig.ScoreSaber.TopRanked.MaxSongs;
            set => BeatSyncConfig.ScoreSaber.TopRanked.MaxSongs = value;
        }
        [UIValue("ScoreSaber.TopRanked.CreatePlaylist")]
        public bool ScoreSaber_TopRanked_CreatePlaylist
        {
            get => BeatSyncConfig.ScoreSaber.TopRanked.CreatePlaylist;
            set => BeatSyncConfig.ScoreSaber.TopRanked.CreatePlaylist = value;
        }
        [UIValue("ScoreSaber.TopRanked.PlaylistStyle")]
        public PlaylistStyle ScoreSaber_TopRanked_PlaylistStyle
        {
            get => BeatSyncConfig.ScoreSaber.TopRanked.PlaylistStyle;
            set => BeatSyncConfig.ScoreSaber.TopRanked.PlaylistStyle = value;
        }
        [UIValue("ScoreSaber.TopRanked.MaxStars")]
        public float ScoreSaber_TopRanked_MaxStars
        {
            get => BeatSyncConfig.ScoreSaber.TopRanked.MaxStars;
            set => BeatSyncConfig.ScoreSaber.TopRanked.MaxStars = value;
        }
        [UIValue("ScoreSaber.TopRanked.MinStars")]
        public float ScoreSaber_TopRanked_MinStars
        {
            get => BeatSyncConfig.ScoreSaber.TopRanked.MinStars;
            set => BeatSyncConfig.ScoreSaber.TopRanked.MinStars = value;
        }
        #endregion
        #region LatestRanked
        [UIValue("ScoreSaber.LatestRanked.Enabled")]
        public bool ScoreSaber_LatestRanked_Enabled
        {
            get => BeatSyncConfig.ScoreSaber.LatestRanked.Enabled;
            set => BeatSyncConfig.ScoreSaber.LatestRanked.Enabled = value;
        }
        [UIValue("ScoreSaber.LatestRanked.MaxSongs")]
        public int ScoreSaber_LatestRanked_MaxSongs
        {
            get => BeatSyncConfig.ScoreSaber.LatestRanked.MaxSongs;
            set => BeatSyncConfig.ScoreSaber.LatestRanked.MaxSongs = value;
        }
        [UIValue("ScoreSaber.LatestRanked.CreatePlaylist")]
        public bool ScoreSaber_LatestRanked_CreatePlaylist
        {
            get => BeatSyncConfig.ScoreSaber.LatestRanked.CreatePlaylist;
            set => BeatSyncConfig.ScoreSaber.LatestRanked.CreatePlaylist = value;
        }
        [UIValue("ScoreSaber.LatestRanked.PlaylistStyle")]
        public PlaylistStyle ScoreSaber_LatestRanked_PlaylistStyle
        {
            get => BeatSyncConfig.ScoreSaber.LatestRanked.PlaylistStyle;
            set => BeatSyncConfig.ScoreSaber.LatestRanked.PlaylistStyle = value;
        }
        [UIValue("ScoreSaber.LatestRanked.MaxStars")]
        public float ScoreSaber_LatestRanked_MaxStars
        {
            get => BeatSyncConfig.ScoreSaber.LatestRanked.MaxStars;
            set => BeatSyncConfig.ScoreSaber.LatestRanked.MaxStars = value;
        }
        [UIValue("ScoreSaber.LatestRanked.MinStars")]
        public float ScoreSaber_LatestRanked_MinStars
        {
            get => BeatSyncConfig.ScoreSaber.LatestRanked.MinStars;
            set => BeatSyncConfig.ScoreSaber.LatestRanked.MinStars = value;
        }
        #endregion
        #region Trending
        [UIValue("ScoreSaber.Trending.Enabled")]
        public bool ScoreSaber_Trending_Enabled
        {
            get => BeatSyncConfig.ScoreSaber.Trending.Enabled;
            set => BeatSyncConfig.ScoreSaber.Trending.Enabled = value;
        }
        [UIValue("ScoreSaber.Trending.MaxSongs")]
        public int ScoreSaber_Trending_MaxSongs
        {
            get => BeatSyncConfig.ScoreSaber.Trending.MaxSongs;
            set => BeatSyncConfig.ScoreSaber.Trending.MaxSongs = value;
        }
        [UIValue("ScoreSaber.Trending.CreatePlaylist")]
        public bool ScoreSaber_Trending_CreatePlaylist
        {
            get => BeatSyncConfig.ScoreSaber.Trending.CreatePlaylist;
            set => BeatSyncConfig.ScoreSaber.Trending.CreatePlaylist = value;
        }
        [UIValue("ScoreSaber.Trending.PlaylistStyle")]
        public PlaylistStyle ScoreSaber_Trending_PlaylistStyle
        {
            get => BeatSyncConfig.ScoreSaber.Trending.PlaylistStyle;
            set => BeatSyncConfig.ScoreSaber.Trending.PlaylistStyle = value;
        }
        [UIValue("ScoreSaber.Trending.RankedOnly")]
        public bool ScoreSaber_Trending_RankedOnly
        {
            get => BeatSyncConfig.ScoreSaber.Trending.RankedOnly;
            set => BeatSyncConfig.ScoreSaber.Trending.RankedOnly = value;
        }
        [UIValue("ScoreSaber.Trending.IncludeUnstarred")]
        public bool ScoreSaber_Trending_IncludeUnstarred
        {
            get => BeatSyncConfig.ScoreSaber.Trending.IncludeUnstarred;
            set => BeatSyncConfig.ScoreSaber.Trending.IncludeUnstarred = value;
        }
        [UIValue("ScoreSaber.Trending.MaxStars")]
        public float ScoreSaber_Trending_MaxStars
        {
            get => BeatSyncConfig.ScoreSaber.Trending.MaxStars;
            set => BeatSyncConfig.ScoreSaber.Trending.MaxStars = value;
        }
        [UIValue("ScoreSaber.Trending.MinStars")]
        public float ScoreSaber_Trending_MinStars
        {
            get => BeatSyncConfig.ScoreSaber.Trending.MinStars;
            set => BeatSyncConfig.ScoreSaber.Trending.MinStars = value;
        }
        #endregion
        #region TopPlayed
        [UIValue("ScoreSaber.TopPlayed.Enabled")]
        public bool ScoreSaber_TopPlayed_Enabled
        {
            get => BeatSyncConfig.ScoreSaber.TopPlayed.Enabled;
            set => BeatSyncConfig.ScoreSaber.TopPlayed.Enabled = value;
        }
        [UIValue("ScoreSaber.TopPlayed.MaxSongs")]
        public int ScoreSaber_TopPlayed_MaxSongs
        {
            get => BeatSyncConfig.ScoreSaber.TopPlayed.MaxSongs;
            set => BeatSyncConfig.ScoreSaber.TopPlayed.MaxSongs = value;
        }
        [UIValue("ScoreSaber.TopPlayed.CreatePlaylist")]
        public bool ScoreSaber_TopPlayed_CreatePlaylist
        {
            get => BeatSyncConfig.ScoreSaber.TopPlayed.CreatePlaylist;
            set => BeatSyncConfig.ScoreSaber.TopPlayed.CreatePlaylist = value;
        }
        [UIValue("ScoreSaber.TopPlayed.PlaylistStyle")]
        public PlaylistStyle ScoreSaber_TopPlayed_PlaylistStyle
        {
            get => BeatSyncConfig.ScoreSaber.TopPlayed.PlaylistStyle;
            set => BeatSyncConfig.ScoreSaber.TopPlayed.PlaylistStyle = value;
        }
        [UIValue("ScoreSaber.TopPlayed.RankedOnly")]
        public bool ScoreSaber_TopPlayed_RankedOnly
        {
            get => BeatSyncConfig.ScoreSaber.TopPlayed.RankedOnly;
            set => BeatSyncConfig.ScoreSaber.TopPlayed.RankedOnly = value;
        }
        [UIValue("ScoreSaber.TopPlayed.IncludeUnstarred")]
        public bool ScoreSaber_TopPlayed_IncludeUnstarred
        {
            get => BeatSyncConfig.ScoreSaber.TopPlayed.IncludeUnstarred;
            set => BeatSyncConfig.ScoreSaber.TopPlayed.IncludeUnstarred = value;
        }
        [UIValue("ScoreSaber.TopPlayed.MaxStars")]
        public float ScoreSaber_TopPlayed_MaxStars
        {
            get => BeatSyncConfig.ScoreSaber.TopPlayed.MaxStars;
            set => BeatSyncConfig.ScoreSaber.TopPlayed.MaxStars = value;
        }
        [UIValue("ScoreSaber.TopPlayed.MinStars")]
        public float ScoreSaber_TopPlayed_MinStars
        {
            get => BeatSyncConfig.ScoreSaber.TopPlayed.MinStars;
            set => BeatSyncConfig.ScoreSaber.TopPlayed.MinStars = value;
        }
        #endregion
        #endregion
        #endregion
    }
}
