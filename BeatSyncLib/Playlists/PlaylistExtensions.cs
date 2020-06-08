using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;

namespace BeatSyncLib.Playlists
{
    public static class PlaylistExtensions
    {
        public static readonly string AuthorPlaylistPrefix = "BeatSyncAuthor_";
        public static Lazy<string> GetImageLoader(string resourcePath)
        {
            return new Lazy<string>(() => BeatSaberPlaylistsLib.Utilities.ImageToBase64(resourcePath));
        }

        public static readonly ReadOnlyDictionary<BuiltInPlaylist, Lazy<string>> PlaylistImageLoaders = new ReadOnlyDictionary<BuiltInPlaylist, Lazy<string>>(new Dictionary<BuiltInPlaylist, Lazy<string>>()
        {
            {BuiltInPlaylist.BeatSyncAll, GetImageLoader("BeatSyncPlaylists.Icons.BeatSync.BeatSyncAll.png") },
            {BuiltInPlaylist.BeatSyncRecent, GetImageLoader("BeatSyncPlaylists.Icons.BeatSync.BeatSyncRecent.png") },
            {BuiltInPlaylist.BeastSaberBookmarks, GetImageLoader("BeatSyncPlaylists.Icons.BeastSaber.BSaberBookmarks.png") },
            {BuiltInPlaylist.BeastSaberFollows, GetImageLoader("BeatSyncPlaylists.Icons.BeastSaber.BSaberFollows.png") },
            {BuiltInPlaylist.BeastSaberCurator, GetImageLoader("BeatSyncPlaylists.Icons.BeastSaber.BSaberCurator.png") },
            {BuiltInPlaylist.ScoreSaberTopRanked, GetImageLoader("BeatSyncPlaylists.Icons.ScoreSaber.ScoreSaberTopRanked.png") },
            {BuiltInPlaylist.ScoreSaberLatestRanked, GetImageLoader("BeatSyncPlaylists.Icons.ScoreSaber.ScoreSaberLatestRanked.png") },
            {BuiltInPlaylist.ScoreSaberTopPlayed, GetImageLoader("BeatSyncPlaylists.Icons.ScoreSaber.ScoreSaberTopPlayed.png") },
            {BuiltInPlaylist.ScoreSaberTrending, GetImageLoader("BeatSyncPlaylists.Icons.ScoreSaber.ScoreSaberTrending.png") },
            {BuiltInPlaylist.BeatSaverFavoriteMappers, GetImageLoader("BeatSyncPlaylists.Icons.BeatSaver.BeatSaverFavoriteMappers.png") },
            {BuiltInPlaylist.BeatSaverLatest, GetImageLoader("BeatSyncPlaylists.Icons.BeatSaver.BeatSaverLatest.png") },
            {BuiltInPlaylist.BeatSaverHot, GetImageLoader("BeatSyncPlaylists.Icons.BeatSaver.BeatSaverHot.png") },
            {BuiltInPlaylist.BeatSaverPlays, GetImageLoader("BeatSyncPlaylists.Icons.BeatSaver.BeatSaverPlays.png") },
            {BuiltInPlaylist.BeatSaverDownloads, GetImageLoader("BeatSyncPlaylists.Icons.BeatSaver.BeatSaverDownloads.png") },
            {BuiltInPlaylist.BeatSaverMapper, GetImageLoader("BeatSyncPlaylists.Icons.BeatSaver.BeatSaverMapper.png") }
        });

        public static string GetBuiltinPlaylistFilename(BuiltInPlaylist builtInPlaylist)
        {
            return builtInPlaylist switch
            {
                BuiltInPlaylist.BeatSyncAll => "BeatSyncPlaylist",
                BuiltInPlaylist.BeastSaberBookmarks => "BeatSyncBSaberBookmarks",
                BuiltInPlaylist.BeastSaberFollows => "BeatSyncBSaberFollows",
                BuiltInPlaylist.BeastSaberCurator => "BeatSyncBSaberCuratorRecommended",
                BuiltInPlaylist.ScoreSaberTopRanked => "BeatSyncScoreSaberTopRanked",
                BuiltInPlaylist.ScoreSaberLatestRanked => "BeatSyncScoreSaberLatestRanked",
                BuiltInPlaylist.ScoreSaberTopPlayed => "BeatSyncScoreSaberTopPlayed",
                BuiltInPlaylist.ScoreSaberTrending => "BeatSyncScoreSaberTrending",
                BuiltInPlaylist.BeatSaverFavoriteMappers => "BeatSyncFavoriteMappers",
                BuiltInPlaylist.BeatSaverLatest => "BeatSyncBeatSaverLatest",
                BuiltInPlaylist.BeatSaverHot => "BeatSyncBeatSaverHot",
                BuiltInPlaylist.BeatSaverPlays => "BeatSyncBeatSaverPlays",
                BuiltInPlaylist.BeatSaverDownloads => "BeatSyncBeatSaverDownloads",
                BuiltInPlaylist.BeatSaverMapper => throw new ArgumentException("BeatSaverMapper not a supported type for this method", nameof(builtInPlaylist)),
                BuiltInPlaylist.BeatSyncRecent => "BeatSyncRecent",
                _ => throw new ArgumentException($"{builtInPlaylist} BuiltInPlaylist type is not supported.")
            };
        }

        private static IPlaylist CreateBuiltinPlaylist(this PlaylistManager manager, BuiltInPlaylist builtInPlaylist)
        {

            IPlaylist playlist = builtInPlaylist switch
            {
                BuiltInPlaylist.BeatSyncAll => manager.CreatePlaylist("BeatSyncPlaylist", "BeatSync Playlist", "BeatSync",
                                       PlaylistImageLoaders[BuiltInPlaylist.BeatSyncAll], "Every song BeatSync has downloaded."),
                BuiltInPlaylist.BeastSaberBookmarks => manager.CreatePlaylist("BeatSyncBSaberBookmarks", "BeastSaber Bookmarks", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberBookmarks]),
                BuiltInPlaylist.BeastSaberFollows => manager.CreatePlaylist("BeatSyncBSaberFollows", "BeastSaber Follows", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberFollows]),
                BuiltInPlaylist.BeastSaberCurator => manager.CreatePlaylist("BeatSyncBSaberCuratorRecommended", "Curator Recommended", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberCurator]),
                BuiltInPlaylist.ScoreSaberTopRanked => manager.CreatePlaylist("BeatSyncScoreSaberTopRanked", "ScoreSaber Top Ranked", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTopRanked]),
                BuiltInPlaylist.ScoreSaberLatestRanked => manager.CreatePlaylist("BeatSyncScoreSaberLatestRanked", "ScoreSaber Latest Ranked", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberLatestRanked]),
                BuiltInPlaylist.ScoreSaberTopPlayed => manager.CreatePlaylist("BeatSyncScoreSaberTopPlayed", "ScoreSaber Top Played", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTopPlayed]),
                BuiltInPlaylist.ScoreSaberTrending => manager.CreatePlaylist("BeatSyncScoreSaberTrending", "ScoreSaber Trending", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTrending]),
                BuiltInPlaylist.BeatSaverFavoriteMappers => manager.CreatePlaylist("BeatSyncFavoriteMappers", "Favorite Mappers", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverFavoriteMappers]),
                BuiltInPlaylist.BeatSaverLatest => manager.CreatePlaylist("BeatSyncBeatSaverLatest", "BeatSaver Latest", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverLatest]),
                BuiltInPlaylist.BeatSaverHot => manager.CreatePlaylist("BeatSyncBeatSaverHot", "Beat Saver Hot", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverHot]),
                BuiltInPlaylist.BeatSaverPlays => manager.CreatePlaylist("BeatSyncBeatSaverPlays", "Beat Saver Plays", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverPlays]),
                BuiltInPlaylist.BeatSaverDownloads => manager.CreatePlaylist("BeatSyncBeatSaverDownloads", "Beat Saver Downloads", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverDownloads]),
                BuiltInPlaylist.BeatSyncRecent => manager.CreatePlaylist("BeatSyncRecent", "BeatSync Recent Songs", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSyncRecent]),
                BuiltInPlaylist.BeatSaverMapper => throw new ArgumentException("BeatSaverMapper not a supported type for this method", nameof(builtInPlaylist)),
                _ => throw new ArgumentException($"{builtInPlaylist} BuiltInPlaylist type is not supported."),
            };
            manager.RegisterPlaylist(playlist);
            return playlist;
        }

        public static IPlaylist GetOrAddPlaylist(this PlaylistManager manager, BuiltInPlaylist builtInPlaylist)
        {
            IPlaylist? playlist = manager.GetPlaylist(GetBuiltinPlaylistFilename(builtInPlaylist));
            if (playlist != null)
                return playlist;
            return manager.CreateBuiltinPlaylist(builtInPlaylist);
        }

        public static IPlaylist GetOrCreateAuthorPlaylist(this PlaylistManager manager, string authorName)
        {
            string fileName = AuthorPlaylistPrefix + authorName;
            if (manager.TryGetPlaylist(fileName, out IPlaylist? existing) && existing != null)
                return existing;
            IPlaylist newPlaylist = manager.CreatePlaylist(fileName, authorName, "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverMapper], $"Beatmaps by {authorName}.");
            manager.RegisterPlaylist(newPlaylist);
            return newPlaylist;
        }

        public static IPlaylistSong? Add(this IPlaylist playlist, SongFeedReaders.Data.ISong song)
        {
            if (song?.Hash == null || song.Hash.Length == 0)
            {
                return null;
            }
            return playlist.Add(song.Hash, song.Name, song.Key, song.LevelAuthorName);
        }
    }


    public enum BuiltInPlaylist
    {
        BeatSyncAll = 0,
        BeastSaberBookmarks = 1,
        BeastSaberFollows = 2,
        BeastSaberCurator = 3,
        ScoreSaberTopRanked = 4,
        ScoreSaberLatestRanked = 5,
        ScoreSaberTopPlayed = 6,
        ScoreSaberTrending = 7,
        BeatSaverFavoriteMappers = 8,
        BeatSaverLatest = 9,
        BeatSaverHot = 10,
        BeatSaverPlays = 11,
        BeatSaverDownloads = 12,
        BeatSaverMapper = 13,
        BeatSyncRecent = 14
    }
}
