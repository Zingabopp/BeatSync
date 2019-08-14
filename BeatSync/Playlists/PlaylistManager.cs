using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BeatSync;
using System.Collections.Concurrent;

namespace BeatSync.Playlists
{
    public static class PlaylistManager
    {
        static PlaylistManager()
        {
            AvailablePlaylists = new ConcurrentDictionary<string, Playlist>();
            AvailablePlaylists.TryAdd("BeatSyncPlaylist", null);
            AvailablePlaylists.TryAdd("BeatSyncBSaberBookmarks", null);
            AvailablePlaylists.TryAdd("BeatSyncBSaberFollows", null);
            AvailablePlaylists.TryAdd("BeatSyncBSaberCuratorRecommended", null);
            AvailablePlaylists.TryAdd("BeatSyncScoreSaberTopRanked", null);
            AvailablePlaylists.TryAdd("BeatSyncFavoriteMappers", null);
            AvailablePlaylists.TryAdd("BeatSyncRecent", null);
        }
        private const string PlaylistPath = @"Playlists";

        private static ConcurrentDictionary<string, Playlist> AvailablePlaylists;

        public static Dictionary<string, Playlist> DefaultPlaylists = new Dictionary<string, Playlist>()
        {
            {"BeatSyncPlaylist", new Playlist("BeatSyncPlaylist", "BeatSync Playlist", "BeatSync", "1") },
            {"BeatSyncBSaberBookmarks", new Playlist("BeatSyncBSaberBookmarks", "BeastSaber Bookmarks", "BeatSync", "1") },
            {"BeatSyncBSaberFollows", new Playlist("BeatSyncBSaberFollows", "BeastSaber Follows", "BeatSync", "1") },
            {"BeatSyncBSaberCuratorRecommended", new Playlist("BeatSyncBSaberCuratorRecommended", "Curator Recommended", "BeatSync", "1") },
            {"BeatSyncScoreSaberTopRanked", new Playlist("BeatSyncScoreSaberTopRanked", "ScoreSaber Top Ranked", "BeatSync", "1") },
            {"BeatSyncFavoriteMappers", new Playlist("BeatSyncFavoriteMappers", "Favorite Mappers", "BeatSync", "1") },
            {"BeatSyncRecent", new Playlist("BeatSyncRecent", "BeatSync Recent Songs", "BeatSync", "1") }
        };

        public static Dictionary<int, Playlist> LegacyPlaylists = new Dictionary<int, Playlist>()
        {
            { 0, new Playlist("SyncSaberPlaylist", "SyncSaber Playlist", "SyncSaber", "1") },
            { 1, new Playlist("SyncSaberBookmarksPlaylist", "BeastSaber Bookmarks", "brian91292", "1") },
            { 2, new Playlist("SyncSaberFollowingsPlaylist", "BeastSaber Followings", "brian91292", "1") },
            { 3, new Playlist("SyncSaberCuratorRecommendedPlaylist", "BeastSaber Curator Recommended", "brian91292", "1") },
            { 4, new Playlist("ScoreSaberTopRanked", "ScoreSaber Top Ranked", "SyncSaber", "1") }
        };


        /// <summary>
        /// Retrieves the specified playlist. If the playlist doesn't exist, creates one using the default.
        /// </summary>
        /// <param name="builtInPlaylist"></param>
        /// <returns></returns>
        public static Playlist GetPlaylist(BuiltInPlaylist builtInPlaylist)
        {
            Playlist playlist = null;
            var key = AvailablePlaylists.Keys.ElementAt((int)builtInPlaylist);
            if (AvailablePlaylists.TryGetValue(key, out playlist))
            {
                if (playlist == null)
                {
                    var path = FileIO.GetPlaylistFilePath(key);
                    if (string.IsNullOrEmpty(path))
                    {
                        var defPlaylist = DefaultPlaylists[key];
                        AvailablePlaylists.TryUpdate(key, defPlaylist, null);
                        return defPlaylist;
                    }
                    else
                        playlist = JsonConvert.DeserializeObject<Playlist>(File.ReadAllText(path));
                }
            }
            return playlist;
        }



        public static void ConvertLegacyPlaylists()
        {
            foreach (var playlistPair in LegacyPlaylists)
            {
                var legPlaylist = FileIO.ReadPlaylist(playlistPair.Value);
                var songCount = legPlaylist?.Songs.Count ?? 0;
                if (songCount > 0)
                {
                    var newPlaylist = DefaultPlaylists.Values.ElementAt(playlistPair.Key);
                    newPlaylist.Songs = newPlaylist.Songs.Union(legPlaylist.Songs).ToList();
                    foreach (var song in newPlaylist.Songs)
                    {
                        song.AddPlaylist(newPlaylist);
                        //MasterList.AddOrUpdate(song.Hash, song, (hash, existingSong) =>
                        //{
                        //    existingSong.AddPlaylist(newPlaylist);
                        //    return existingSong;
                        //});
                    }
                    FileIO.WritePlaylist(DefaultPlaylists.Values.ElementAt(playlistPair.Key));
                }
            }
        }
    }

    public enum BuiltInPlaylist
    {
        BeatSyncAll = 0,
        BeastSaberBookmarks = 1,
        BeastSaberFollows = 2,
        BeastSaberCurator = 3,
        ScoreSaberTopRanked = 4,
        FavoriteMappers = 5,
        BeatSyncRecent = 6
    }
}
