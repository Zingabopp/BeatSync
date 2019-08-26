using BeatSync.Configs;
using BeatSync.Playlists;
using BeatSync.Utilities;
using SongCore.Data;
using SongFeedReaders;
using SongFeedReaders.DataflowAlternative;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Downloader
{
    public class SongDownloader
    {

        private const string BeatSaverDownloadUrlBase = "https://beatsaver.com/api/download/hash/";
        private static readonly string SongTempPath = Path.GetFullPath(Path.Combine("UserData", "BeatSyncTemp"));
        private readonly string CustomLevelsPath;
        public ConcurrentQueue<PlaylistSong> DownloadQueue { get; private set; }
        private PluginConfig Config;
        public HistoryManager HistoryManager { get; private set; }
        public SongHasher HashSource { get; private set; }

        //private TransformBlock<PlaylistSong, JobResult> DownloadBatch;

        public SongDownloader(PluginConfig config, HistoryManager historyManager, SongHasher hashSource, string customLevelsPath)
        {
            CustomLevelsPath = customLevelsPath;
            Directory.CreateDirectory(CustomLevelsPath);
            HashSource = hashSource;
            DownloadQueue = new ConcurrentQueue<PlaylistSong>();
            HistoryManager = historyManager;
            Config = config;
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
            DownloadResult result;
            var downloadUri = new Uri(BeatSaverDownloadUrlBase + song.Hash.ToLower());
            var downloadTarget = Path.Combine(target, song.Key);
            result = await FileIO.DownloadFileAsync(downloadUri, downloadTarget, true).ConfigureAwait(false);
            return result;
        }


        /// <summary>
        /// This should be redone, return a DownloadResult so other things can take action with regards to HistoryManager.
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
                Logger.log?.Error($"Error downloading {song.Key}: {ex.Message}");
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
            if (config.BeatSaver.Enabled)
            {
                readerTasks.Add(ReadBeatSaver());
            }
            if (config.ScoreSaber.Enabled)
            {
                readerTasks.Add(ReadScoreSaber());
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
                var notInHistory = HistoryManager.TryAdd(playlistSong, 0); // Make sure it's in HistoryManager even if it already exists.
                var didntExist = HashSource.ExistingSongs.TryAdd(pair.Value.Hash, "");
                if (didntExist && notInHistory)
                {
                    //Logger.log?.Info($"Queuing {pair.Value.SongKey} - {pair.Value.SongName} by {pair.Value.MapperName} for download.");

                    DownloadQueue.Enqueue(playlistSong);
                }
                else if (!didntExist && HistoryManager.TryGetValue(pair.Value.Hash, out var value))
                {
                    if (value.Flag == HistoryFlag.None)
                        HistoryManager.TryUpdateFlag(pair.Value.Hash, HistoryFlag.PreExisting);
                }

                allPlaylist?.TryAdd(playlistSong);

            }
            allPlaylist?.TryWriteFile();

        }

        public async Task<Dictionary<string, ScrapedSong>> ReadFeed(IFeedReader reader, IFeedSettings settings, Playlist feedPlaylist = null)
        {
            var feedName = reader.GetFeedName(settings);
            Logger.log?.Info($"Getting songs from {feedName} feed.");
            var songs = await reader.GetSongsFromFeedAsync(settings).ConfigureAwait(false) ?? new Dictionary<string, ScrapedSong>();
            foreach (var scrapedSong in songs.Reverse()) // Reverse so the last songs have the oldest DateTime
            {
                if (HistoryManager.TryGetValue(scrapedSong.Value.Hash, out var historyEntry)
                    && (historyEntry.Flag == HistoryFlag.NotFound
                    || historyEntry.Flag == HistoryFlag.Deleted))
                {
                    continue;
                }
                if (string.IsNullOrEmpty(scrapedSong.Value.SongKey))
                {
                    try
                    {
                        // ScrapedSong doesn't have a Beat Saver key associated with it, probably scraped from ScoreSaber
                        scrapedSong.Value.UpdateFrom(await BeatSaverReader.GetSongByHashAsync(scrapedSong.Key), false);
                    }
                    catch (ArgumentNullException)
                    {
                        Logger.log?.Warn($"Unable to find {scrapedSong.Value?.SongName} by {scrapedSong.Value?.MapperName} on Beat Saver ({scrapedSong.Key})");
                    }
                }
                var song = new PlaylistSong(scrapedSong.Value.Hash, scrapedSong.Value.SongName, scrapedSong.Value.SongKey, scrapedSong.Value.MapperName);

                feedPlaylist?.TryAdd(song);
            }
            feedPlaylist?.TryWriteFile();
            var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log?.Info($"Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")} in the {feedName} feed.");
            return songs;
        }

        #region Feed Read Functions
        public async Task<Dictionary<string, ScrapedSong>> ReadBeastSaber()
        {
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
            }
            catch (Exception ex)
            {
                Logger.log?.Error(ex);
                return null;
            }
            var readerSongs = new Dictionary<string, ScrapedSong>();
            if (config.Bookmarks.Enabled)
            {
                try
                {
                    var feedSettings = config.Bookmarks.ToFeedSettings();
                    var feedPlaylist = config.Bookmarks.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Bookmarks.FeedPlaylist)
                        : null;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (ArgumentException ex)
                {
                    Logger.log?.Critical("Exception in BeastSaber Bookmarks: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeastSaber, Bookmarks.");
                    Logger.log?.Error(ex);
                }


            }
            if (config.Follows.Enabled)
            {
                try
                {
                    var feedSettings = config.Follows.ToFeedSettings();
                    var feedPlaylist = config.Follows.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Follows.FeedPlaylist)
                        : null;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (ArgumentException ex)
                {
                    Logger.log?.Critical("Exception in BeastSaber Follows: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeastSaber, Follows.");
                    Logger.log?.Error(ex);
                }
            }
            if (config.CuratorRecommended.Enabled)
            {
                try
                {
                    var feedSettings = config.CuratorRecommended.ToFeedSettings();
                    var feedPlaylist = config.CuratorRecommended.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.CuratorRecommended.FeedPlaylist)
                        : null;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeastSaber, Curator Recommended.");
                    Logger.log?.Error(ex);
                }
            }
            if (BeatSync.Paused)
                await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
            sw.Stop();
            var totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log?.Info($"Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} from {reader.Name} in {sw.Elapsed.ToString()}");
            return readerSongs;
        }

        public async Task<Dictionary<string, ScrapedSong>> ReadBeatSaver(Playlist allPlaylist = null)
        {
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
                return null;
            }
            var readerSongs = new Dictionary<string, ScrapedSong>();

            if (config.FavoriteMappers.Enabled)
            {
                try
                {
                    var feedSettings = config.FavoriteMappers.ToFeedSettings();
                    var feedPlaylist = config.FavoriteMappers.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.FavoriteMappers.FeedPlaylist)
                        : null;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, FavoriteMappers: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, FavoriteMappers.");
                    Logger.log?.Error(ex);
                }


            }
            if (config.Hot.Enabled)
            {
                try
                {
                    var feedSettings = config.Hot.ToFeedSettings();
                    var feedPlaylist = config.Hot.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Hot.FeedPlaylist)
                        : null;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, Hot: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, Hot.");
                    Logger.log?.Error(ex);
                }
            }
            if (config.Downloads.Enabled)
            {
                try
                {
                    var feedSettings = config.Downloads.ToFeedSettings();
                    var feedPlaylist = config.Downloads.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Downloads.FeedPlaylist)
                        : null;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log?.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log?.Critical("Exception in ReadBeatSaver, Downloads: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadBeatSaver, Downloads.");
                    Logger.log?.Error(ex);
                }
            }
            sw.Stop();
            if (BeatSync.Paused)
                await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
            var totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log?.Info($"Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} from {reader.Name} in {sw.Elapsed.ToString()}");
            return readerSongs;
        }

        public async Task<Dictionary<string, ScrapedSong>> ReadScoreSaber(Playlist allPlaylist = null)
        {
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
                return null;
            }
            var readerSongs = new Dictionary<string, ScrapedSong>();

            if (config.TopRanked.Enabled)
            {
                try
                {
                    var feedSettings = config.TopRanked.ToFeedSettings();
                    var feedPlaylist = config.TopRanked.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.TopRanked.FeedPlaylist)
                        : null;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadScoreSaber, Top Ranked.");
                    Logger.log?.Error(ex);
                }
            }

            if (config.Trending.Enabled)
            {
                try
                {
                    var feedSettings = config.Trending.ToFeedSettings();
                    var feedPlaylist = config.Trending.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.Trending.FeedPlaylist)
                        : null;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadScoreSaber, Trending.");
                    Logger.log?.Error(ex);
                }
            }

            if (config.TopPlayed.Enabled)
            {
                try
                {
                    var feedSettings = config.TopPlayed.ToFeedSettings();
                    var feedPlaylist = config.TopPlayed.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.TopPlayed.FeedPlaylist)
                        : null;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadScoreSaber, Top Played.");
                    Logger.log?.Error(ex);
                }
            }

            if (config.LatestRanked.Enabled)
            {
                try
                {
                    var feedSettings = config.LatestRanked.ToFeedSettings();
                    var feedPlaylist = config.LatestRanked.CreatePlaylist
                        ? PlaylistManager.GetPlaylist(config.LatestRanked.FeedPlaylist)
                        : null;
                    if (BeatSync.Paused)
                        await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error("Exception in ReadScoreSaber, Latest Ranked.");
                    Logger.log?.Error(ex);
                }
            }

            sw.Stop();
            if (BeatSync.Paused)
                await SongFeedReaders.Utilities.WaitUntil(() => !BeatSync.Paused, 500).ConfigureAwait(false);
            var totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log?.Info($"Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} from {reader.Name} in {sw.Elapsed.ToString()}");
            return readerSongs;
        }
        #endregion
    }
}
