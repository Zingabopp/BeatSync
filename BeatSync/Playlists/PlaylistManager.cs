using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using BeatSync;

namespace BeatSync.Playlists
{
    public static class PlaylistManager
    {
        private const string PlaylistPath = @"Playlists";

        public static Dictionary<string, Playlist> DefaultPlaylists = new Dictionary<string, Playlist>()
        {
            {"BeatSyncPlaylist", new Playlist("BeatSyncPlaylist", "BeatSync Playlist", "BeatSync", "1") },
            {"BeatSyncBSaberBookmarks", new Playlist("BeatSyncBSaberBookmarks", "BeastSaber Bookmarks", "BeatSync", "1") },
            {"BeatSyncBSaberFollows", new Playlist("BeatSyncBSaberFollows", "BeastSaber Follows", "BeatSync", "1") },
            {"BeatSyncBSaberCuratorRecommended", new Playlist("BeatSyncBSaberCuratorRecommended", "Curator Recommended", "BeatSync", "1") },
            {"BeatSyncScoreSaberTopRanked", new Playlist("BeatSyncScoreSaberTopRanked", "ScoreSaber Top Ranked", "BeatSync", "1") },
            {"BeatSyncFavoriteMappers", new Playlist("BeatSyncFavoriteMappers", "Favorite Mappers", "BeatSync", "1") }
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
            //var legScoreSaber = ReadPlaylist("ScoreSaberTopRanked.json");
            //var legBookmarks = ReadPlaylist("SyncSaberBookmarksPlaylist.json");
            //var legCurator = ReadPlaylist("SyncSaberCuratorRecommendedPlaylist.json");
            //var legFollows = ReadPlaylist("SyncSaberFollowingsPlaylist.json");
            //var legAll = ReadPlaylist("SyncSaberPlaylist.json");
            //AvailablePlaylists["BeatSyncScoreSaberTopRanked"].Songs = legScoreSaber.Songs;
            //AvailablePlaylists["BeatSyncBSaberBookmarks"].Songs = legBookmarks.Songs;
            //AvailablePlaylists["BeatSyncBSaberCuratorRecommended"].Songs = legCurator.Songs;
            //AvailablePlaylists["BeatSyncBSaberFollows"].Songs = legFollows.Songs;
            //AvailablePlaylists["BeatSyncPlaylist"].Songs = legAll.Songs;
            foreach (var playlistPair in LegacyPlaylists)
            {
                var legPlaylist = FileIO.ReadPlaylist(playlistPair.Value);
                var songCount = legPlaylist?.Songs.Count ?? 0;
                if (songCount > 0)
                {
                    var newPlaylist = DefaultPlaylists.Values.ElementAt(playlistPair.Key);
                    newPlaylist.Songs = newPlaylist.Songs.Union(legPlaylist.Songs).ToList();
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
        FavoriteMappers = 5
    }
}
