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

namespace BeatSync.Downloader
{
    public class SongDownloader
    {

        private const string BeatSaverDownloadUrlBase = "https://beatsaver.com/api/download/hash/";
        private static readonly string SongTempPath = Path.GetFullPath(Path.Combine("UserData", "BeatSyncTemp"));
        private readonly string CustomLevelsPath;
        public ConcurrentDictionary<string, PlaylistSong> RetrievedSongs { get; private set; }
        public ConcurrentQueue<PlaylistSong> DownloadQueue { get; private set; }
        private PluginConfig Config;
        public HistoryManager HistoryManager { get; private set; }
        public SongHasher HashSource { get; private set; }
        public FavoriteMappers FavoriteMappers { get; private set; }

        //private TransformBlock<PlaylistSong, JobResult> DownloadBatch;

        public SongDownloader(PluginConfig config, HistoryManager historyManager, SongHasher hashSource, string customLevelsPath)
        {
            CustomLevelsPath = customLevelsPath;
            Directory.CreateDirectory(CustomLevelsPath);
            HashSource = hashSource;
            DownloadQueue = new ConcurrentQueue<PlaylistSong>();
            RetrievedSongs = new ConcurrentDictionary<string, PlaylistSong>();
            HistoryManager = historyManager;
            FavoriteMappers = new FavoriteMappers();
            FavoriteMappers.Initialize();
            Config = config.Clone();
        }

        public void ProcessJob(JobResult job)
        {
            if (job.Successful)
            {
                HistoryManager.TryUpdateFlag(job.Song, HistoryFlag.Downloaded);
                var recentPlaylist = Config.RecentPlaylistDays > 0 ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncRecent) : null;
                recentPlaylist?.TryAdd(job.Song);

            }
            else if (job.DownloadResult.Status != DownloadResultStatus.Success)
            {
                if (job.DownloadResult.HttpStatusCode == 404)
                {
                    // Song isn't on Beat Saver anymore, keep it in history so it isn't attempted again.
                    Logger.log?.Debug($"Setting 404 flag for {job.Song.ToString()}");
                    if (!HistoryManager.TryUpdateFlag(job.Song, HistoryFlag.NotFound))
                        Logger.log?.Debug($"Failed to update flag for {job.Song.ToString()}");
                    PlaylistManager.RemoveSongFromAll(job.Song);
                }
                else
                {
                    // Download failed for some reason, remove from history so it tries again.
                    HistoryManager.TryRemove(job.Song.Hash);
                }
            }
            else if (job.ZipResult?.ResultStatus != ZipExtractResultStatus.Success)
            {
                // Unzipping failed for some reason, remove from history so it tries again.
                HistoryManager.TryRemove(job.Song.Hash);
            }
        }

        public async Task<List<JobResult>> RunDownloaderAsync(int maxConcurrentDownloads)
        {
            var downloadBatch = new TransformBlock<PlaylistSong, JobResult>(DownloadJob, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = DownloadQueue.Count + 100,
                MaxDegreeOfParallelism = maxConcurrentDownloads,
                EnsureOrdered = false
            });
            //Logger.log?.Info($"Starting downloader.");
            var jobResults = new List<JobResult>();
            try
            {
                Logger.log?.Debug($"RunDownloaderAsync: {DownloadQueue.Count} songs in DownloadQueue.");
                while (DownloadQueue.TryDequeue(out var song))
                {
                    if (downloadBatch.TryReceiveAll(out var jobs))
                    {
                        jobResults.AddRange(jobs.Select(r =>
                        {
                            if (r.Exception != null)
                                return new JobResult() { Exception = r.Exception };
                            return r.Output;
                        }));
                        foreach (var job in jobResults)
                        {
                            if (job.Exception != null)
                                Logger.log?.Warn($"Error in one of the DownloadJobs.\n{job.Exception.Message}\n{job.Exception.StackTrace}");
                            if (BeatSync.Paused)
                                await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                            ProcessJob(job);
                        }
                    }
                    await downloadBatch.SendAsync(song).ConfigureAwait(false);
                }
                downloadBatch.Complete();
                Logger.log?.Debug($"Waiting for Completion.");
                await downloadBatch.Completion.ConfigureAwait(false);
                Logger.log?.Debug($"All downloads should be complete.");
                if (downloadBatch.TryReceiveAll(out var jobsCompleted))
                {
                    jobResults.AddRange(jobsCompleted.Select(r =>
                    {
                        if (r.Exception != null)
                            return new JobResult() { Exception = r.Exception };
                        return r.Output;
                    }));
                    foreach (var job in jobResults)
                    {
                        if (job.Exception != null)
                            Logger.log?.Warn($"Error in one of the DownloadJobs.\n{job.Exception.Message}\n{job.Exception.StackTrace}");
                        if (BeatSync.Paused)
                            await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                        ProcessJob(job);
                    }
                }
            }
            catch (Exception ex)
            { Logger.log?.Error(ex); }
            try
            {
                if (Directory.Exists(SongTempPath))
                    Directory.Delete(SongTempPath, true);
            }
            catch (Exception) { }
            return jobResults;
        }

        /// <summary>
        /// Attempts to download a song to the specified target path.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static async Task<DownloadResult> DownloadSongAsync(PlaylistSong song, string target)
        {
            DownloadResult result = null;
            try
            {
                var downloadUri = new Uri(BeatSaverDownloadUrlBase + song.Hash.ToLower());
                var downloadTarget = Path.Combine(target, song.Key ?? song.Hash);
                result = await FileIO.DownloadFileAsync(downloadUri, downloadTarget, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error downloading song {song.Key ?? song.Hash}.\n{ex.Message}");
                Logger.log?.Debug(ex);
                if (result == null)
                    result = new DownloadResult(null, DownloadResultStatus.Unknown, 0, ex.Message, ex);
            }
            return result;
        }


        /// <summary>
        /// Attempts to delete the downloaded zip when finished.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public async Task<JobResult> DownloadJob(PlaylistSong song)
        {

            bool directoryCreated = false;
            JobResult result = new JobResult() { Song = song };
            bool overwrite = true;
            string extractDirectory = null;
            try
            {
                if (string.IsNullOrEmpty(song?.Key))
                    await song.UpdateSongKeyAsync().ConfigureAwait(false);
                var songDirPath = Path.GetFullPath(Path.Combine(CustomLevelsPath, song.DirectoryName));
                directoryCreated = !Directory.Exists(songDirPath);
                // Won't remove if it fails, why bother with the HashDictionary TryAdd check if we're overwriting/incrementing folder name
                // This doesn't guarantee the song isn't already downloaded
                //if (HashSource.HashDictionary.TryAdd(songDirPath, new SongHashData(0, song.Hash)))
                //{
                if (BeatSync.Paused)
                    await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                var downloadResult = await DownloadSongAsync(song, SongTempPath).ConfigureAwait(false);
                result.DownloadResult = downloadResult;
                if (downloadResult.Status == DownloadResultStatus.Success)
                {
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var zipResult = await Task.Run(() => FileIO.ExtractZip(downloadResult.FilePath, songDirPath, overwrite)).ConfigureAwait(false);
                    // Try to delete zip file
                    try
                    {
                        var deleteSuccessful = await FileIO.TryDeleteAsync(downloadResult.FilePath).ConfigureAwait(false);
                    }
                    catch (IOException ex)
                    {
                        Logger.log?.Warn($"Unable to delete zip file after extraction: {downloadResult.FilePath}.\n{ex.Message}");
                    }

                    result.ZipResult = zipResult;
                    extractDirectory = Path.GetFullPath(zipResult.OutputDirectory);
                    if (!overwrite && !songDirPath.Equals(extractDirectory))
                    {
                        Logger.log?.Debug($"songDirPath {songDirPath} != {extractDirectory}, updating dictionary.");
                        directoryCreated = true;
                        HashSource.ExistingSongs[song.Hash] = extractDirectory;
                    }
                    Logger.log?.Info($"Finished downloading and extracting {song}");
                    var extractedHash = await SongHasher.GetSongHashDataAsync(extractDirectory).ConfigureAwait(false);
                    result.HashAfterDownload = extractedHash.songHash;
                    if (!song.Hash.Equals(extractedHash.songHash))
                        Logger.log?.Warn($"Extracted hash doesn't match Beat Saver hash for {song}");
                    else
                        Logger.log?.Debug($"Extracted hash matches Beat Saver hash for {song}");
                }
                //}
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error downloading {song.Key ?? song.Hash}: {ex.Message}");
                Logger.log?.Debug(ex);
                if (result.Exception == null)
                    result.Exception = ex;
            }
            finally
            {
                if (File.Exists(result.DownloadResult?.FilePath))
                    await FileIO.TryDeleteAsync(result.DownloadResult?.FilePath).ConfigureAwait(false);
            }
            return result;
        }


        public async Task RunReaders()
        {
            List<Task<Dictionary<string, ScrapedSong>>> readerTasks = new List<Task<Dictionary<string, ScrapedSong>>>();
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

            // TODO: Don't use HistoryManager to stop the same song from being added to the download queue, too many problems.

            foreach (var pair in songsToDownload)
            {
                var playlistSong = new PlaylistSong(pair.Value.Hash, pair.Value.SongName, pair.Value.SongKey, pair.Value.MapperName);
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
                if (HistoryManager.TryGetValue(scrapedSong.Value.Hash, out var historyEntry)
                    && (historyEntry.Flag == HistoryFlag.NotFound
                    || historyEntry.Flag == HistoryFlag.Deleted))
                {
                    continue;
                }
                //if (string.IsNullOrEmpty(scrapedSong.Value.SongKey))
                //{
                //    try
                //    {
                //        //Logger.log?.Info($"Grabbing key from BeatSaver: {scrapedSong.Value.SongName} by {scrapedSong.Value.MapperName}");
                //        // ScrapedSong doesn't have a Beat Saver key associated with it, probably scraped from ScoreSaber
                //        scrapedSong.Value.UpdateFrom(await BeatSaverReader.GetSongByHashAsync(scrapedSong.Key), false);
                //    }
                //    catch (ArgumentNullException)
                //    {
                //        Logger.log?.Warn($"Unable to find {scrapedSong.Value?.SongName} by {scrapedSong.Value?.MapperName} on Beat Saver ({scrapedSong.Key})");
                //    }
                //}
                var song = scrapedSong.Value.ToPlaylistSong();
                song.DateAdded = addDate;
                var source = $"{reader.Name}.{reader.GetFeedName(settings)}";
                song.FeedSources.Add(source);
                // Can't do this, it would mess up playlist ordering (i.e. ScoreSaber Top Ranked)
                RetrievedSongs.AddOrUpdate(song.Hash, song, (k, v) => RetrievedSongsUpdater(v, song, source));
                feedPlaylist?.TryAdd(song);
                addDate = addDate - decrement;
            }
            feedPlaylist?.TryWriteFile();

            return songs;
        }

        private PlaylistSong RetrievedSongsUpdater(PlaylistSong existing, PlaylistSong newSong, string source)
        {
            existing.TryAddFeedSource(source);
            return existing;
        }

        #region Feed Read Functions
        public async Task<Dictionary<string, ScrapedSong>> ReadBeastSaber()
        {
            var readerName = "BeastSaber";
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
                SetStatus(readerName, "Running", UI.FontColor.Green);
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
                    PostEvent(readerName, "Starting Feed: Bookmarks...");
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
                    AppendLastEvent(readerName, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                    readerSongs.Merge(songs);
                }
                catch (ArgumentException ex)
                {
                    warning = true;
                    Logger.log?.Critical("Exception in BeastSaber Bookmarks: " + ex.Message);
                    PostEvent(readerName, "Exception in Bookmarks, see log.", UI.FontColor.Red);
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadBeastSaber, Bookmarks.");
                    Logger.log?.Error(ex);
                    PostEvent(readerName, "Exception in Bookmarks, see log.", UI.FontColor.Red);
                }
            }
            else if (string.IsNullOrEmpty(config.Username))
            {
                Logger.log?.Warn("BeastSaber Bookmarks feed is enabled, but a username has not been provided.");
                PostEvent(readerName, "Bookmarks: No Username specified, skipping.", UI.FontColor.Yellow);
            }
            if (config.Follows.Enabled && !string.IsNullOrEmpty(config.Username))
            {
                try
                {
                    PostEvent(readerName, "Starting Feed: Follows...");
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
                    AppendLastEvent(readerName, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                    readerSongs.Merge(songs);
                }
                catch (ArgumentException ex)
                {
                    warning = true;
                    Logger.log?.Critical("Exception in BeastSaber Follows: " + ex.Message);
                    PostEvent(readerName, "Exception in Follows, see log.", UI.FontColor.Red);
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadBeastSaber, Follows.");
                    Logger.log?.Error(ex);
                    PostEvent(readerName, "Exception in Follows, see log.", UI.FontColor.Red);
                }
            }
            else if (string.IsNullOrEmpty(config.Username))
            {
                Logger.log?.Warn("BeastSaber Follows feed is enabled, but a username has not been provided.");
                PostEvent(readerName, "Follows: No Username specified, skipping.", UI.FontColor.Yellow);
                warning = true;
            }
            if (config.CuratorRecommended.Enabled)
            {
                try
                {
                    PostEvent(readerName, "Starting Feed: Curator Recommended...");
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
                    AppendLastEvent(readerName, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeastSaber, Curator Recommended.");
                    Logger.log?.Error(ex);
                    PostEvent(readerName, "Exception in Curator Recommended, see log.", UI.FontColor.Red);
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
                SetStatus(readerName, "Finished with warnings", UI.FontColor.Yellow);
            }
            else
            {
                SetStatus(readerName, "Finished", UI.FontColor.White);
            }
            return readerSongs;
        }

        public async Task<Dictionary<string, ScrapedSong>> ReadBeatSaver(Playlist allPlaylist = null)
        {
            var readerName = "BeatSaver";
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
                    PostEvent(readerName, $"Starting Feed: FavoriteMappers ({FavoriteMappers.Mappers.Count} mappers)...");
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
                    AppendLastEvent(readerName, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                    PostEvent(readerName, "Exception in FavoriteMappers, see log.", UI.FontColor.Red);
                    warning = true;
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, FavoriteMappers: " + ex.Message);
                    PostEvent(readerName, "Exception in FavoriteMappers, see log.", UI.FontColor.Red);
                    warning = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, FavoriteMappers.");
                    PostEvent(readerName, "Exception in FavoriteMappers, see log.", UI.FontColor.Red);
                    Logger.log?.Error(ex);
                    warning = true;
                }
            }
            else if (config.FavoriteMappers.Enabled)
            {
                Logger.log?.Warn("BeatSaver's FavoriteMappers feed is enabled, but no mappers could be found in UserData\\FavoriteMappers.ini");
                PostEvent(readerName, "No mappers found in FavoriteMappers.ini, skipping", UI.FontColor.Yellow);
            }
            if (config.Hot.Enabled)
            {
                try
                {
                    PostEvent(readerName, "Starting Feed: Hot...");
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
                    AppendLastEvent(readerName, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                    PostEvent(readerName, "Exception in Hot, see log.", UI.FontColor.Red);
                    warning = true;
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, Hot: " + ex.Message);
                    PostEvent(readerName, "Exception in Hot, see log.", UI.FontColor.Red);
                    warning = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, Hot.");
                    PostEvent(readerName, "Exception in Hot, see log.", UI.FontColor.Red);
                    Logger.log?.Error(ex);
                    warning = true;
                }
            }
            if (config.Downloads.Enabled)
            {
                try
                {
                    PostEvent(readerName, "Starting Feed: Downloads...");
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
                    AppendLastEvent(readerName, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                    PostEvent(readerName, "Exception in Downloads, see log.", UI.FontColor.Red);
                    warning = true;
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, Downloads: " + ex.Message);
                    PostEvent(readerName, "Exception in Downloads, see log.", UI.FontColor.Red);
                    warning = true;
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, Downloads.");
                    Logger.log?.Error(ex);
                    PostEvent(readerName, "Exception in Downloads, see log.", UI.FontColor.Red);
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
                SetStatus(readerName, "Finished with warnings", UI.FontColor.Yellow);
            }
            else
            {
                SetStatus(readerName, "Finished", UI.FontColor.White);
            }
            return readerSongs;
        }


        public async Task<Dictionary<string, ScrapedSong>> ReadScoreSaber(Playlist allPlaylist = null)
        {
            var readerName = "ScoreSaber";
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
                    PostEvent(readerName, "Starting Feed: TopRanked...");
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
                    AppendLastEvent(readerName, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Top Ranked.");
                    Logger.log?.Error(ex);
                    PostEvent(readerName, "Exception in Top Ranked, see log.", UI.FontColor.Red);
                }
            }

            if (config.Trending.Enabled)
            {
                try
                {
                    PostEvent(readerName, "Starting Feed: Trending...");
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
                    AppendLastEvent(readerName, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Trending.");
                    Logger.log?.Error(ex);
                    PostEvent(readerName, "Exception in Trending, see log.", UI.FontColor.Red);
                }
            }

            if (config.TopPlayed.Enabled)
            {
                try
                {
                    PostEvent(readerName, "Starting Feed: TopPlayed...");
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
                    AppendLastEvent(readerName, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Top Played.");
                    Logger.log?.Error(ex);
                    PostEvent(readerName, "Exception in Top Played, see log.", UI.FontColor.Red);
                }
            }

            if (config.LatestRanked.Enabled)
            {
                try
                {
                    PostEvent(readerName, "Starting Feed: LatestRanked...");
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
                    AppendLastEvent(readerName, $"{(songs.Count == 1 ? "1 song found" : $"{songs.Count} songs found")}.");
                }
                catch (Exception ex)
                {
                    warning = true;
                    Logger.log?.Error("Exception in ReadScoreSaber, Latest Ranked.");
                    Logger.log?.Error(ex);
                    PostEvent(readerName, "Exception in Latest Ranked, see log.", UI.FontColor.Red);
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
                SetStatus(readerName, "Finished with warnings", UI.FontColor.Yellow);
            }
            else
            {
                SetStatus(readerName, "Finished", UI.FontColor.White);
            }
            return readerSongs;
        }
        #endregion

        /// <summary>
        /// Used for setting the Header (reader, subHeader, color).
        /// </summary>
        public event Action<string, string, UI.FontColor> OnSourceEvent;

        /// <summary>
        /// Used for posting a string to the reader's status list (reader, text, color).
        /// </summary>
        public event Action<string, string, UI.FontColor> OnEvent;

        /// <summary>
        /// Used to append the last event for the specified reader.
        /// </summary>
        public event Action<string, string, UI.FontColor> OnAppendEvent;

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
            OnSourceEvent?.Invoke(reader, subHeader, color);
        }

        public void PostEvent(string reader, string text, UI.FontColor color = UI.FontColor.None)
        {
            OnEvent?.Invoke(reader, text, color);
        }

        public void AppendLastEvent(string reader, string text, UI.FontColor color = UI.FontColor.None)
        {
            OnAppendEvent?.Invoke(reader, text, color);
        }
    }
}
