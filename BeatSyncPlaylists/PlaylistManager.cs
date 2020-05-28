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

        private readonly Dictionary<BuiltInPlaylist, IPlaylist> AvailablePlaylists = new Dictionary<BuiltInPlaylist, IPlaylist>(); // Doesn't need to be concurrent, basically readonly
        private object _changedLock = new object();
        private HashSet<IPlaylist> ChangedPlaylists = new HashSet<IPlaylist>();
        /// <summary>
        /// Key is the file name in uppercase.
        /// </summary>
        private ConcurrentDictionary<string, IPlaylist> CustomPlaylists = new ConcurrentDictionary<string, IPlaylist>();
        public static readonly string AuthorPlaylistPrefix = "BeatSyncAuthor_";
        public string? PlaylistPath { get; protected set; }

        public IPlaylistHandler DefaultHandler { get; } = new LegacyPlaylistHandler();

        public string DisabledPlaylistsPath => Path.Combine(PlaylistPath, "DisabledPlaylists");
        protected PlaylistManager()
        {
            RegisterHandler(new LegacyPlaylistHandler());
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

        public IPlaylist CreatePlaylist(string fileName, string title, string author, Lazy<string> imageLoader, string? description = null)
        {
            IPlaylist playlist = new LegacyPlaylist(fileName, title, author, imageLoader) { Description = description };
            return playlist;
        }

        public IPlaylist CreatePlaylist(string fileName, string title, string author, string coverImage, string? description = null)
        {
            IPlaylist playlist = new LegacyPlaylist(fileName, title, author, coverImage) { Description = description };
            return playlist;
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

        public void StoreAllPlaylists()
        {
            IPlaylist[]? changedPlaylists;
            lock (_changedLock)
            {
                changedPlaylists = ChangedPlaylists.ToArray();
                ChangedPlaylists.Clear();
            }
            foreach (var playlist in changedPlaylists)
            {
                if (playlist == null)
                    continue;
                StorePlaylist(playlist, false);
            }
        }

        private void OnPlaylistChanged(object sender, EventArgs e)
        {
            if(sender is IPlaylist playlist)
            {
                AddToChanged(playlist);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="playlist"></param>
        /// <param name="removeFromChanged"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void StorePlaylist(IPlaylist playlist, bool removeFromChanged = true)
        {
            string playlistPath = PlaylistPath ?? throw new InvalidOperationException($"{nameof(PlaylistPath)} has not been set.");
            if (PlaylistHandlers.TryGetValue(playlist.GetType(), out IPlaylistHandler playlistHandler))
            {
                string extension = playlistHandler.DefaultExtension;
                if (playlist.SuggestedExtension != null && playlistHandler.GetSupportedExtensions().Contains(playlist.SuggestedExtension))
                    extension = playlist.SuggestedExtension;
                string fileName = playlist.Filename + "." + extension;
                Logger.log?.Debug($"Writing {fileName} to file.");
                playlistHandler.SerializeToFile(playlist, Path.Combine(playlistPath, fileName));
                if (removeFromChanged)
                    RemoveFromChanged(playlist);
            }
            else
            {
                Logger.log?.Error($"No matching handler for playlist: {playlist.Filename}");
            }
        }

        protected IPlaylist? LoadPlaylistFromFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName), "fileName cannot be null or empty.");
            string playlistPath = PlaylistPath ?? throw new InvalidOperationException($"{nameof(PlaylistPath)} has not been set.");
            IPlaylist? playlist = null;
            string[] files = Directory.GetFiles(playlistPath);
            string file = files.FirstOrDefault(f => fileName.Equals(Path.GetFileNameWithoutExtension(f), StringComparison.OrdinalIgnoreCase));
            string? fileExtension = null;
            if (file != null)
            {
                fileExtension = Path.GetExtension(file).TrimStart('.');
                if (fileExtension != null && PlaylistExtensionHandlers.TryGetValue(fileExtension, out IPlaylistHandler handler))
                {
                    playlist = handler.Deserialize(file);
                    playlist.SuggestedExtension = fileExtension;
                    if (playlist != null)
                    {
                        playlist.Filename = Path.GetFileNameWithoutExtension(file);
                        AddToCustomPlaylists(playlist, false);
                        Logger.log?.Debug($"Playlist loaded from file: {playlist.Filename}.{fileExtension} with {playlist.Count} songs.");
                    }
                }
            }
            return playlist;
        }

        public bool AddToCustomPlaylists(IPlaylist playlist, bool asChanged = true)
        {
            if (!string.IsNullOrEmpty(playlist.Filename))
            {
                if (CustomPlaylists.TryAdd(playlist.Filename.ToUpper(), playlist))
                {
                    playlist.PlaylistChanged += OnPlaylistChanged;
                    if (asChanged)
                        AddToChanged(playlist);
                    return true;
                }
                else
                    return false;
            }
            throw new InvalidOperationException("Playlist Filename cannot be null or empty.");
        }

        public bool TryGetCustomPlaylist(string fileName, out IPlaylist? playlist)
        {
            return CustomPlaylists.TryGetValue(fileName.ToUpper(), out playlist);
        }

        public void AddToChanged(IPlaylist playlist)
        {
            lock (_changedLock)
            {
                ChangedPlaylists.Add(playlist);
            }
        }
        private void RemoveFromChanged(IPlaylist playlist)
        {
            lock (_changedLock)
            {
                ChangedPlaylists.Remove(playlist);
            }
        }
        public bool PlaylistIsChanged(IPlaylist playlist)
        {
            return ChangedPlaylists.Contains(playlist);
        }

        /// <summary>
        /// Retrieves the specified playlist. If the playlist doesn't exist, returns null.
        /// </summary>
        /// <param name="playlistFileName"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public IPlaylist? GetPlaylist(string playlistFileName)
        {
            _ = PlaylistPath ?? throw new InvalidOperationException($"{nameof(PlaylistPath)} has not been set.");
            if (string.IsNullOrEmpty(playlistFileName))
                return null;
            IPlaylist? playlist = null;

            // Check if this playlist exists in CustomPlaylists
            if (playlist == null)
            {
                TryGetCustomPlaylist(playlistFileName, out playlist);
            }

            // Try to load from file
            if (playlist == null)
            {
                playlist = LoadPlaylistFromFile(playlistFileName);
            }
            if (playlist != null)
                Logger.log?.Debug($"Returning {playlist?.Filename}: {playlist?.Title} with {playlist?.Count} songs.");
            return playlist;
        }

        public IPlaylist GetOrAdd(string playlistFileName, Func<IPlaylist> playlistFactory)
        {
            if (string.IsNullOrEmpty(playlistFileName))
                throw new ArgumentNullException(nameof(playlistFileName), "playlistFileName cannot be null or empty.");
            var playlist = GetPlaylist(playlistFileName);
            if (playlist == null)
            {
                playlist = playlistFactory() ?? throw new ArgumentException("playlistFactory returned a null IPlaylist.", nameof(playlistFactory));
                playlist.Filename = playlistFileName;
                AddToCustomPlaylists(playlist);
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
