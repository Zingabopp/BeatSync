using BeatSyncLib.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BeatSyncLib.Playlists.Legacy;

namespace BeatSyncLib.Playlists
{
    public static class PlaylistManager
    {
        public const string PlaylistPath = @"Playlists";
        public static readonly string ConvertedPlaylistPath = Path.Combine(PlaylistPath, "ConvertedSyncSaber");
        public static readonly string DisabledPlaylistsPath = Path.Combine(PlaylistPath, "DisabledPlaylists");
        public static readonly string[] PlaylistExtensions = new string[] { ".blist", ".bplist", ".json" };

        private static Dictionary<BuiltInPlaylist, IPlaylist> AvailablePlaylists = new Dictionary<BuiltInPlaylist, IPlaylist>(); // Doesn't need to be concurrent, basically readonly

        /// <summary>
        /// Key is the file name in lowercase.
        /// </summary>
        private static ConcurrentDictionary<string, IPlaylist> CustomPlaylists = new ConcurrentDictionary<string, IPlaylist>();


        public static Lazy<string> GetImageLoader(string resourcePath)
        {
            return new Lazy<string>(() => Util.ImageToBase64(resourcePath));
        }

        public static readonly ReadOnlyDictionary<BuiltInPlaylist, Lazy<string>> PlaylistImageLoaders = new ReadOnlyDictionary<BuiltInPlaylist, Lazy<string>>(new Dictionary<BuiltInPlaylist, Lazy<string>>()
        {
            {BuiltInPlaylist.BeatSyncAll, GetImageLoader("BeatSync.Icons.Playlists.BeatSync.BeatSyncAll.png") },
            {BuiltInPlaylist.BeatSyncRecent, GetImageLoader("BeatSync.Icons.Playlists.BeatSync.BeatSyncRecent.png") },
            {BuiltInPlaylist.BeastSaberBookmarks, GetImageLoader("BeatSync.Icons.Playlists.BeastSaber.BSaberBookmarks.png") },
            {BuiltInPlaylist.BeastSaberFollows, GetImageLoader("BeatSync.Icons.Playlists.BeastSaber.BSaberFollows.png") },
            {BuiltInPlaylist.BeastSaberCurator, GetImageLoader("BeatSync.Icons.Playlists.BeastSaber.BSaberCurator.png") },
            {BuiltInPlaylist.ScoreSaberTopRanked, GetImageLoader("BeatSync.Icons.Playlists.ScoreSaber.ScoreSaberTopRanked.png") },
            {BuiltInPlaylist.ScoreSaberLatestRanked, GetImageLoader("BeatSync.Icons.Playlists.ScoreSaber.ScoreSaberLatestRanked.png") },
            {BuiltInPlaylist.ScoreSaberTopPlayed, GetImageLoader("BeatSync.Icons.Playlists.ScoreSaber.ScoreSaberTopPlayed.png") },
            {BuiltInPlaylist.ScoreSaberTrending, GetImageLoader("BeatSync.Icons.Playlists.ScoreSaber.ScoreSaberTrending.png") },
            {BuiltInPlaylist.BeatSaverFavoriteMappers, GetImageLoader("BeatSync.Icons.Playlists.BeatSaver.BeatSaverFavoriteMappers.png") },
            {BuiltInPlaylist.BeatSaverLatest, GetImageLoader("BeatSync.Icons.Playlists.BeatSaver.BeatSaverLatest.png") },
            {BuiltInPlaylist.BeatSaverHot, GetImageLoader("BeatSync.Icons.Playlists.BeatSaver.BeatSaverHot.png") },
            {BuiltInPlaylist.BeatSaverPlays, GetImageLoader("BeatSync.Icons.Playlists.BeatSaver.BeatSaverPlays.png") },
            {BuiltInPlaylist.BeatSaverDownloads, GetImageLoader("BeatSync.Icons.Playlists.BeatSaver.BeatSaverDownloads.png") },
            {BuiltInPlaylist.BeatSaverMapper, GetImageLoader("BeatSync.Icons.Playlists.BeatSaver.BeatSaverMapper.png") }
        });

        public static readonly ReadOnlyDictionary<BuiltInPlaylist, IPlaylist> DefaultPlaylists = new ReadOnlyDictionary<BuiltInPlaylist, IPlaylist>(new Dictionary<BuiltInPlaylist, IPlaylist>()
        {
            {BuiltInPlaylist.BeatSyncAll, new LegacyPlaylist("BeatSyncPlaylist.blist", "BeatSync Playlist", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSyncAll]) },
            {BuiltInPlaylist.BeastSaberBookmarks, new LegacyPlaylist("BeatSyncBSaberBookmarks.blist", "BeastSaber Bookmarks", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberBookmarks]) },
            {BuiltInPlaylist.BeastSaberFollows, new LegacyPlaylist("BeatSyncBSaberFollows.blist", "BeastSaber Follows", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberFollows]) },
            {BuiltInPlaylist.BeastSaberCurator, new LegacyPlaylist("BeatSyncBSaberCuratorRecommended.blist", "Curator Recommended", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberCurator]) },
            {BuiltInPlaylist.ScoreSaberTopRanked, new LegacyPlaylist("BeatSyncScoreSaberTopRanked.blist", "ScoreSaber Top Ranked", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTopRanked]) },
            {BuiltInPlaylist.ScoreSaberLatestRanked, new LegacyPlaylist("BeatSyncScoreSaberLatestRanked.blist", "ScoreSaber Latest Ranked", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberLatestRanked]) },
            {BuiltInPlaylist.ScoreSaberTopPlayed, new LegacyPlaylist("BeatSyncScoreSaberTopPlayed.blist", "ScoreSaber Top Played", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTopPlayed]) },
            {BuiltInPlaylist.ScoreSaberTrending, new LegacyPlaylist("BeatSyncScoreSaberTrending.blist", "ScoreSaber Trending", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTrending]) },
            {BuiltInPlaylist.BeatSaverFavoriteMappers, new LegacyPlaylist("BeatSyncFavoriteMappers.blist", "Favorite Mappers", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverFavoriteMappers]) },
            {BuiltInPlaylist.BeatSaverLatest, new LegacyPlaylist("BeatSyncBeatSaverLatest.blist", "BeatSaver Latest", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverLatest]) },
            {BuiltInPlaylist.BeatSaverHot, new LegacyPlaylist("BeatSyncBeatSaverHot.blist", "Beat Saver Hot", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverHot]) },
            {BuiltInPlaylist.BeatSaverPlays, new LegacyPlaylist("BeatSyncBeatSaverPlays.blist", "Beat Saver Plays", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverPlays]) },
            {BuiltInPlaylist.BeatSaverDownloads, new LegacyPlaylist("BeatSyncBeatSaverDownloads.blist", "Beat Saver Downloads", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverDownloads]) },
            {BuiltInPlaylist.BeatSyncRecent, new LegacyPlaylist("BeatSyncRecent.blist", "BeatSync Recent Songs", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSyncRecent]) }
        });


        //public static Dictionary<int, Playlist> LegacyPlaylists = new Dictionary<int, Playlist>()
        //{
        //    { 0, new Playlist("SyncSaberPlaylist.json", "SyncSaber Playlist", "SyncSaber", "1") },
        //    { 1, new Playlist("SyncSaberBookmarksPlaylist.json", "BeastSaber Bookmarks", "brian91292", "1") },
        //    { 2, new Playlist("SyncSaberFollowingsPlaylist.json", "BeastSaber Followings", "brian91292", "1") },
        //    { 3, new Playlist("SyncSaberCuratorRecommendedPlaylist.json", "BeastSaber Curator Recommended", "brian91292", "1") },
        //    { 4, new Playlist("ScoreSaberTopRanked.json", "ScoreSaber Top Ranked", "SyncSaber", "1") }
        //};

        /// <summary>
        /// Attempts to remove the song with the matching hash from all loaded playlists.
        /// </summary>
        /// <param name="hash"></param>
        public static void RemoveSongFromAll(string hash)
        {
            hash = hash.ToUpper();
            foreach (var playlist in AvailablePlaylists.Values)
            {
                if (playlist == null)
                    continue;
                playlist.TryRemove(hash);
            }
            var customPlaylistKeys = CustomPlaylists.Keys;
            foreach (var key in customPlaylistKeys)
            {
                CustomPlaylists[key].TryRemove(hash);
            }
        }

        /// <summary>
        /// Attempts to remove the song from all loaded playlists.
        /// </summary>
        /// <param name="song"></param>
        public static void RemoveSongFromAll(IPlaylistSong song)
        {
            RemoveSongFromAll(song.Hash);
        }

        public static void WriteAllPlaylists()
        {
            foreach (var playlist in AvailablePlaylists.Values)
            {
                if (playlist == null)
                    continue;
                if (playlist.IsDirty)
                {
                    Logger.log?.Debug($"Writing {playlist.FilePath} to file.");
                    playlist.TryStore();
                }
            }
            var customPlaylistKeys = CustomPlaylists.Keys;
            foreach (var key in customPlaylistKeys)
            {
                if (CustomPlaylists[key].IsDirty)
                {
                    Logger.log?.Debug($"Writing {CustomPlaylists[key].FilePath} to file.");
                    CustomPlaylists[key].TryStore();
                }
            }
        }

        /// <summary>
        /// Retrieves the specified playlist. If the playlist doesn't exist, creates one using the default.
        /// </summary>
        /// <param name="builtInPlaylist"></param>
        /// <returns></returns>
        public static IPlaylist GetPlaylist(BuiltInPlaylist builtInPlaylist)
        {
            IPlaylist playlist = null;
            bool playlistExists = AvailablePlaylists.TryGetValue(builtInPlaylist, out playlist);
            if (!playlistExists || playlist == null)
            {
                var defPlaylist = DefaultPlaylists[builtInPlaylist];
                var path = FileIO.GetPlaylistFilePath(defPlaylist.FilePath);
                if (string.IsNullOrEmpty(path)) // If GetPlaylistFilePath returned null, the file doesn't exist
                {
                    //if (AvailablePlaylists[builtInPlaylist] == null)
                    //    AvailablePlaylists[builtInPlaylist] = defPlaylist;
                    playlist = defPlaylist;
                }
                else
                {
                    playlist = FileIO.ReadPlaylist<LegacyPlaylist>(path);
                    playlist.FilePath = path;
                    //if (playlist.Cover == new byte[] { (byte)'1' })
                    //{
                    //    playlist.ImageLoader = PlaylistImageLoaders[builtInPlaylist];
                    //}
                    Logger.log?.Debug($"Playlist loaded from file: {playlist.FilePath} with {playlist.Count} songs.");
                }
                AvailablePlaylists.Add(builtInPlaylist, playlist);
            }
            Logger.log?.Debug($"Returning {playlist?.FilePath}: {playlist?.Title} for {builtInPlaylist.ToString()} with {playlist?.Count} songs.");
            return playlist;
        }

        /// <summary>
        /// Retrieves the specified playlist. If the playlist doesn't exist, returns null.
        /// </summary>
        /// <param name="builtInPlaylist"></param>
        /// <returns></returns>
        public static IPlaylist GetPlaylist(string playlistFileName)
        {
            IPlaylist playlist = null;
            // Check if the playlist is one of the built in ones.
            foreach (var defaultPlaylist in DefaultPlaylists)
            {
                if (defaultPlaylist.Value.FilePath == playlistFileName)
                {
                    playlist = GetPlaylist(defaultPlaylist.Key);
                }
            }


            // Check if this playlist exists in CustomPlaylists
            if (playlist == null && CustomPlaylists.ContainsKey(playlistFileName.ToLower()))
            {
                playlist = CustomPlaylists[playlistFileName.ToLower()];
            }

            // Check if the playlistFileName exists
            if (playlist == null)
            {
                var existingFile = FileIO.GetPlaylistFilePath(playlistFileName);
                if (!string.IsNullOrEmpty(existingFile))
                {
                    playlist = FileIO.ReadPlaylist<LegacyPlaylist>(existingFile);
                    playlist.FilePath = playlistFileName;
                    CustomPlaylists.TryAdd(playlistFileName, playlist);
                    Logger.log?.Debug($"Playlist FileName is {playlist.FilePath}");
                }
            }
            Logger.log?.Debug($"Returning {playlist?.FilePath}: {playlist?.Title} with {playlist?.Count} songs.");
            return playlist;
        }

        public static bool TryAdd(IPlaylist playlist)
        {
            return CustomPlaylists.TryAdd(playlist.FilePath.ToLower(), playlist);
        }

        public static IPlaylist GetOrAdd(string playlistFileName, Func<IPlaylist> newPlaylist)
        {
            var playlist = GetPlaylist(playlistFileName);
            if (playlist == null)
            {
                playlist = newPlaylist();
                if (playlist != null)
                {
                    if (!string.IsNullOrEmpty(playlist.FilePath))
                        CustomPlaylists.TryAdd(playlist.FilePath?.ToLower() ?? "", playlist);
                    else
                        Logger.log?.Warn($"Invalid playlist file name in playlist function given to PlaylistManager.GetOrAdd()");
                }
                else
                    Logger.log?.Warn($"Playlist function returned a null playlist in PlaylistManager.GetOrAdd()");
            }
            return playlist;
        }


        //public static void ConvertLegacyPlaylists()
        //{
        //    foreach (var playlistPair in LegacyPlaylists)
        //    {
        //        var legPath = FileIO.GetPlaylistFilePath(playlistPair.Value.FileName);
        //        var legPlaylist = FileIO.ReadPlaylist(playlistPair.Value);
        //        var songCount = legPlaylist?.Songs.Count ?? 0;
        //        if (songCount > 0)
        //        {
        //            var newPlaylist = DefaultPlaylists.Values.ElementAt(playlistPair.Key);
        //            newPlaylist.Songs = newPlaylist.Songs.Union(legPlaylist.Songs).ToList();
        //            foreach (var song in newPlaylist.Songs)
        //            {
        //                song.AddPlaylist(newPlaylist);
        //                //MasterList.AddOrUpdate(song.Hash, song, (hash, existingSong) =>
        //                //{
        //                //    existingSong.AddPlaylist(newPlaylist);
        //                //    return existingSong;
        //                //});
        //            }
        //            FileIO.WritePlaylist(DefaultPlaylists.Values.ElementAt(playlistPair.Key));

        //        }

        //        if (File.Exists(legPath))
        //        {
        //            Directory.CreateDirectory(ConvertedPlaylistPath);
        //            File.Move(legPath, Path.Combine(ConvertedPlaylistPath, Path.GetFileName(legPath)));
        //        }
        //    }
        //}
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
