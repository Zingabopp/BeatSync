using BeatSync.Configs;
using BeatSync.Playlists;
using BeatSync.Utilities;
using SongFeedReaders;
using SongFeedReaders.DataflowAlternative;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeatSync.UI;

namespace BeatSync.Downloader
{
    public class SongDownloader
    {
        private static readonly string SongTempPath = Path.GetFullPath(Path.Combine("UserData", "BeatSyncTemp"));
        private readonly string CustomLevelsPath;
        private PluginConfig Config;
        private Playlist RecentPlaylist;
        public HistoryManager HistoryManager { get; private set; }
        public SongHasher HashSource { get; private set; }
        public FavoriteMappers FavoriteMappers { get; private set; }
        public DownloadManager DownloadManager { get; private set; }
        public IStatusManager StatusManager { get; set; }

        public SongDownloader(PluginConfig config, HistoryManager historyManager, SongHasher hashSource, string customLevelsPath)
        {
            DownloadManager = new DownloadManager(config.MaxConcurrentDownloads);
            CustomLevelsPath = customLevelsPath;
            Directory.CreateDirectory(CustomLevelsPath);
            HashSource = hashSource;
            HistoryManager = historyManager;
            FavoriteMappers = new FavoriteMappers();
            FavoriteMappers.Initialize();
            Config = config.Clone();
            RecentPlaylist = Config.RecentPlaylistDays > 0 ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncRecent) : null;
        }

        public void ProcessJob(IDownloadJob job)
        {
            var playlistSong = new PlaylistSong(job.SongHash, job.SongName, job.SongKey, job.LevelAuthorName);
            var historyEntry = HistoryManager.GetOrAdd(playlistSong.Hash, (s) => new HistoryEntry(playlistSong, HistoryFlag.None));
            if (job.Result.Successful)
            {
                //HistoryManager.TryUpdateFlag(job.SongHash, HistoryFlag.Downloaded);
                historyEntry.Flag = HistoryFlag.Downloaded;
                RecentPlaylist?.TryAdd(playlistSong);

            }
            else if (job.Result.DownloadResult.Status != DownloadResultStatus.Success)
            {
                if (job.Result.DownloadResult.HttpStatusCode == 404)
                {
                    // Song isn't on Beat Saver anymore, keep it in history so it isn't attempted again.
                    Logger.log?.Debug($"Setting 404 flag for {playlistSong.ToString()}");
                    historyEntry.Flag = HistoryFlag.BeatSaverNotFound;
                    //if (!HistoryManager.TryUpdateFlag(job.SongHash, HistoryFlag.BeatSaverNotFound))
                    //Logger.log?.Debug($"Failed to update flag for {playlistSong.ToString()}");
                    PlaylistManager.RemoveSongFromAll(job.SongHash);
                }
                else
                {
                    // Download failed for some reason, remove from history so it tries again.
                    historyEntry.Flag = HistoryFlag.Error;
                    //HistoryManager.TryRemove(job.SongHash, out var _);
                }
            }
            else if (job.Result.ZipResult?.ResultStatus != ZipExtractResultStatus.Success)
            {
                // Unzipping failed for some reason, remove from history so it tries again.
                historyEntry.Flag = HistoryFlag.Error;
                //HistoryManager.TryRemove(job.SongHash, out var _);
            }
        }

        /// <summary>
        /// Signals the DownloadManager to complete the remaining downloads, returns a List of the JobResults.
        /// </summary>
        /// <returns></returns>
        public async Task<List<IDownloadJob>> WaitDownloadCompletionAsync()
        {
            List<IDownloadJob> jobs = null;
            try
            {
                Logger.log?.Debug($"Waiting for Completion.");
                await DownloadManager.CompleteAsync().ConfigureAwait(false);
                Logger.log?.Debug($"All downloads should be complete.");
                jobs = DownloadManager.CompletedJobs.ToList();
                foreach (var job in jobs)
                {
                    if (job.Exception != null)
                    {
                        Logger.log?.Warn($"Error in one of the DownloadJobs.\n{job.Exception.Message}");
                        Logger.log?.Debug($"Error in one of the DownloadJobs.\n{job.Exception.StackTrace}");
                    }
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    ProcessJob(job);
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error processing downloads:\n {ex.Message}");
                Logger.log?.Debug($"Error processing downloads:\n {ex.StackTrace}");
            }
            try
            {
                if (Directory.Exists(SongTempPath))
                    Directory.Delete(SongTempPath, true);
            }
            catch (Exception) { }
            return jobs;
        }

        public async Task RunReaders()
        {
            List<Task<Dictionary<string, ScrapedSong>>> readerTasks = new List<Task<Dictionary<string, ScrapedSong>>>();
            DownloadManager.Start();
            var config = Config;
            if (config.BeastSaber.Enabled)
            {
                readerTasks.Add(ReadBeastSaber());
            }
            else
            {
                SetStatus("BeastSaber", "Disabled", UI.FontColor.Red);
            }
            if (config.BeatSaver.Enabled)
            {
                readerTasks.Add(ReadBeatSaver());
            }
            else
            {
                SetStatus("BeatSaver", "Disabled", UI.FontColor.Red);
            }
            if (config.ScoreSaber.Enabled)
            {
                readerTasks.Add(ReadScoreSaber());
            }
            else
            {
                SetStatus("ScoreSaber", "Disabled", UI.FontColor.Red);
            }
            Dictionary<string, ScrapedSong>[] results = null;
            try
            {
                results = await Task.WhenAll(readerTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error in reading feeds.\n{ex.Message}\n{ex.StackTrace}");
            }

            Logger.log?.Info($"Finished reading feeds.");
            var songsToDownload = new Dictionary<string, ScrapedSong>();
            foreach (var readTask in readerTasks)
            {
                if (!readTask.DidCompleteSuccessfully())
                {
                    Logger.log?.Warn("Task not successful, skipping.");
                    continue;
                }
                Logger.log?.Debug($"Queuing songs from task.");
                songsToDownload.Merge(await readTask);
            }
            Logger.log?.Info($"Found {songsToDownload.Count} unique songs.");
            var allPlaylist = config.AllBeatSyncSongsPlaylist ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncAll) : null;

            foreach (var pair in songsToDownload)
            {

                var playlistSong = new PlaylistSong(pair.Value.Hash, pair.Value.SongName, pair.Value.SongKey, pair.Value.MapperName);
                /*
                var notInHistory = HistoryManager.TryAdd(playlistSong, HistoryFlag.None); // Make sure it's in HistoryManager even if it already exists.
                HistoryManager.TryGetValue(pair.Value.Hash, out var historyEntry);
                var didntExist = HashSource.ExistingSongs.TryAdd(pair.Value.Hash, "");
                if (didntExist && (notInHistory || historyEntry.Flag == HistoryFlag.Error))
                {
                    //Logger.log?.Info($"Queuing {pair.Value.SongKey} - {pair.Value.SongName} by {pair.Value.MapperName} for download.");

                    DownloadQueue.Enqueue(playlistSong);
                }
                else if (!didntExist && historyEntry != null)
                {
                    if (historyEntry.Flag == HistoryFlag.None)
                        HistoryManager.TryUpdateFlag(pair.Value.Hash, HistoryFlag.PreExisting);
                }
                */

                allPlaylist?.TryAdd(playlistSong);

            }
            allPlaylist?.TryWriteFile();

        }

        public async Task<Dictionary<string, ScrapedSong>> ReadFeed(IFeedReader reader, IFeedSettings settings, Playlist feedPlaylist, PlaylistStyle playlistStyle)
        {
            //Logger.log?.Info($"Getting songs from {feedName} feed.");
            var songs = await reader.GetSongsFromFeedAsync(settings).ConfigureAwait(false) ?? new Dictionary<string, ScrapedSong>();
            if (songs.Count > 0 && playlistStyle == PlaylistStyle.Replace)
                feedPlaylist.Clear();
            var addDate = DateTime.Now;
            var decrement = new TimeSpan(1);
            foreach (var scrapedSong in songs)
            {
                // Skip songs that were deleted or not found on Beat Saver.
                var inHistory = HistoryManager.TryGetValue(scrapedSong.Value.Hash, out var historyEntry);
                if (inHistory && (historyEntry.Flag == HistoryFlag.BeatSaverNotFound
                    || historyEntry.Flag == HistoryFlag.Deleted))
                {
                    continue;
                }
                var song = scrapedSong.Value.ToPlaylistSong();
                if (!inHistory && HashSource.ExistingSongs.ContainsKey(scrapedSong.Value.Hash))
                {
                    HistoryManager.TryAdd(song, HistoryFlag.PreExisting);
                }
                song.DateAdded = addDate;
                var source = $"{reader.Name}.{reader.GetFeedName(settings)}";
                song.FeedSources.Add(source);
                feedPlaylist?.TryAdd(song);
                addDate = addDate - decrement;
            }
            feedPlaylist?.TryWriteFile();

            return songs;
        }

        #region Feed Read Functions
        public async Task<Dictionary<string, ScrapedSong>> ReadBeastSaber()
        {
            string readerName = string.Empty; // BeastSaberReader
            bool error = false;
            bool warning = false;
            if (BeatSync.Paused)
                await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log?.Info("Starting BeastSaber reading");

            var config = Config.BeastSaber;
            BeastSaberReader reader = null;
            try
            {
                reader = new BeastSaberReader(config.Username, config.MaxConcurrentPageChecks);
                readerName = reader.Name;
            }
            catch (Exception ex)
            {
                Logger.log?.Error(ex);
                SetError(readerName);
                return null;
            }
            var readerSongs = new Dictionary<string, ScrapedSong>();
            if (config.Bookmarks.Enabled && !string.IsNullOrEmpty(config.Username))
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Bookmarks...") ?? 0;
                    var feedSettings = config.Bookmarks.ToFeedSettings();
                    var feedPlaylist = config.Bookmarks.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Bookmarks.FeedPlaylist)
                        : null;
                    var playlistStyle = config.Bookmarks.PlaylistStyle;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist, playlistStyle).ConfigureAwait(false);
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    var feedName = reader.GetFeedName(feedSettings);
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.");
                    StatusManager?.AppendPost(postId, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                    readerSongs.Merge(songs);
                }
                catch (ArgumentException ex)
                {
                    warning = true;
                    Logger.log?.Critical("Exception in BeastSaber Bookmarks: " + ex.Message);
                    int postId = StatusManager?.Post(readerName, "Exception in Bookmarks, see log.", UI.FontColor.Red) ?? 0;
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadBeastSaber, Bookmarks.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Bookmarks, see log.", UI.FontColor.Red) ?? 0;
                }
            }
            else if (string.IsNullOrEmpty(config.Username))
            {
                Logger.log?.Warn("BeastSaber Bookmarks feed is enabled, but a username has not been provided.");
                int postId = StatusManager?.Post(readerName, "Bookmarks: No Username specified, skipping.", UI.FontColor.Yellow) ?? 0;
            }
            if (config.Follows.Enabled && !string.IsNullOrEmpty(config.Username))
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Follows...") ?? 0;
                    var feedSettings = config.Follows.ToFeedSettings();
                    var feedPlaylist = config.Follows.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Follows.FeedPlaylist)
                        : null;
                    var playlistStyle = config.Follows.PlaylistStyle;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist, playlistStyle).ConfigureAwait(false);
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    var feedName = reader.GetFeedName(feedSettings);
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.");
                    StatusManager?.AppendPost(postId, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                    readerSongs.Merge(songs);
                }
                catch (ArgumentException ex)
                {
                    warning = true;
                    Logger.log?.Critical("Exception in BeastSaber Follows: " + ex.Message);
                    int postId = StatusManager?.Post(readerName, "Exception in Follows, see log.", UI.FontColor.Red) ?? 0;
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadBeastSaber, Follows.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Follows, see log.", UI.FontColor.Red) ?? 0;
                }
            }
            else if (string.IsNullOrEmpty(config.Username))
            {
                Logger.log?.Warn("BeastSaber Follows feed is enabled, but a username has not been provided.");
                int postId = StatusManager?.Post(readerName, "Follows: No Username specified, skipping.", UI.FontColor.Yellow) ?? 0;
                warning = true;
            }
            if (config.CuratorRecommended.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Curator Recommended...") ?? 0;
                    //await Task.Delay(1000);
                    //StatusManager?.PinPost(postId);
                    //for (int i = 0; i < 10; i++)
                    //{
                    //    var testId = StatusManager?.Post(readerName, $"Test {i}") ?? 0;
                    //    if (!(StatusManager?.PinPost(testId) ?? false))
                    //        Logger.log?.Info("Pinning failed");
                    //    if (i == 5)
                    //        StatusManager.UnpinAndRemovePost(postId);
                    //    await Task.Delay(1000);
                    //}
                    var feedSettings = config.CuratorRecommended.ToFeedSettings();
                    var feedPlaylist = config.CuratorRecommended.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.CuratorRecommended.FeedPlaylist)
                        : null;
                    var playlistStyle = config.CuratorRecommended.PlaylistStyle;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist, playlistStyle).ConfigureAwait(false);
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    var feedName = reader.GetFeedName(feedSettings);
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.");
                    StatusManager?.AppendPost(postId, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeastSaber, Curator Recommended.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Curator Recommended, see log.", UI.FontColor.Red) ?? 0;
                }
            }
            if (BeatSync.Paused)
                await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
            sw.Stop();
            var totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log?.Info($"{reader.Name}: Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} in {sw.Elapsed.ToString()}");
            if (error)
            {

            }
            else if (warning)
            {
                SetStatus(readerName, "Finished Reading with warnings", UI.FontColor.Yellow);
            }
            else
            {
                SetStatus(readerName, "Finished Reading", UI.FontColor.White);
            }
            await Task.Delay(2000).ConfigureAwait(false); // Wait a bit before clearing.
            StatusManager.Clear(reader.Name);
            int songsPosted = 0;
            foreach (var song in readerSongs.Values)
            {

                if (PostJobToDownload(song, readerName))
                    songsPosted++;
            }
            SetStatus(readerName, $"Downloading {songsPosted} songs");
            return readerSongs;
        }

        public async Task<Dictionary<string, ScrapedSong>> ReadBeatSaver(Playlist allPlaylist = null)
        {
            string readerName = string.Empty; // BeatSaverReader
            bool warning = false;
            bool error = false;
            if (BeatSync.Paused)
                await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log?.Info("Starting BeatSaver reading");

            var config = Config.BeatSaver;
            BeatSaverReader reader = null;
            try
            {
                reader = new BeatSaverReader();
                readerName = reader.Name;
            }
            catch (Exception ex)
            {
                Logger.log?.Error("Exception creating BeatSaverReader in ReadBeatSaver.");
                Logger.log?.Error(ex);
                SetError(readerName);
                return null;
            }
            var readerSongs = new Dictionary<string, ScrapedSong>();

            if (config.FavoriteMappers.Enabled && (FavoriteMappers.Mappers?.Count() ?? 0) > 0)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, $"Starting Feed: FavoriteMappers ({FavoriteMappers.Mappers.Count} mappers)...") ?? 0;
                    var feedSettings = config.FavoriteMappers.ToFeedSettings() as BeatSaverFeedSettings;
                    Playlist feedPlaylist = null;
                    if (!config.FavoriteMappers.SeparateMapperPlaylists)
                    {
                        feedPlaylist = config.FavoriteMappers.CreatePlaylist
                            ? PlaylistManager.GetPlaylist(config.FavoriteMappers.FeedPlaylist)
                            : null;
                    }

                    var playlistStyle = config.FavoriteMappers.PlaylistStyle;
                    var songs = new Dictionary<string, ScrapedSong>();
                    foreach (var author in FavoriteMappers.Mappers)
                    {
                        feedSettings.Criteria = author;
                        if (BeatSync.Paused)
                            await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                        var authorSongs = await ReadFeed(reader, feedSettings, feedPlaylist, playlistStyle).ConfigureAwait(false);
                        Logger.log?.Info($"   FavoriteMappers: Found {authorSongs.Count} songs by {author}");
                        if (config.FavoriteMappers.CreatePlaylist && config.FavoriteMappers.SeparateMapperPlaylists)
                        {
                            var playlistFileName = $"{author}.bplist";
                            var mapperPlaylist = PlaylistManager.GetOrAdd(playlistFileName, () => new Playlist(playlistFileName, author, "BeatSync", "1"));
                            if (mapperPlaylist != null)
                            {
                                if (playlistStyle == PlaylistStyle.Replace)
                                    mapperPlaylist.Clear();
                                foreach (var song in authorSongs.Values)
                                {
                                    mapperPlaylist.TryAdd(song.ToPlaylistSong());
                                }
                            }
                        }

                        songs.Merge(authorSongs);
                    }
                    if (feedPlaylist != null)
                    {
                        foreach (var song in songs.Values)
                        {
                            feedPlaylist.TryAdd(song.ToPlaylistSong());
                        }
                    }
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    var feedName = reader.GetFeedName(feedSettings);
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.");
                    StatusManager?.AppendPost(postId, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                    int postId = StatusManager?.Post(readerName, "Exception in FavoriteMappers, see log.", UI.FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, FavoriteMappers: " + ex.Message);
                    int postId = StatusManager?.Post(readerName, "Exception in FavoriteMappers, see log.", UI.FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, FavoriteMappers.");
                    int postId = StatusManager?.Post(readerName, "Exception in FavoriteMappers, see log.", UI.FontColor.Red) ?? 0;
                    Logger.log?.Error(ex);
                    warning = true;
                }
            }
            else if (config.FavoriteMappers.Enabled)
            {
                Logger.log?.Warn("BeatSaver's FavoriteMappers feed is enabled, but no mappers could be found in UserData\\FavoriteMappers.ini");
                int postId = StatusManager?.Post(readerName, "No mappers found in FavoriteMappers.ini, skipping", UI.FontColor.Yellow) ?? 0;
            }
            if (config.Hot.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Hot...") ?? 0;
                    var feedSettings = config.Hot.ToFeedSettings();
                    var feedPlaylist = config.Hot.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Hot.FeedPlaylist)
                        : null;
                    var playlistStyle = config.Hot.PlaylistStyle;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist, playlistStyle).ConfigureAwait(false);
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    var feedName = reader.GetFeedName(feedSettings);
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.");
                    StatusManager?.AppendPost(postId, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                    int postId = StatusManager?.Post(readerName, "Exception in Hot, see log.", UI.FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, Hot: " + ex.Message);
                    int postId = StatusManager?.Post(readerName, "Exception in Hot, see log.", UI.FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, Hot.");
                    int postId = StatusManager?.Post(readerName, "Exception in Hot, see log.", UI.FontColor.Red) ?? 0;
                    Logger.log?.Error(ex);
                    warning = true;
                }
            }
            if (config.Downloads.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Downloads...") ?? 0;
                    var feedSettings = config.Downloads.ToFeedSettings();
                    var feedPlaylist = config.Downloads.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Downloads.FeedPlaylist)
                        : null;
                    var playlistStyle = config.Downloads.PlaylistStyle;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist, playlistStyle).ConfigureAwait(false);
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    var feedName = reader.GetFeedName(feedSettings);
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.");
                    readerSongs.Merge(songs);
                    StatusManager?.AppendPost(postId, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                    int postId = StatusManager?.Post(readerName, "Exception in Downloads, see log.", UI.FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, Downloads: " + ex.Message);
                    int postId = StatusManager?.Post(readerName, "Exception in Downloads, see log.", UI.FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, Downloads.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Downloads, see log.", UI.FontColor.Red) ?? 0;
                    warning = true;
                }
            }
            sw.Stop();
            if (BeatSync.Paused)
                await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
            var totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log?.Info($"{reader.Name}: Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} in {sw.Elapsed.ToString()}");
            if (error)
            {

            }
            else if (warning)
            {
                SetStatus(readerName, "Finished Reading Feeds with warnings", UI.FontColor.Yellow);
            }
            else
            {
                SetStatus(readerName, "Finished Reading Feeds", UI.FontColor.White);
            }
            await Task.Delay(2000); // Wait a bit before clearing.
            StatusManager.Clear(reader.Name);
            int songsPosted = 0;
            foreach (var song in readerSongs.Values)
            {
                if (PostJobToDownload(song, readerName))
                    songsPosted++;
            }
            SetStatus(readerName, $"Downloading {songsPosted} songs");
            return readerSongs;
        }

        public async Task<Dictionary<string, ScrapedSong>> ReadScoreSaber(Playlist allPlaylist = null)
        {
            string readerName = string.Empty; // ScoreSaberReader
            bool error = false;
            bool warning = false;
            if (BeatSync.Paused)
                await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log?.Info("Starting ScoreSaber reading");

            var config = Config.ScoreSaber;
            ScoreSaberReader reader = null;
            try
            {
                reader = new ScoreSaberReader();
                readerName = reader.Name;
            }
            catch (Exception ex)
            {
                Logger.log?.Error(ex);
                SetError(readerName);
                return null;
            }
            var readerSongs = new Dictionary<string, ScrapedSong>();

            if (config.TopRanked.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: TopRanked...") ?? 0;
                    var feedSettings = config.TopRanked.ToFeedSettings();
                    var feedPlaylist = config.TopRanked.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.TopRanked.FeedPlaylist)
                        : null;
                    var playlistStyle = config.TopRanked.PlaylistStyle;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist, playlistStyle).ConfigureAwait(false);
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    var feedName = reader.GetFeedName(feedSettings);
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.");
                    readerSongs.Merge(songs);
                    StatusManager?.AppendPost(postId, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Top Ranked.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Top Ranked, see log.", UI.FontColor.Red) ?? 0;
                }
            }

            if (config.Trending.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Trending...") ?? 0;
                    var feedSettings = config.Trending.ToFeedSettings();
                    var feedPlaylist = config.Trending.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Trending.FeedPlaylist)
                        : null;
                    var playlistStyle = config.Trending.PlaylistStyle;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist, playlistStyle).ConfigureAwait(false);
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    var feedName = reader.GetFeedName(feedSettings);
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.");
                    readerSongs.Merge(songs);
                    StatusManager?.AppendPost(postId, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Trending.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Trending, see log.", UI.FontColor.Red) ?? 0;
                }
            }

            if (config.TopPlayed.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: TopPlayed...") ?? 0;
                    var feedSettings = config.TopPlayed.ToFeedSettings();
                    var feedPlaylist = config.TopPlayed.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.TopPlayed.FeedPlaylist)
                        : null;
                    var playlistStyle = config.TopPlayed.PlaylistStyle;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist, playlistStyle).ConfigureAwait(false);
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    var feedName = reader.GetFeedName(feedSettings);
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.");
                    readerSongs.Merge(songs);
                    StatusManager?.AppendPost(postId, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Top Played.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Top Played, see log.", UI.FontColor.Red) ?? 0;
                }
            }

            if (config.LatestRanked.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: LatestRanked...") ?? 0;
                    var feedSettings = config.LatestRanked.ToFeedSettings();
                    var feedPlaylist = config.LatestRanked.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.LatestRanked.FeedPlaylist)
                        : null;
                    var playlistStyle = config.LatestRanked.PlaylistStyle;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist, playlistStyle).ConfigureAwait(false);
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    var feedName = reader.GetFeedName(feedSettings);
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.");
                    readerSongs.Merge(songs);
                    StatusManager?.AppendPost(postId, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Latest Ranked.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Latest Ranked, see log.", UI.FontColor.Red) ?? 0;
                }
            }

            sw.Stop();
            if (BeatSync.Paused)
                await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
            var totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log?.Info($"{reader.Name}: Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} in {sw.Elapsed.ToString()}");
            if (error)
            {

            }
            else if (warning)
            {
                SetStatus(readerName, "Finished Reading Feeds with warnings", UI.FontColor.Yellow);
            }
            else
            {
                SetStatus(readerName, "Finished Reading Feeds", UI.FontColor.White);
            }
            await Task.Delay(2000); // Wait a bit before clearing.
            StatusManager.Clear(reader.Name);
            int songsPosted = 0;
            foreach (var song in readerSongs.Values)
            {
                if (PostJobToDownload(song, readerName))
                    songsPosted++;
            }
            SetStatus(readerName, $"Downloading {songsPosted} songs");
            return readerSongs;
        }
        #endregion


        public bool PostJobToDownload(PlaylistSong playlistSong, string readerName)
        {
            bool downloadPosted = false;
            var inHistory = HistoryManager.TryGetValue(playlistSong.Hash, out var historyEntry);
            var existsOnDisk = HashSource.ExistingSongs.TryGetValue(playlistSong.Hash, out var _);
            if (!existsOnDisk && (!inHistory || historyEntry.Flag == HistoryFlag.Error))
            {
                //Logger.log?.Info($"Queuing {pair.Value.SongKey} - {pair.Value.SongName} by {pair.Value.MapperName} for download.");
                downloadPosted = DownloadManager.TryPostJob(new DownloadJob(playlistSong, CustomLevelsPath), out var postedJob);
                if (postedJob != null)
                {

                    //Logger.log?.Info($"{readerName} posted job {playlistSong}");
                    new JobEventContainer(postedJob, readerName, StatusManager);
                }
            }
            else if (existsOnDisk && historyEntry != null)
            {
                if (historyEntry.Flag == HistoryFlag.None)
                    HistoryManager.TryUpdateFlag(playlistSong.Hash, HistoryFlag.PreExisting);
            }

            return downloadPosted;
        }

        public bool PostJobToDownload(ScrapedSong song, string readerName)
        {
            return PostJobToDownload(song.ToPlaylistSong(), readerName);
        }

        public void SetError(string reader)
        {
            SetStatus(reader, "Error", UI.FontColor.Red);
        }

        public void SetWarning(string reader)
        {
            SetStatus(reader, "Warning", UI.FontColor.Yellow);
        }

        public void SetStatus(string reader, string subHeader, UI.FontColor color = UI.FontColor.None)
        {
            StatusManager?.SetSubHeader(reader, subHeader);
            if (color != FontColor.None)
                StatusManager?.SetHeaderColor(reader, color);
        }
    }
}
