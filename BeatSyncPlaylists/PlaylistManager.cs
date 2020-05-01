using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BeatSyncPlaylists.Playlists.Legacy;

namespace BeatSyncPlaylists
{
    public class PlaylistManager
    {
        protected readonly Dictionary<Type, IPlaylistHandler> PlaylistHandlers = new Dictionary<Type, IPlaylistHandler>();
        protected readonly Dictionary<string, IPlaylistHandler> PlaylistExtensionHandlers = new Dictionary<string, IPlaylistHandler>();
        public string PlaylistPath { get; protected set; }
        public string DisabledPlaylistsPath => Path.Combine(PlaylistPath, "DisabledPlaylists");
        public static readonly string[] PlaylistExtensions = new string[] { ".blist", ".bplist", ".json" };
        protected PlaylistManager()
        {
            RegisterHandler(new LegacyPlaylistHandler());
            DefaultPlaylists = new ReadOnlyDictionary<BuiltInPlaylist, IPlaylist>(new Dictionary<BuiltInPlaylist, IPlaylist>()
                {
                    {BuiltInPlaylist.BeatSyncAll, CreatePlaylist("BeatSyncPlaylist.bplist", "BeatSync Playlist", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSyncAll]) },
                    {BuiltInPlaylist.BeastSaberBookmarks, CreatePlaylist("BeatSyncBSaberBookmarks.bplist", "BeastSaber Bookmarks", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberBookmarks]) },
                    {BuiltInPlaylist.BeastSaberFollows, CreatePlaylist("BeatSyncBSaberFollows.bplist", "BeastSaber Follows", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberFollows]) },
                    {BuiltInPlaylist.BeastSaberCurator, CreatePlaylist("BeatSyncBSaberCuratorRecommended.bplist", "Curator Recommended", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberCurator]) },
                    {BuiltInPlaylist.ScoreSaberTopRanked, CreatePlaylist("BeatSyncScoreSaberTopRanked.bplist", "ScoreSaber Top Ranked", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTopRanked]) },
                    {BuiltInPlaylist.ScoreSaberLatestRanked, CreatePlaylist("BeatSyncScoreSaberLatestRanked.bplist", "ScoreSaber Latest Ranked", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberLatestRanked]) },
                    {BuiltInPlaylist.ScoreSaberTopPlayed, CreatePlaylist("BeatSyncScoreSaberTopPlayed.bplist", "ScoreSaber Top Played", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTopPlayed]) },
                    {BuiltInPlaylist.ScoreSaberTrending, CreatePlaylist("BeatSyncScoreSaberTrending.bplist", "ScoreSaber Trending", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTrending]) },
                    {BuiltInPlaylist.BeatSaverFavoriteMappers, CreatePlaylist("BeatSyncFavoriteMappers.bplist", "Favorite Mappers", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverFavoriteMappers]) },
                    {BuiltInPlaylist.BeatSaverLatest, CreatePlaylist("BeatSyncBeatSaverLatest.bplist", "BeatSaver Latest", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverLatest]) },
                    {BuiltInPlaylist.BeatSaverHot, CreatePlaylist("BeatSyncBeatSaverHot.bplist", "Beat Saver Hot", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverHot]) },
                    {BuiltInPlaylist.BeatSaverPlays, CreatePlaylist("BeatSyncBeatSaverPlays.bplist", "Beat Saver Plays", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverPlays]) },
                    {BuiltInPlaylist.BeatSaverDownloads, CreatePlaylist("BeatSyncBeatSaverDownloads.bplist", "Beat Saver Downloads", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverDownloads]) },
                    {BuiltInPlaylist.BeatSyncRecent, CreatePlaylist("BeatSyncRecent.bplist", "BeatSync Recent Songs", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSyncRecent]) }
                });
        }

        public PlaylistManager(string playlistDirectory)
            : this()
        {
            if (string.IsNullOrEmpty(playlistDirectory))
                throw new ArgumentNullException(nameof(playlistDirectory), $"PlaylistManager cannot have a null {nameof(playlistDirectory)}");
            PlaylistPath = Path.GetFullPath(playlistDirectory);
        }

        public void RegisterHandler(IPlaylistHandler playlistHandler)
        {
            if (!PlaylistHandlers.ContainsKey(playlistHandler.HandledType))
                PlaylistHandlers.Add(playlistHandler.HandledType, playlistHandler);
            foreach (string ext in playlistHandler.GetSupportedExtensions())
            {
                if (!PlaylistExtensionHandlers.ContainsKey(ext))
                    PlaylistExtensionHandlers.Add(ext, playlistHandler);
            }
        }

        public void RegisterHandlerForExtension(string extension, IPlaylistHandler playlistHandler)
        {
            extension = extension.TrimStart('.');
            if (!PlaylistHandlers.ContainsKey(playlistHandler.HandledType))
                PlaylistHandlers.Add(playlistHandler.HandledType, playlistHandler);
            if (!PlaylistExtensionHandlers.ContainsKey(extension))
                PlaylistExtensionHandlers.Add(extension, playlistHandler);
            else
                PlaylistExtensionHandlers[extension] = playlistHandler;
        }

        public IPlaylistHandler GetHandlerForExtension(string extension)
        {
            extension.TrimStart('.');
            IPlaylistHandler handler = null;
            PlaylistExtensionHandlers.TryGetValue(extension, out handler);
            return handler;
        }

        private Dictionary<BuiltInPlaylist, IPlaylist> AvailablePlaylists = new Dictionary<BuiltInPlaylist, IPlaylist>(); // Doesn't need to be concurrent, basically readonly

        /// <summary>
        /// Key is the file name in lowercase.
        /// </summary>
        private ConcurrentDictionary<string, IPlaylist> CustomPlaylists = new ConcurrentDictionary<string, IPlaylist>();


        public static Lazy<string> GetImageLoader(string resourcePath)
        {
            return new Lazy<string>(() => Util.ImageToBase64(resourcePath));
        }

        public static readonly ReadOnlyDictionary<BuiltInPlaylist, Lazy<string>> PlaylistImageLoaders = new ReadOnlyDictionary<BuiltInPlaylist, Lazy<string>>(new Dictionary<BuiltInPlaylist, Lazy<string>>()
        {
            {BuiltInPlaylist.BeatSyncAll, GetImageLoader("BeatSyncLib.Icons.Playlists.BeatSync.BeatSyncAll.png") },
            {BuiltInPlaylist.BeatSyncRecent, GetImageLoader("BeatSyncLib.Icons.Playlists.BeatSync.BeatSyncRecent.png") },
            {BuiltInPlaylist.BeastSaberBookmarks, GetImageLoader("BeatSyncLib.Icons.Playlists.BeastSaber.BSaberBookmarks.png") },
            {BuiltInPlaylist.BeastSaberFollows, GetImageLoader("BeatSyncLib.Icons.Playlists.BeastSaber.BSaberFollows.png") },
            {BuiltInPlaylist.BeastSaberCurator, GetImageLoader("BeatSyncLib.Icons.Playlists.BeastSaber.BSaberCurator.png") },
            {BuiltInPlaylist.ScoreSaberTopRanked, GetImageLoader("BeatSyncLib.Icons.Playlists.ScoreSaber.ScoreSaberTopRanked.png") },
            {BuiltInPlaylist.ScoreSaberLatestRanked, GetImageLoader("BeatSyncLib.Icons.Playlists.ScoreSaber.ScoreSaberLatestRanked.png") },
            {BuiltInPlaylist.ScoreSaberTopPlayed, GetImageLoader("BeatSyncLib.Icons.Playlists.ScoreSaber.ScoreSaberTopPlayed.png") },
            {BuiltInPlaylist.ScoreSaberTrending, GetImageLoader("BeatSyncLib.Icons.Playlists.ScoreSaber.ScoreSaberTrending.png") },
            {BuiltInPlaylist.BeatSaverFavoriteMappers, GetImageLoader("BeatSyncLib.Icons.Playlists.BeatSaver.BeatSaverFavoriteMappers.png") },
            {BuiltInPlaylist.BeatSaverLatest, GetImageLoader("BeatSyncLib.Icons.Playlists.BeatSaver.BeatSaverLatest.png") },
            {BuiltInPlaylist.BeatSaverHot, GetImageLoader("BeatSyncLib.Icons.Playlists.BeatSaver.BeatSaverHot.png") },
            {BuiltInPlaylist.BeatSaverPlays, GetImageLoader("BeatSyncLib.Icons.Playlists.BeatSaver.BeatSaverPlays.png") },
            {BuiltInPlaylist.BeatSaverDownloads, GetImageLoader("BeatSyncLib.Icons.Playlists.BeatSaver.BeatSaverDownloads.png") },
            {BuiltInPlaylist.BeatSaverMapper, GetImageLoader("BeatSyncLib.Icons.Playlists.BeatSaver.BeatSaverMapper.png") }
        });

        public readonly ReadOnlyDictionary<BuiltInPlaylist, IPlaylist> DefaultPlaylists;

        public IPlaylist CreatePlaylist(string fileName, string title, string author, Lazy<string> imageLoader)
        {
            return new LegacyPlaylist(fileName, title, author, imageLoader);
        }

        public IPlaylist CreatePlaylist(string fileName, string title, string author, string coverImage)
        {
            return new LegacyPlaylist(fileName, title, author, coverImage);
        }

        /// <summary>
        /// Attempts to remove the song with the matching hash from all loaded playlists.
        /// </summary>
        /// <param name="hash"></param>
        public void RemoveSongFromAll(string hash)
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
        public void RemoveSongFromAll(IPlaylistSong song)
        {
            RemoveSongFromAll(song.Hash);
        }

        public void WriteAllPlaylists()
        {
            foreach (var playlist in AvailablePlaylists.Values)
            {
                if (playlist == null)
                    continue;
                if (playlist.IsDirty)
                {
                    Logger.log?.Debug($"Writing {playlist.Filename} to file.");
                    playlist.TryStore();
                }
            }
            var customPlaylistKeys = CustomPlaylists.Keys;
            foreach (var key in customPlaylistKeys)
            {
                if (CustomPlaylists[key].IsDirty)
                {
                    Logger.log?.Debug($"Writing {CustomPlaylists[key].Filename} to file.");
                    CustomPlaylists[key].TryStore();
                }
            }
        }

        /// <summary>
        /// Retrieves the specified playlist. If the playlist doesn't exist, creates one using the default.
        /// </summary>
        /// <param name="builtInPlaylist"></param>
        /// <returns></returns>
        public IPlaylist GetPlaylist(BuiltInPlaylist builtInPlaylist)
        {
            IPlaylist playlist = null;
            bool playlistExists = AvailablePlaylists.TryGetValue(builtInPlaylist, out playlist);
            if (!playlistExists || playlist == null)
            {
                var defPlaylist = DefaultPlaylists[builtInPlaylist];
                var path = Path.Combine(PlaylistPath, defPlaylist.Filename);
                var extension = Path.GetExtension(path);
                if (!File.Exists(path) || !PlaylistExtensionHandlers.ContainsKey(extension))
                {
                    //if (AvailablePlaylists[builtInPlaylist] == null)
                    //    AvailablePlaylists[builtInPlaylist] = defPlaylist;
                    playlist = defPlaylist;
                }
                else
                {
                    playlist = PlaylistExtensionHandlers[extension].Deserialize(path);
                    if (playlist == null)
                        playlist = defPlaylist;
                    playlist.Filename = path;
                    //if (playlist.Cover == new byte[] { (byte)'1' })
                    //{
                    //    playlist.ImageLoader = PlaylistImageLoaders[builtInPlaylist];
                    //}
                    Logger.log?.Debug($"Playlist loaded from file: {playlist.Filename} with {playlist.Count} songs.");
                }
                AvailablePlaylists.Add(builtInPlaylist, playlist);
            }
            Logger.log?.Debug($"Returning {playlist?.Filename}: {playlist?.Title} for {builtInPlaylist.ToString()} with {playlist?.Count} songs.");
            return playlist;
        }

        /// <summary>
        /// Retrieves the specified playlist. If the playlist doesn't exist, returns null.
        /// </summary>
        /// <param name="builtInPlaylist"></param>
        /// <returns></returns>
        public IPlaylist GetPlaylist(string playlistFileName)
        {
            IPlaylist playlist = null;
            // Check if the playlist is one of the built in ones.
            foreach (var defaultPlaylist in DefaultPlaylists)
            {
                if (defaultPlaylist.Value.Filename == playlistFileName)
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
                var existingFile = Path.Combine(PlaylistPath, playlistFileName);
                string extension = Path.GetExtension(existingFile);
                if (!string.IsNullOrEmpty(existingFile) && PlaylistExtensionHandlers.TryGetValue(extension, out IPlaylistHandler handler))
                {
                    playlist = handler.Deserialize(existingFile);
                    playlist.Filename = playlistFileName;
                    CustomPlaylists.TryAdd(playlistFileName, playlist);
                    Logger.log?.Debug($"Playlist FileName is {playlist.Filename}");
                }
            }
            Logger.log?.Debug($"Returning {playlist?.Filename}: {playlist?.Title} with {playlist?.Count} songs.");
            return playlist;
        }

        public bool TryAdd(IPlaylist playlist)
        {
            return CustomPlaylists.TryAdd(playlist.Filename.ToLower(), playlist);
        }

        public IPlaylist GetOrAdd(string playlistFileName, Func<IPlaylist> newPlaylist)
        {
            var playlist = GetPlaylist(playlistFileName);
            if (playlist == null)
            {
                playlist = newPlaylist();
                if (playlist != null)
                {
                    if (!string.IsNullOrEmpty(playlist.Filename))
                        CustomPlaylists.TryAdd(playlist.Filename?.ToLower() ?? "", playlist);
                    else
                        Logger.log?.Warn($"Invalid playlist file name in playlist function given to PlaylistManager.GetOrAdd()");
                }
                else
                    Logger.log?.Warn($"Playlist function returned a null playlist in PlaylistManager.GetOrAdd()");
            }
            return playlist;
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
