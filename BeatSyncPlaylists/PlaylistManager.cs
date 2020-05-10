using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using BeatSyncPlaylists.Legacy;
using BeatSyncPlaylists.Logging;

namespace BeatSyncPlaylists
{
    public class PlaylistManager
    {
        protected readonly Dictionary<Type, IPlaylistHandler> PlaylistHandlers = new Dictionary<Type, IPlaylistHandler>();
        protected readonly Dictionary<string, IPlaylistHandler> PlaylistExtensionHandlers = new Dictionary<string, IPlaylistHandler>();
        public string PlaylistPath { get; protected set; }

        public IPlaylistHandler DefaultHandler { get; } = new LegacyPlaylistHandler();

        public string DisabledPlaylistsPath => Path.Combine(PlaylistPath, "DisabledPlaylists");
        protected PlaylistManager()
        {
            RegisterHandler(new LegacyPlaylistHandler());
            DefaultPlaylists = new ReadOnlyDictionary<BuiltInPlaylist, IPlaylist>(new Dictionary<BuiltInPlaylist, IPlaylist>()
                {
                    {BuiltInPlaylist.BeatSyncAll, CreatePlaylist("BeatSyncPlaylist", "BeatSync Playlist", "BeatSync",
                        PlaylistImageLoaders[BuiltInPlaylist.BeatSyncAll], "Every song BeatSync has downloaded.") },
                    {BuiltInPlaylist.BeastSaberBookmarks, CreatePlaylist("BeatSyncBSaberBookmarks", "BeastSaber Bookmarks", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberBookmarks]) },
                    {BuiltInPlaylist.BeastSaberFollows, CreatePlaylist("BeatSyncBSaberFollows", "BeastSaber Follows", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberFollows]) },
                    {BuiltInPlaylist.BeastSaberCurator, CreatePlaylist("BeatSyncBSaberCuratorRecommended", "Curator Recommended", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeastSaberCurator]) },
                    {BuiltInPlaylist.ScoreSaberTopRanked, CreatePlaylist("BeatSyncScoreSaberTopRanked", "ScoreSaber Top Ranked", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTopRanked]) },
                    {BuiltInPlaylist.ScoreSaberLatestRanked, CreatePlaylist("BeatSyncScoreSaberLatestRanked", "ScoreSaber Latest Ranked", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberLatestRanked]) },
                    {BuiltInPlaylist.ScoreSaberTopPlayed, CreatePlaylist("BeatSyncScoreSaberTopPlayed", "ScoreSaber Top Played", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTopPlayed]) },
                    {BuiltInPlaylist.ScoreSaberTrending, CreatePlaylist("BeatSyncScoreSaberTrending", "ScoreSaber Trending", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.ScoreSaberTrending]) },
                    {BuiltInPlaylist.BeatSaverFavoriteMappers, CreatePlaylist("BeatSyncFavoriteMappers", "Favorite Mappers", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverFavoriteMappers]) },
                    {BuiltInPlaylist.BeatSaverLatest, CreatePlaylist("BeatSyncBeatSaverLatest", "BeatSaver Latest", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverLatest]) },
                    {BuiltInPlaylist.BeatSaverHot, CreatePlaylist("BeatSyncBeatSaverHot", "Beat Saver Hot", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverHot]) },
                    {BuiltInPlaylist.BeatSaverPlays, CreatePlaylist("BeatSyncBeatSaverPlays", "Beat Saver Plays", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverPlays]) },
                    {BuiltInPlaylist.BeatSaverDownloads, CreatePlaylist("BeatSyncBeatSaverDownloads", "Beat Saver Downloads", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSaverDownloads]) },
                    {BuiltInPlaylist.BeatSyncRecent, CreatePlaylist("BeatSyncRecent", "BeatSync Recent Songs", "BeatSync", PlaylistImageLoaders[BuiltInPlaylist.BeatSyncRecent]) }
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
            PlaylistExtensionHandlers.TryGetValue(extension, out IPlaylistHandler? handler);
            return handler;
        }

        private Dictionary<BuiltInPlaylist, IPlaylist> AvailablePlaylists = new Dictionary<BuiltInPlaylist, IPlaylist>(); // Doesn't need to be concurrent, basically readonly

        /// <summary>
        /// Key is the file name in lowercase.
        /// </summary>
        private ConcurrentDictionary<string, IPlaylist> CustomPlaylists = new ConcurrentDictionary<string, IPlaylist>();


        public static Lazy<string> GetImageLoader(string resourcePath)
        {
            return new Lazy<string>(() => Utilities.ImageToBase64(resourcePath));
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

        public readonly ReadOnlyDictionary<BuiltInPlaylist, IPlaylist> DefaultPlaylists;

        public IPlaylist CreatePlaylist(string fileName, string title, string author, Lazy<string> imageLoader, string? description = null)
        {
            return new LegacyPlaylist(fileName, title, author, imageLoader) { Description = description };
        }

        public IPlaylist CreatePlaylist(string fileName, string title, string author, string coverImage, string? description = null)
        {
            return new LegacyPlaylist(fileName, title, author, coverImage) { Description = description };
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
                playlist.TryRemoveByHash(hash);
            }
            var customPlaylistKeys = CustomPlaylists.Keys;
            foreach (var key in customPlaylistKeys)
            {
                CustomPlaylists[key].TryRemoveByHash(hash);
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
                    StorePlaylist(playlist);
                }
            }
            foreach (IPlaylist customPlaylist in CustomPlaylists.Values)
            {
                if (customPlaylist.IsDirty)
                {
                    StorePlaylist(customPlaylist);
                }
            }
        }

        public void StorePlaylist(IPlaylist playlist)
        {
            if (PlaylistHandlers.TryGetValue(playlist.GetType(), out IPlaylistHandler playlistHandler))
            {
                string extension = playlistHandler.DefaultExtension;
                if (playlist.SuggestedExtension != null && playlistHandler.GetSupportedExtensions().Contains(playlist.SuggestedExtension))
                    extension = playlist.SuggestedExtension;
                string fileName = playlist.Filename + "." + extension;
                Logger.log?.Debug($"Writing {fileName} to file.");
                playlistHandler.SerializeToFile(playlist, Path.Combine(PlaylistPath, fileName));
                playlist.MarkDirty(false);
            }
            else
            {
                Logger.log?.Error($"No matching handler for playlist: {playlist.Filename}");
            }
        }

        /// <summary>
        /// Retrieves the specified playlist. If the playlist doesn't exist, creates one using the default.
        /// </summary>
        /// <param name="builtInPlaylist"></param>
        /// <returns></returns>
        public IPlaylist GetPlaylist(BuiltInPlaylist builtInPlaylist)
        {
            IPlaylist? playlist = null;
            bool playlistExists = AvailablePlaylists.TryGetValue(builtInPlaylist, out playlist);
            if (!playlistExists || playlist == null)
            {
                var defPlaylist = DefaultPlaylists[builtInPlaylist];
                string[] files = Directory.GetFiles(PlaylistPath);
                string file = files.FirstOrDefault(f => defPlaylist.Filename.Equals(Path.GetFileNameWithoutExtension(f), StringComparison.OrdinalIgnoreCase));
                string? fileExtension = null;
                if (file != null)
                    fileExtension = Path.GetExtension(file).TrimStart('.');
                if (fileExtension != null && PlaylistExtensionHandlers.TryGetValue(fileExtension, out IPlaylistHandler handler))
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    playlist = handler.Deserialize(file);
                    playlist.SuggestedExtension = fileExtension;
#pragma warning restore CS8604 // Possible null reference argument.
                    if (playlist == null)
                    {
                        playlist = defPlaylist;
                        Logger.log?.Debug($"Playlist created with filename: {playlist.Filename}.{fileExtension}.");
                    }
                    else
                    {
                        playlist.Filename = Path.GetFileNameWithoutExtension(file);
                        Logger.log?.Debug($"Playlist loaded from file: {playlist.Filename}.{fileExtension} with {playlist.Count} songs.");
                    }
                }
                else
                {
                    playlist = defPlaylist;
                    Logger.log?.Debug($"Playlist created with filename: {playlist.Filename}.");
                }
                AvailablePlaylists.Add(builtInPlaylist, playlist);
            }
            if (playlist == null)
                throw new ArgumentException($"BuiltInPlaylist not supported: {builtInPlaylist}.", nameof(builtInPlaylist));
            return playlist;
        }

        protected void AddToCustomPlaylists(IPlaylist playlist)
        {
            if (!string.IsNullOrEmpty(playlist.Filename))
                CustomPlaylists.TryAdd(playlist.Filename.ToUpper(), playlist);
        }

        protected bool TryGetCustomPlaylist(string fileName, out IPlaylist? playlist)
        {
            return CustomPlaylists.TryGetValue(fileName.ToUpper(), out playlist);
        }

        /// <summary>
        /// Retrieves the specified playlist. If the playlist doesn't exist, returns null.
        /// </summary>
        /// <param name="builtInPlaylist"></param>
        /// <returns></returns>
        public IPlaylist? GetPlaylist(string playlistFileName)
        {
            if (string.IsNullOrEmpty(playlistFileName))
                return null;
            IPlaylist? playlist = null;
            // Check if the playlist is one of the built in ones.
            foreach (var defaultPlaylist in DefaultPlaylists)
            {
                if (defaultPlaylist.Value.Filename.Equals(playlistFileName, StringComparison.OrdinalIgnoreCase))
                {
                    playlist = GetPlaylist(defaultPlaylist.Key);
                }
            }


            // Check if this playlist exists in CustomPlaylists
            if (playlist == null)
            {
                TryGetCustomPlaylist(playlistFileName, out playlist);
            }

            // Check if the playlistFileName exists
            if (playlist == null)
            {

                string? existingFile = Directory.GetFiles(PlaylistPath).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(playlistFileName, StringComparison.OrdinalIgnoreCase));
                if (existingFile != null)
                {
                    string extension = Path.GetExtension(existingFile);
                    if (!string.IsNullOrEmpty(existingFile) && PlaylistExtensionHandlers.TryGetValue(extension, out IPlaylistHandler handler))
                    {
                        playlist = handler.Deserialize(existingFile);
                        playlist.Filename = playlistFileName;
                        AddToCustomPlaylists(playlist);
                        Logger.log?.Debug($"Playlist FileName is {playlist.Filename}");
                    }
                }
            }
            Logger.log?.Debug($"Returning {playlist?.Filename}: {playlist?.Title} with {playlist?.Count} songs.");
            return playlist;
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
                        AddToCustomPlaylists(playlist);
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
