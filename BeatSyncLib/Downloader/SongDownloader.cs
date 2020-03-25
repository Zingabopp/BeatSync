using BeatSyncLib.Configs;
using BeatSyncLib.Hashing;
using BeatSyncLib.History;
using BeatSyncLib.Playlists;
using BeatSyncLib.Playlists.Legacy;
using BeatSyncLib.Utilities;
using SongFeedReaders.Data;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeastSaber;
using SongFeedReaders.Readers.BeatSaver;
using SongFeedReaders.Readers.ScoreSaber;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public class SongDownloader
    {
        private readonly string CustomLevelsPath;
        private BeatSyncConfig Config;
        private IPlaylist RecentPlaylist;
        public HistoryManager HistoryManager { get; private set; }
        public SongHasher HashSource { get; private set; }
        public FavoriteMappers FavoriteMappers { get; private set; }
        public DownloadManager DownloadManager { get; private set; }
        public IStatusManager StatusManager { get; set; }

        public SongDownloader(BeatSyncConfig config, HistoryManager historyManager, SongHasher hashSource, string customLevelsPath)
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

        [Obsolete("Zip handling")]
        public void ProcessJob(IDownloadJob job)
        {
            LegacyPlaylistSong playlistSong = new LegacyPlaylistSong(job.SongHash, job.SongName, job.SongKey, job.LevelAuthorName);
            HistoryEntry historyEntry = HistoryManager.GetOrAdd(playlistSong.Hash, (s) => new HistoryEntry(playlistSong, HistoryFlag.None));
            if (job.DownloadResult.Status == DownloadResultStatus.Success)
            {
                //HistoryManager.TryUpdateFlag(job.SongHash, HistoryFlag.Downloaded);
                historyEntry.Flag = HistoryFlag.Downloaded;
                RecentPlaylist?.TryAdd(job.SongHash, job.SongName, job.SongKey, job.LevelAuthorName);
            }
            else if (job.Status == DownloadJobStatus.Canceled)
            {
                Logger.log?.Warn($"Download canceled for {job.ToString()}");
                if (!HistoryManager.TryRemove(job.SongHash, out HistoryEntry _) && historyEntry != null)
                    historyEntry.Flag = HistoryFlag.Error;
            }
            else if (job.DownloadResult.Status != DownloadResultStatus.Success)
            {
                DownloadResult result = job.DownloadResult;
                switch (result.Status)
                {
                    case DownloadResultStatus.Unknown:
                        Logger.log?.Warn($"Unknown error downloading {job.ToString()}: {result.Exception?.Message}");
                        Logger.log?.Debug(result.Exception);
                        historyEntry.Flag = HistoryFlag.Error;
                        break;
                    case DownloadResultStatus.NetFailed:
                        if (result.HttpStatusCode == 429)
                            Logger.log?.Warn($"Rate limit exceeded for {job.ToString()}");
                        else
                        {
                            Logger.log?.Warn($"Web error downloading {job.ToString()}: {result.Exception?.Message}");
                            Logger.log?.Debug(result.Exception);
                        }
                        // Download failed for some reason, remove from history so it tries again.
                        historyEntry.Flag = HistoryFlag.Error;
                        break;
                    case DownloadResultStatus.IOFailed:
                        Logger.log?.Warn($"IO error downloading {job.ToString()}: {result.Exception?.Message}");
                        Logger.log?.Debug(result.Exception);
                        historyEntry.Flag = HistoryFlag.Error;
                        break;
                    case DownloadResultStatus.InvalidRequest:
                        Logger.log?.Warn($"Invalid URI provided for {job.ToString()}");
                        historyEntry.Flag = HistoryFlag.Error;
                        break;
                    case DownloadResultStatus.NetNotFound:
                        Logger.log?.Warn($"{job.ToString()} was deleted from BeatSaver.");
                        //Logger.log?.Debug($"Setting 404 flag for {playlistSong.ToString()}");
                        // Song isn't on Beat Saver anymore, keep it in history so it isn't attempted again.
                        historyEntry.Flag = HistoryFlag.BeatSaverNotFound;
                        PlaylistManager.RemoveSongFromAll(job.SongHash);
                        break;
                    case DownloadResultStatus.Canceled:
                        Logger.log?.Warn($"Download canceled for {job.ToString()}");
                        if (!HistoryManager.TryRemove(job.SongHash, out HistoryEntry _) && historyEntry != null)
                            historyEntry.Flag = HistoryFlag.Error;
                        break;
                    default:
                        Logger.log?.Warn($"Uncaught error downloading {job.ToString()}: {result.Exception?.Message}");
                        Logger.log?.Debug(result.Exception);
                        historyEntry.Flag = HistoryFlag.Error;
                        break;
                }
            }
            //else if (job.DownloadResult.ZipResult?.ResultStatus != ZipExtractResultStatus.Success)
            //{
            //    var result = job.DownloadResult.ZipResult;
            //    switch (result.ResultStatus)
            //    {
            //        case ZipExtractResultStatus.Unknown:
            //            Logger.log?.Warn($"Unknown error extracting {job.ToString()}: {result.Exception?.Message}");
            //            break;
            //        case ZipExtractResultStatus.SourceFailed:
            //            Logger.log?.Warn($"Source error extracting {job.ToString()}: {result.Exception?.Message}");
            //            break;
            //        case ZipExtractResultStatus.DestinationFailed:
            //            Logger.log?.Warn($"Destination error extracting {job.ToString()}: {result.Exception?.Message}");
            //            break;
            //        default:
            //            break;
            //    }
            //    // Unzipping failed for some reason, remove from history so it tries again.
            //    Logger.log?.Debug(result.Exception);
            //    historyEntry.Flag = HistoryFlag.Error;
            //    //HistoryManager.TryRemove(job.SongHash, out var _);
            //}
        }

        /// <summary>
        /// Signals the DownloadManager to complete the remaining downloads, returns a List of the JobResults.
        /// </summary>
        /// <exception cref="TaskCanceledException"></exception>
        /// <returns></returns>
        public async Task<List<IJob>> WaitDownloadCompletionAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return new List<IJob>();
            List<IJob> jobs = null;
            List<IJob> processedJobs = new List<IJob>();
            try
            {
                Logger.log?.Debug($"Waiting for Completion.");
                await DownloadManager.CompleteAsync().ConfigureAwait(false);
                Logger.log?.Debug($"All downloads should be complete.");
                jobs = DownloadManager.CompletedJobs.ToList();
                //foreach (var job in jobs)
                //{
                //    if (BeatSync.Paused)
                //        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500, cancellationToken).ConfigureAwait(false);
                //    if (cancellationToken.IsCancellationRequested)
                //        throw new TaskCanceledException(Task.FromResult(processedJobs));
                //    // TODO: Should probably just have ProcessJob be part of the Download job...
                //    ProcessJob(job);
                //    processedJobs.Add(job);
                //}
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException)
                    throw;
                Logger.log?.Error($"Error processing downloads:\n {ex.Message}");
                Logger.log?.Debug($"Error processing downloads:\n {ex.StackTrace}");
            }
            return processedJobs;
        }

        public async Task RunReaders(CancellationToken cancellationToken)
        {
            List<Task<Dictionary<string, ScrapedSong>>> readerTasks = new List<Task<Dictionary<string, ScrapedSong>>>();
            DownloadManager.Start(cancellationToken);
            BeatSyncConfig config = Config;
            if (config.BeastSaber.Enabled)
            {
                readerTasks.Add(ReadBeastSaber(cancellationToken));
            }
            else
            {
                SetStatus(BeastSaberReader.NameKey, "Disabled", FontColor.White);
            }
            if (config.BeatSaver.Enabled)
            {
                readerTasks.Add(ReadBeatSaver(cancellationToken));
            }
            else
            {
                SetStatus(BeatSaverReader.NameKey, "Disabled", FontColor.White);
            }
            if (config.ScoreSaber.Enabled)
            {
                readerTasks.Add(ReadScoreSaber(cancellationToken));
            }
            else
            {
                SetStatus(ScoreSaberReader.NameKey, "Disabled", FontColor.White);
            }
            try
            {
                await Task.WhenAll(readerTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error in reading feeds.\n{ex.Message}\n{ex.StackTrace}");
            }

            Logger.log?.Info($"Finished reading feeds.");
            Dictionary<string, ScrapedSong> songsToDownload = new Dictionary<string, ScrapedSong>();
            foreach (Task<Dictionary<string, ScrapedSong>> readTask in readerTasks)
            {
                if (!readTask.DidCompleteSuccessfully())
                {
                    Logger.log?.Warn("Task not successful, skipping.");
                    continue;
                }
                Logger.log?.Debug($"Queuing songs from task.");
                songsToDownload.Merge(await readTask.ConfigureAwait(false));
            }
            Logger.log?.Info($"Found {songsToDownload.Count} unique songs.");
            IPlaylist allPlaylist = config.AllBeatSyncSongsPlaylist ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncAll) : null;

            foreach (KeyValuePair<string, ScrapedSong> pair in songsToDownload)
            {
                allPlaylist?.TryAdd(pair.Value.Hash, pair.Value.Name, pair.Value.Key, pair.Value.LevelAuthorName);
            }
            allPlaylist?.TryStore();

        }

        #region Feed Read Functions
        public async Task<FeedResult> ReadFeed(IFeedReader reader, IFeedSettings settings, int postId, IPlaylist feedPlaylist, PlaylistStyle playlistStyle, CancellationToken cancellationToken)
        {
            //Logger.log?.Info($"Getting songs from {feedName} feed.");
            string feedName = reader.GetFeedName(settings);
            FeedResult feedResult = await reader.GetSongsFromFeedAsync(settings, null, cancellationToken).ConfigureAwait(false) ?? new FeedResult(new Dictionary<string, ScrapedSong>(), null, null, FeedResultError.Error);
            if (feedResult.Count > 0 && playlistStyle == PlaylistStyle.Replace)
                feedPlaylist.Clear();
            DateTime addDate = DateTime.Now;
            TimeSpan decrement = new TimeSpan(1);
            int skippedForHistory = 0;
            foreach (KeyValuePair<string, ScrapedSong> scrapedSong in feedResult.Songs)
            {
                IFeedSong song;
                if (HistoryManager != null)
                {
                    // Skip songs that were deleted or not found on Beat Saver.
                    bool inHistory = HistoryManager.TryGetValue(scrapedSong.Value.Hash, out HistoryEntry historyEntry);
                    if (inHistory && (historyEntry.Flag == HistoryFlag.BeatSaverNotFound
                        || historyEntry.Flag == HistoryFlag.Deleted))
                    {
                        skippedForHistory++;
                        continue;
                    }
                    song = scrapedSong.Value.ToFeedSong<LegacyPlaylistSong>();
                    if (!inHistory && HashSource.ExistingSongs.ContainsKey(scrapedSong.Value.Hash))
                    {
                        HistoryManager.TryAdd(song, HistoryFlag.PreExisting);
                    }
                }
                else
                    song = scrapedSong.Value.ToFeedSong<LegacyPlaylistSong>();
                song.DateAdded = addDate;
                string source = $"{reader.Name}.{feedName}";
                song.AddFeedSource(source);
                feedPlaylist?.TryAdd(song);
                addDate = addDate - decrement;
            }
            feedPlaylist?.TryStore();
            int pages = feedResult.Songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            string historyPostFix = string.Empty;
            string appendText = string.Empty;
            if (feedResult.Successful)
            {
                if (skippedForHistory > 0)
                {
                    Logger.log?.Critical($"Skipped {skippedForHistory} songs in {reader.Name}.{feedName}");
                    historyPostFix = $" Skipped {skippedForHistory} {(skippedForHistory == 1 ? "song" : $"songs")} in history.";
                }
                if (settings is BeatSaverFeedSettings beatSaverSettings && beatSaverSettings.Feed == BeatSaverFeedName.Author)
                {
                    Logger.log?.Info($"   FavoriteMappers: Found {feedResult.Count} songs by {beatSaverSettings.SearchQuery.Value.Criteria}.{historyPostFix}");
                }
                else
                {
                    Logger.log?.Info($"{reader.Name}.{feedName} Feed: Found {feedResult.Count} songs from {pages} {(pages == 1 ? "page" : "pages")}.{historyPostFix}");
                }
            }
            FontColor color = FontColor.None;
            if (feedResult.ErrorCode != FeedResultError.Error)
                appendText = $"{(feedResult.Count == 1 ? "1 song found" : $"{feedResult.Count} songs found")}.{historyPostFix}";
            if (feedResult.ErrorCode > FeedResultError.None)
            {
                string errorText = "Warning";
                color = FontColor.Yellow;
                if (feedResult.Exception is FeedReaderException feedReaderException)
                {
                    if (feedReaderException.FailureCode == FeedReaderFailureCode.SourceFailed)
                    {
                        errorText = "Site Failure";
                    }
                    else if (feedReaderException.FailureCode == FeedReaderFailureCode.Cancelled)
                    {
                        errorText = "Cancelled";
                    }
                    else if (feedResult.Songs.Count == 0)
                    {
                        errorText = "Error";
                    }
                    color = FontColor.Red;
                }
                else
                {
                    if (feedResult.PageErrors.Count > 0)
                        errorText = string.Join(", ", feedResult.PageErrors.Select(e => e.ErrorToString()));
                    else
                        errorText = "Unknown Error";
                    if (feedResult.ErrorCode == FeedResultError.Error)
                        color = FontColor.Red;
                }
                appendText = $"{appendText} ({errorText})";
                StatusManager?.SetHeaderColor(reader.Name, FontColor.Yellow);
            }
            StatusManager?.AppendPost(postId, appendText, color);
            return feedResult;
        }

        public async Task<Dictionary<string, ScrapedSong>> ReadBeastSaber(CancellationToken cancellationToken)
        {
            string readerName = string.Empty; // BeastSaberReader
            bool error = false;
            bool warning = false;
            if (cancellationToken.IsCancellationRequested)
                return null;
            await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log?.Info("Starting BeastSaber reading");

            BeastSaberConfig config = Config.BeastSaber;
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
            Dictionary<string, ScrapedSong> readerSongs = new Dictionary<string, ScrapedSong>();
            Dictionary<string, FeedResult> feedResults = new Dictionary<string, FeedResult>();
            if (config.Bookmarks.Enabled && !string.IsNullOrEmpty(config.Username))
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Bookmarks...") ?? 0;
                    IFeedSettings feedSettings = config.Bookmarks.ToFeedSettings();
                    IPlaylist feedPlaylist = config.Bookmarks.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Bookmarks.FeedPlaylist)
                        : null;
                    PlaylistStyle playlistStyle = config.Bookmarks.PlaylistStyle;
                    if (cancellationToken.IsCancellationRequested)
                        return readerSongs;
                    await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
                    FeedResult feedResult = await ReadFeed(reader, feedSettings, postId, feedPlaylist, playlistStyle, cancellationToken).ConfigureAwait(false);
                    feedResults.Add(feedSettings.FeedName, feedResult);
                    readerSongs.Merge(feedResult.Songs);
                }
                catch (ArgumentException ex)
                {
                    warning = true;
                    Logger.log?.Critical("Exception in BeastSaber Bookmarks: " + ex.Message);
                    int postId = StatusManager?.Post(readerName, "Exception in Bookmarks, see log.", FontColor.Red) ?? 0;
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadBeastSaber, Bookmarks.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Bookmarks, see log.", FontColor.Red) ?? 0;
                }
            }
            else if (string.IsNullOrEmpty(config.Username))
            {
                Logger.log?.Warn("BeastSaber Bookmarks feed is enabled, but a username has not been provided.");
                int postId = StatusManager?.Post(readerName, "Bookmarks: No Username specified, skipping.", FontColor.Yellow) ?? 0;
            }
            if (config.Follows.Enabled && !string.IsNullOrEmpty(config.Username))
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Follows...") ?? 0;
                    IFeedSettings feedSettings = config.Follows.ToFeedSettings();
                    IPlaylist feedPlaylist = config.Follows.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Follows.FeedPlaylist)
                        : null;
                    PlaylistStyle playlistStyle = config.Follows.PlaylistStyle;
                    if (cancellationToken.IsCancellationRequested)
                        return readerSongs;
                    await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
                    FeedResult feedResult = await ReadFeed(reader, feedSettings, postId, feedPlaylist, playlistStyle, cancellationToken).ConfigureAwait(false);
                    feedResults.Add(feedSettings.FeedName, feedResult);
                    readerSongs.Merge(feedResult.Songs);
                }
                catch (ArgumentException ex)
                {
                    warning = true;
                    Logger.log?.Critical("Exception in BeastSaber Follows: " + ex.Message);
                    int postId = StatusManager?.Post(readerName, "Exception in Follows, see log.", FontColor.Red) ?? 0;
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadBeastSaber, Follows.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Follows, see log.", FontColor.Red) ?? 0;
                }
            }
            else if (string.IsNullOrEmpty(config.Username))
            {
                Logger.log?.Warn("BeastSaber Follows feed is enabled, but a username has not been provided.");
                int postId = StatusManager?.Post(readerName, "Follows: No Username specified, skipping.", FontColor.Yellow) ?? 0;
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
                    IFeedSettings feedSettings = config.CuratorRecommended.ToFeedSettings();
                    IPlaylist feedPlaylist = config.CuratorRecommended.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.CuratorRecommended.FeedPlaylist)
                        : null;
                    PlaylistStyle playlistStyle = config.CuratorRecommended.PlaylistStyle;
                    if (cancellationToken.IsCancellationRequested)
                        return readerSongs;
                    await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
                    FeedResult feedResult = await ReadFeed(reader, feedSettings, postId, feedPlaylist, playlistStyle, cancellationToken).ConfigureAwait(false);
                    feedResults.Add(feedSettings.FeedName, feedResult);
                    readerSongs.Merge(feedResult.Songs);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeastSaber, Curator Recommended.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Curator Recommended, see log.", FontColor.Red) ?? 0;
                }
            }
            if (cancellationToken.IsCancellationRequested)
                return readerSongs;
            await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
            sw.Stop();
            int totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log?.Info($"{reader.Name}: Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} in {sw.Elapsed.ToString()}");
            if (error)
            {

            }
            else if (warning)
            {
                SetStatus(readerName, "Finished Reading with warnings", FontColor.Yellow);
            }
            else
            {
                //SetStatus(readerName, "Finished Reading");
            }
            if (cancellationToken.IsCancellationRequested)
                return readerSongs;
            await Task.Delay(2000).ConfigureAwait(false); // Wait a bit before clearing.
            await FinishFeed(readerName, readerSongs.Values, cancellationToken).ConfigureAwait(false);
            return readerSongs;
        }

        public async Task<Dictionary<string, ScrapedSong>> ReadBeatSaver(CancellationToken cancellationToken)
        {
            string readerName = string.Empty; // BeatSaverReader
            bool warning = false;
            bool error = false;
            await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log?.Info("Starting BeatSaver reading");

            BeatSaverConfig config = Config.BeatSaver;
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
            Dictionary<string, ScrapedSong> readerSongs = new Dictionary<string, ScrapedSong>();

            if (config.FavoriteMappers.Enabled && (FavoriteMappers.Mappers?.Count() ?? 0) > 0)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, $"Starting Feed: FavoriteMappers ({FavoriteMappers.Mappers.Count} mappers)...") ?? 0;
                    StatusManager?.PinPost(postId);
                    BeatSaverFeedSettings feedSettings = config.FavoriteMappers.ToFeedSettings() as BeatSaverFeedSettings;
                    IPlaylist feedPlaylist = null;
                    if (!config.FavoriteMappers.SeparateMapperPlaylists)
                    {
                        feedPlaylist = config.FavoriteMappers.CreatePlaylist
                            ? PlaylistManager.GetPlaylist(config.FavoriteMappers.FeedPlaylist)
                            : null;
                    }

                    PlaylistStyle playlistStyle = config.FavoriteMappers.PlaylistStyle;
                    Dictionary<string, ScrapedSong> songs = new Dictionary<string, ScrapedSong>();
                    int[] authorPosts = new int[FavoriteMappers.Mappers.Count];
                    int postIndex = 0;
                    SearchQueryBuilder queryBuilder = new SearchQueryBuilder(BeatSaverSearchType.author, string.Empty);
                    foreach (string author in FavoriteMappers.Mappers)
                    {
                        queryBuilder.Criteria = author;
                        feedSettings.SearchQuery = queryBuilder.GetQuery();
                        await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
                        authorPosts[postIndex] = StatusManager?.Post(readerName, $"  {author}...") ?? 0;
                        if (config.FavoriteMappers.CreatePlaylist && config.FavoriteMappers.SeparateMapperPlaylists)
                        {
                            string playlistFileName = $"{author}.bplist";
                            feedPlaylist = PlaylistManager.GetOrAdd(playlistFileName, () => new LegacyPlaylist(playlistFileName, author, "BeatSync", PlaylistManager.PlaylistImageLoaders[BuiltInPlaylist.BeatSaverMapper].Value));
                        }
                        FeedResult authorSongs = await ReadFeed(reader, feedSettings, authorPosts[postIndex], feedPlaylist, playlistStyle, cancellationToken).ConfigureAwait(false);
                        postIndex++;
                        songs.Merge(authorSongs.Songs);
                    }
                    await Task.Delay(1000);
                    for (int i = 0; i < authorPosts.Length; i++)
                    {
                        StatusManager?.RemovePost(authorPosts[i]);
                    }
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                    int postId = StatusManager?.Post(readerName, "Exception in FavoriteMappers, see log.", FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, FavoriteMappers: " + ex.Message);
                    int postId = StatusManager?.Post(readerName, "Exception in FavoriteMappers, see log.", FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, FavoriteMappers.");
                    int postId = StatusManager?.Post(readerName, "Exception in FavoriteMappers, see log.", FontColor.Red) ?? 0;
                    Logger.log?.Error(ex);
                    warning = true;
                }
            }
            else if (config.FavoriteMappers.Enabled)
            {
                Logger.log?.Warn("BeatSaver's FavoriteMappers feed is enabled, but no mappers could be found in UserData\\FavoriteMappers.ini");
                int postId = StatusManager?.Post(readerName, "No mappers found in FavoriteMappers.ini, skipping", FontColor.Yellow) ?? 0;
            }
            if (config.Hot.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Hot...") ?? 0;
                    StatusManager?.PinPost(postId);
                    IFeedSettings feedSettings = config.Hot.ToFeedSettings();
                    IPlaylist feedPlaylist = config.Hot.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Hot.FeedPlaylist)
                        : null;
                    PlaylistStyle playlistStyle = config.Hot.PlaylistStyle;
                    await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
                    FeedResult songs = await ReadFeed(reader, feedSettings, postId, feedPlaylist, playlistStyle, cancellationToken).ConfigureAwait(false);
                    readerSongs.Merge(songs.Songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                    int postId = StatusManager?.Post(readerName, "Exception in Hot, see log.", FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, Hot: " + ex.Message);
                    int postId = StatusManager?.Post(readerName, "Exception in Hot, see log.", FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, Hot.");
                    int postId = StatusManager?.Post(readerName, "Exception in Hot, see log.", FontColor.Red) ?? 0;
                    Logger.log?.Error(ex);
                    warning = true;
                }
            }
            if (config.Downloads.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Downloads...") ?? 0;
                    StatusManager?.PinPost(postId);
                    IFeedSettings feedSettings = config.Downloads.ToFeedSettings();
                    IPlaylist feedPlaylist = config.Downloads.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Downloads.FeedPlaylist)
                        : null;
                    PlaylistStyle playlistStyle = config.Downloads.PlaylistStyle;
                    await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
                    FeedResult songs = await ReadFeed(reader, feedSettings, postId, feedPlaylist, playlistStyle, cancellationToken).ConfigureAwait(false);

                    readerSongs.Merge(songs.Songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                    int postId = StatusManager?.Post(readerName, "Exception in Downloads, see log.", FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, Downloads: " + ex.Message);
                    int postId = StatusManager?.Post(readerName, "Exception in Downloads, see log.", FontColor.Red) ?? 0;
                    warning = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, Downloads.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Downloads, see log.", FontColor.Red) ?? 0;
                    warning = true;
                }
            }
            sw.Stop();
            await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
            int totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log?.Info($"{reader.Name}: Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} in {sw.Elapsed.ToString()}");
            if (error)
            {

            }
            else if (warning)
            {
                SetStatus(readerName, "Finished Reading Feeds with warnings", FontColor.Yellow);
            }
            else
            {
                SetStatus(readerName, "Finished Reading Feeds", FontColor.White);
            }
            await Task.Delay(2000); // Wait a bit before clearing.
            await FinishFeed(readerName, readerSongs.Values, cancellationToken).ConfigureAwait(false);
            return readerSongs;
        }

        public async Task<Dictionary<string, ScrapedSong>> ReadScoreSaber(CancellationToken cancellationToken)
        {
            string readerName = string.Empty; // ScoreSaberReader
            bool error = false;
            bool warning = false;
            await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log?.Info("Starting ScoreSaber reading");

            ScoreSaberConfig config = Config.ScoreSaber;
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
            Dictionary<string, ScrapedSong> readerSongs = new Dictionary<string, ScrapedSong>();

            if (config.TopRanked.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: TopRanked...") ?? 0;
                    IFeedSettings feedSettings = config.TopRanked.ToFeedSettings();
                    IPlaylist feedPlaylist = config.TopRanked.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.TopRanked.FeedPlaylist)
                        : null;
                    PlaylistStyle playlistStyle = config.TopRanked.PlaylistStyle;
                    await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
                    FeedResult songs = await ReadFeed(reader, feedSettings, postId, feedPlaylist, playlistStyle, cancellationToken).ConfigureAwait(false);

                    readerSongs.Merge(songs.Songs);
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Top Ranked.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Top Ranked, see log.", FontColor.Red) ?? 0;
                }
            }

            if (config.Trending.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: Trending...") ?? 0;
                    IFeedSettings feedSettings = config.Trending.ToFeedSettings();
                    IPlaylist feedPlaylist = config.Trending.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Trending.FeedPlaylist)
                        : null;
                    PlaylistStyle playlistStyle = config.Trending.PlaylistStyle;
                    await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
                    FeedResult songs = await ReadFeed(reader, feedSettings, postId, feedPlaylist, playlistStyle, cancellationToken).ConfigureAwait(false);

                    readerSongs.Merge(songs.Songs);
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Trending.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Trending, see log.", FontColor.Red) ?? 0;
                }
            }

            if (config.TopPlayed.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: TopPlayed...") ?? 0;
                    IFeedSettings feedSettings = config.TopPlayed.ToFeedSettings();
                    IPlaylist feedPlaylist = config.TopPlayed.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.TopPlayed.FeedPlaylist)
                        : null;
                    PlaylistStyle playlistStyle = config.TopPlayed.PlaylistStyle;
                    await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
                    FeedResult songs = await ReadFeed(reader, feedSettings, postId, feedPlaylist, playlistStyle, cancellationToken).ConfigureAwait(false);

                    readerSongs.Merge(songs.Songs);
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Top Played.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Top Played, see log.", FontColor.Red) ?? 0;
                }
            }

            if (config.LatestRanked.Enabled)
            {
                try
                {
                    int postId = StatusManager?.Post(readerName, "Starting Feed: LatestRanked...") ?? 0;
                    IFeedSettings feedSettings = config.LatestRanked.ToFeedSettings();
                    IPlaylist feedPlaylist = config.LatestRanked.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.LatestRanked.FeedPlaylist)
                        : null;
                    PlaylistStyle playlistStyle = config.LatestRanked.PlaylistStyle;
                    await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
                    FeedResult songs = await ReadFeed(reader, feedSettings, postId, feedPlaylist, playlistStyle, cancellationToken).ConfigureAwait(false);

                    readerSongs.Merge(songs.Songs);
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Latest Ranked.");
                    Logger.log?.Error(ex);
                    int postId = StatusManager?.Post(readerName, "Exception in Latest Ranked, see log.", FontColor.Red) ?? 0;
                }
            }

            sw.Stop();
            await Util.WaitForPause(cancellationToken).ConfigureAwait(false);
            int totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log?.Info($"{reader.Name}: Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} in {sw.Elapsed.ToString()}");
            if (error)
            {

            }
            else if (warning)
            {
                SetStatus(readerName, "Finished Reading Feeds with warnings", FontColor.Yellow);
            }
            else
            {
                SetStatus(readerName, "Finished Reading Feeds", FontColor.White);
            }
            await Task.Delay(2000).ConfigureAwait(false); // Wait a bit before clearing.
            await FinishFeed(readerName, readerSongs.Values, cancellationToken).ConfigureAwait(false);
            return readerSongs;
        }

        public async Task FinishFeed(string readerName, IEnumerable<ScrapedSong> readerSongs, CancellationToken cancellationToken)
        {
            if (readerSongs.Count() > 0)
            {
                StatusManager.Clear(readerName);
                await Task.Delay(100).ConfigureAwait(false);
                int songsPosted = 0;
                bool finished = false;
                Func<bool> finishedPosting = () => finished;
                foreach (ScrapedSong song in readerSongs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    if (PostJobToDownload(song, readerName, finishedPosting))
                        songsPosted++;
                }
                finished = true;
                if (songsPosted > 0)
                    SetStatus(readerName, $"Downloading {songsPosted} {(songsPosted == 1 ? "song" : "songs")}");
                else
                    SetStatus(readerName, $"No new songs found");
            }
            else
                SetStatus(readerName, $"No new songs found");

        }
        #endregion

        [Obsolete("JobEventContainer thing")]
        public bool PostJobToDownload(IPlaylistSong playlistSong, string readerName, Func<bool> finishedPosting)
        {
            bool downloadPosted = false;
            //var inHistory = HistoryManager.TryGetValue(playlistSong.Hash, out var historyEntry);
            //var existsOnDisk = HashSource.ExistingSongs.TryGetValue(playlistSong.Hash, out var _);
            //if (!existsOnDisk && (!inHistory || historyEntry.Flag == HistoryFlag.Error))
            //{
            //    //Logger.log?.Info($"Queuing {pair.Value.SongKey} - {pair.Value.SongName} by {pair.Value.MapperName} for download.");
            //    // TODO: fix?
            //    downloadPosted = DownloadManager.TryPostJob(new DownloadJob(playlistSong, null), out var postedJob);
            //    if (downloadPosted && postedJob != null)
            //    {
            //        //Logger.log?.Info($"{readerName} posted job {playlistSong}");
            //        postedJob.JobFinished += PostedJob_OnJobFinished;
            //        //new JobEventContainer(postedJob, readerName, StatusManager, finishedPosting);
            //    }
            //}
            //else if (existsOnDisk && historyEntry != null)
            //{
            //    if (historyEntry.Flag == HistoryFlag.None)
            //        HistoryManager.TryUpdateFlag(playlistSong.Hash, HistoryFlag.PreExisting);
            //}

            return downloadPosted;
        }

        private void PostedJob_OnJobFinished(object sender, DownloadJobFinishedEventArgs e)
        {
            if (sender is IDownloadJob job)
            {
                try
                {
                    ProcessJob(job);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error processing downloads:\n {ex.Message}");
                    Logger.log?.Debug($"Error processing downloads:\n {ex.StackTrace}");
                }
            }
        }

        public bool PostJobToDownload(ScrapedSong song, string readerName, Func<bool> finishedPosting)
        {
            return PostJobToDownload(song.ToFeedSong<LegacyPlaylistSong>(), readerName, finishedPosting);
        }

        public void SetError(string reader)
        {
            SetStatus(reader, "Error", FontColor.Red);
        }

        public void SetWarning(string reader)
        {
            SetStatus(reader, "Warning", FontColor.Yellow);
        }

        public void SetStatus(string reader, string subHeader, FontColor statusLevel = FontColor.White)  //FontColor color = FontColor.None)
        {

            StatusManager?.SetSubHeader(reader, subHeader);
            ReaderStatusChanged?.Invoke(this, new ReaderStatusEventArgs(reader, subHeader, statusLevel));
            //if (color != FontColor.None)
            //    StatusManager?.SetHeaderColor(reader, color);
        }

        public event ReaderStatusEventHandler ReaderStatusChanged;
    }
    public delegate void ReaderStatusEventHandler(object sender, ReaderStatusEventArgs a);

    public class ReaderStatusEventArgs
        : EventArgs
    {
        public string Reader { get; }
        public string Subheader { get; }
        public FontColor StatusLevel { get; }
        public ReaderStatusEventArgs(string reader, string subHeader, FontColor readerStatusLevel)
        {
            Reader = reader;
            Subheader = subHeader;
            StatusLevel = readerStatusLevel;
        }

    }
}
