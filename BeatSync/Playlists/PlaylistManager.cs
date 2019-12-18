using BeatSync.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace BeatSync.Playlists
{
    public static class PlaylistManager
    {
        public const string PlaylistPath = @"Playlists";
        public static readonly string ConvertedPlaylistPath = Path.Combine(PlaylistPath, "ConvertedSyncSaber");
        public static readonly string DisabledPlaylistsPath = Path.Combine(PlaylistPath, "DisabledPlaylists");
        public static readonly string[] PlaylistExtensions = new string[] { ".blist", ".bplist", ".json" };

        private static Dictionary<BuiltInPlaylist, Playlist> AvailablePlaylists = new Dictionary<BuiltInPlaylist, Playlist>(); // Doesn't need to be concurrent, basically readonly

        /// <summary>
        /// Key is the file name in lowercase.
        /// </summary>
        private static ConcurrentDictionary<string, Playlist> CustomPlaylists = new ConcurrentDictionary<string, Playlist>();


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

        public static readonly ReadOnlyDictionary<BuiltInPlaylist, Playlist> DefaultPlaylists = new ReadOnlyDictionary<BuiltInPlaylist, Playlist>(new Dictionary<BuiltInPlaylist, Playlist>()
        {
            {BuiltInPlaylist.BeatSyncAll, new Playlist("BeatSyncPlaylist.blist", "BeatSync Playlist", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSyncAll]) },
            {BuiltInPlaylist.BeastSaberBookmarks, new Playlist("BeatSyncBSaberBookmarks.blist", "BeastSaber Bookmarks", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberBookmarks]) },
            {BuiltInPlaylist.BeastSaberFollows, new Playlist("BeatSyncBSaberFollows.blist", "BeastSaber Follows", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberFollows]) },
            {BuiltInPlaylist.BeastSaberCurator, new Playlist("BeatSyncBSaberCuratorRecommended.blist", "Curator Recommended", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberCurator]) },
            {BuiltInPlaylist.ScoreSaberTopRanked, new Playlist("BeatSyncScoreSaberTopRanked.blist", "ScoreSaber Top Ranked", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTopRanked]) },
            {BuiltInPlaylist.ScoreSaberLatestRanked, new Playlist("BeatSyncScoreSaberLatestRanked.blist", "ScoreSaber Latest Ranked", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberLatestRanked]) },
            {BuiltInPlaylist.ScoreSaberTopPlayed, new Playlist("BeatSyncScoreSaberTopPlayed.blist", "ScoreSaber Top Played", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTopPlayed]) },
            {BuiltInPlaylist.ScoreSaberTrending, new Playlist("BeatSyncScoreSaberTrending.blist", "ScoreSaber Trending", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTrending]) },
            {BuiltInPlaylist.BeatSaverFavoriteMappers, new Playlist("BeatSyncFavoriteMappers.blist", "Favorite Mappers", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverFavoriteMappers]) },
            {BuiltInPlaylist.BeatSaverLatest, new Playlist("BeatSyncBeatSaverLatest.blist", "BeatSaver Latest", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverLatest]) },
            {BuiltInPlaylist.BeatSaverHot, new Playlist("BeatSyncBeatSaverHot.blist", "Beat Saver Hot", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverHot]) },
            {BuiltInPlaylist.BeatSaverPlays, new Playlist("BeatSyncBeatSaverPlays.blist", "Beat Saver Plays", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverPlays]) },
            {BuiltInPlaylist.BeatSaverDownloads, new Playlist("BeatSyncBeatSaverDownloads.blist", "Beat Saver Downloads", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverDownloads]) },
            {BuiltInPlaylist.BeatSyncRecent, new Playlist("BeatSyncRecent.blist", "BeatSync Recent Songs", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSyncRecent]) }
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
        public static void RemoveSongFromAll(PlaylistSong song)
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
                    Logger.log?.Debug($"Writing {playlist.FileName} to file.");
                    playlist.TryWriteFile();
                }
            }
            var customPlaylistKeys = CustomPlaylists.Keys;
            foreach (var key in customPlaylistKeys)
            {
                if (CustomPlaylists[key].IsDirty)
                {
                    Logger.log?.Debug($"Writing {CustomPlaylists[key].FileName} to file.");
                    CustomPlaylists[key].TryWriteFile();
                }
            }
        }

        /// <summary>
        /// Retrieves the specified playlist. If the playlist doesn't exist, creates one using the default.
        /// </summary>
        /// <param name="builtInPlaylist"></param>
        /// <returns></returns>
        public static Playlist GetPlaylist(BuiltInPlaylist builtInPlaylist)
        {
            Playlist playlist = null;
            bool playlistExists = AvailablePlaylists.TryGetValue(builtInPlaylist, out playlist);
            if (!playlistExists || playlist == null)
            {
                var defPlaylist = DefaultPlaylists[builtInPlaylist];
                var path = FileIO.GetPlaylistFilePath(defPlaylist.FileName);
                if (string.IsNullOrEmpty(path)) // If GetPlaylistFilePath returned null, the file doesn't exist
                {
                    //if (AvailablePlaylists[builtInPlaylist] == null)
                    //    AvailablePlaylists[builtInPlaylist] = defPlaylist;
                    playlist = defPlaylist;
                }
                else
                {
                    playlist = FileIO.ReadPlaylist(path);
                    playlist.FileName = path;
                    //if (playlist.Cover == new byte[] { (byte)'1' })
                    //{
                    //    playlist.ImageLoader = PlaylistImageLoaders[builtInPlaylist];
                    //}
                    Logger.log?.Debug($"Playlist loaded from file: {playlist.FileName} with {playlist.Count} songs.");
                }
                AvailablePlaylists.Add(builtInPlaylist, playlist);
            }
            Logger.log?.Debug($"Returning {playlist?.FileName}: {playlist?.Title} for {builtInPlaylist.ToString()} with {playlist?.Count} songs.");
            return playlist;
        }

        /// <summary>
        /// Retrieves the specified playlist. If the playlist doesn't exist, returns null.
        /// </summary>
        /// <param name="builtInPlaylist"></param>
        /// <returns></returns>
        public static Playlist GetPlaylist(string playlistFileName)
        {
            Playlist playlist = null;
            // Check if the playlist is one of the built in ones.
            foreach (var defaultPlaylist in DefaultPlaylists)
            {
                if (defaultPlaylist.Value.FileName == playlistFileName)
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
                    playlist = FileIO.ReadPlaylist(existingFile);
                    playlist.FileName = playlistFileName;
                    CustomPlaylists.TryAdd(playlistFileName, playlist);
                    Logger.log?.Debug($"Playlist FileName is {playlist.FileName}");
                }
            }
            Logger.log?.Debug($"Returning {playlist?.FileName}: {playlist?.Title} with {playlist?.Count} songs.");
            return playlist;
        }

        public static bool TryAdd(Playlist playlist)
        {
            return CustomPlaylists.TryAdd(playlist.FileName.ToLower(), playlist);
        }

        public static Playlist GetOrAdd(string playlistFileName, Func<Playlist> newPlaylist)
        {
            var playlist = GetPlaylist(playlistFileName);
            if (playlist == null)
            {
                playlist = newPlaylist();
                if (playlist != null)
                {
                    if (!string.IsNullOrEmpty(playlist.FileName))
                        CustomPlaylists.TryAdd(playlist.FileName?.ToLower() ?? "", playlist);
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
