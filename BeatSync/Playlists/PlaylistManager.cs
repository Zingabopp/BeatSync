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
            MasterList = new ConcurrentDictionary<string, PlaylistSong>();
        }
        private const string PlaylistPath = @"Playlists";
        public static ConcurrentDictionary<string, PlaylistSong> MasterList { get; }

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

        public static Dictionary<string, Playlist> AvailablePlaylists = new Dictionary<string, Playlist>();

        public static string GetPlaylistId(BuiltInPlaylist builtInPlaylist)
        {
            return DefaultPlaylists.Keys.ElementAt((int)builtInPlaylist);
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
                    foreach(var song in newPlaylist.Songs)
                    {
                        song.AddPlaylist(newPlaylist);
                        MasterList.AddOrUpdate(song.Hash, song, (hash, existingSong) =>
                        {
                            existingSong.AddPlaylist(newPlaylist);
                            return existingSong;
                        });
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
