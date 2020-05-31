using BeatSyncConsole.Configs;
using BeatSyncConsole.Loggers;
using BeatSyncConsole.Utilities;
using BeatSyncLib.Configs;
using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Downloading;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Hashing;
using BeatSyncLib.History;
using BeatSyncPlaylists;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using SongFeedReaders.Data;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeastSaber;
using SongFeedReaders.Readers.BeatSaver;
using SongFeedReaders.Readers.ScoreSaber;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;

namespace BeatSyncConsole
{
    class Program
    {
        private const string ConfigDirectory = "configs";
        internal const string ConfigBackupPath = "config.json.bak";
        public static async Task<IJobBuilder> CreateJobBuilderAsync(Config config)
        {
            string tempDirectory = "Temp";
            Directory.CreateDirectory(tempDirectory);
            IDownloadJobFactory downloadJobFactory = new DownloadJobFactory(song =>
            {
                // return new DownloadMemoryContainer();
                return new FileDownloadContainer(Path.Combine(tempDirectory, (song.Key ?? song.Hash) + ".zip"));
            });
            IJobBuilder jobBuilder = new JobBuilder().SetDownloadJobFactory(downloadJobFactory);
            List<ISongLocation> songLocations = new List<ISongLocation>();
            songLocations.AddRange(config.BeatSaberInstallLocations.Where(l => l.Enabled && l.IsValid()));
            songLocations.AddRange(config.CustomSongsPaths.Where(l => l.Enabled && l.IsValid()));

            foreach (ISongLocation location in songLocations)
            {
                bool overwriteTarget = false;
                HistoryManager? historyManager = null;
                SongHasher? songHasher = null;
                PlaylistManager? playlistManager = null;
                if (!string.IsNullOrEmpty(location.HistoryPath))
                {
                    string historyPath = location.HistoryPath;
                    if (!Path.IsPathFullyQualified(historyPath))
                        historyPath = Path.Combine(location.BasePath, historyPath);
                    string historyDirectory = Path.GetDirectoryName(historyPath) ?? string.Empty;
                    try
                    {
                        Directory.CreateDirectory(historyDirectory);
                        historyManager = new HistoryManager(historyPath);
                        historyManager.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Logger.log.Info($"Unable to initialize HistoryManager at '{historyPath}': {ex.Message}");
                    }
                }
                if (!string.IsNullOrEmpty(location.PlaylistDirectory))
                {
                    string playlistDirectory = location.PlaylistDirectory;
                    if (!Path.IsPathFullyQualified(playlistDirectory))
                        playlistDirectory = Path.Combine(location.BasePath, playlistDirectory);
                    Directory.CreateDirectory(playlistDirectory);
                    playlistManager = new PlaylistManager(playlistDirectory);
                }
                string songsDirectory = location.SongsDirectory;
                if (!Path.IsPathFullyQualified(songsDirectory))
                    songsDirectory = Path.Combine(location.BasePath, songsDirectory);
                Directory.CreateDirectory(songsDirectory);
                songHasher = new SongHasher<SongHashData>(songsDirectory);
                await songHasher.InitializeAsync().ConfigureAwait(false);
                Logger.log.Info($"Hashed {songHasher.HashDictionary.Count} songs in {Paths.ReplaceWorkingDirectory(songsDirectory)}.");
                SongTarget songTarget = new DirectoryTarget(songsDirectory, overwriteTarget, songHasher, historyManager, playlistManager);
                //SongTarget songTarget = new MockSongTarget();
                jobBuilder.AddTarget(songTarget);
            }
            JobFinishedAsyncCallback jobFinishedCallback = new JobFinishedAsyncCallback(async (JobResult c) =>
            {
                HistoryEntry entry = c.CreateHistoryEntry();
                foreach (SongTarget target in jobBuilder.SongTargets)
                {
                    // Add entry to history, this should only succeed for jobs that didn't get to the targets.
                    if (target is ITargetWithHistory targetWithHistory && targetWithHistory.HistoryManager != null)
                        targetWithHistory.HistoryManager.TryAdd(c.Song.Hash, entry);
                }
                if (c.Successful)
                {
                    if (c.DownloadResult != null && c.DownloadResult.Status == DownloadResultStatus.Skipped)
                    {
                        //Logger.log.Info($"      Job skipped: {c.Song} not wanted by any targets.");
                    }
                    else
                        Logger.log.Info($"      Job completed successfully: {c.Song}");
                }
                else
                {
                    Logger.log.Info($"      Job failed: {c.Song}");
                }
            });
            jobBuilder.SetDefaultJobFinishedAsyncCallback(jobFinishedCallback);
            return jobBuilder;

        }

        static async Task<JobStats> GetScoreSaberAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager jobManager)
        {
            ScoreSaberConfig sourceConfig = config.ScoreSaber;
            JobStats sourceStats = new JobStats();
            if (!sourceConfig.Enabled)
                return sourceStats;
            ScoreSaberReader reader = new ScoreSaberReader();
            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { sourceConfig.TopRanked, sourceConfig.LatestRanked, sourceConfig.Trending, sourceConfig.TopPlayed };
            if (!feedConfigs.Any(f => f.Enabled))
            {
                Logger.log.Info($"No feeds enabled for {reader.Name}");
                return sourceStats;
            }
            foreach (var feedConfig in feedConfigs.Where(c => c.Enabled))
            {
                Logger.log.Info($"  Starting {feedConfig.GetType().Name} feed...");
                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
                IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager);
                JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
                JobStats feedStats = new JobStats(jobResults);
                ProcessFinishedJobs(jobs, jobBuilder.SongTargets, config, feedConfig);
                Logger.log.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
                sourceStats += feedStats;
            }

            Logger.log.Info($"  Finished ScoreSaber reading: ({sourceStats}).");
            return sourceStats;
        }

        static async Task<JobStats> GetBeastSaberAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager jobManager)
        {
            BeastSaberConfig sourceConfig = config.BeastSaber;
            JobStats sourceStats = new JobStats();
            if (!sourceConfig.Enabled)
                return sourceStats;
            BeastSaberReader reader = new BeastSaberReader(sourceConfig.Username, sourceConfig.MaxConcurrentPageChecks);
            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { sourceConfig.Bookmarks, sourceConfig.Follows, sourceConfig.CuratorRecommended };
            if (!feedConfigs.Any(f => f.Enabled))
            {
                Logger.log.Info($"No feeds enabled for {reader.Name}");
                return sourceStats;
            }
            foreach (var feedConfig in feedConfigs.Where(c => c.Enabled))
            {
                if (string.IsNullOrEmpty(sourceConfig.Username) && feedConfig.GetType() != typeof(BeastSaberCuratorRecommended))
                {
                    Logger.log.Warn($"  {feedConfig.GetType().Name} feed not available without a valid username.");
                    continue;
                }
                Logger.log.Info($"  Starting {feedConfig.GetType().Name} feed...");
                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
                IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager);
                JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
                JobStats feedStats = new JobStats(jobResults);
                ProcessFinishedJobs(jobs, jobBuilder.SongTargets, config, feedConfig);
                Logger.log.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
                sourceStats += feedStats;
            }
            Logger.log.Info($"  Finished BeastSaber reading: ({sourceStats}).");
            return sourceStats;
        }

        static async Task<JobStats> GetBeatSaverAsync(BeatSyncConfig config, IJobBuilder jobBuilder, JobManager jobManager)
        {
            BeatSaverConfig sourceConfig = config.BeatSaver;
            JobStats sourceStats = new JobStats();
            if (!sourceConfig.Enabled)
                return sourceStats;

            BeatSaverReader reader = new BeatSaverReader(sourceConfig.MaxConcurrentPageChecks);
            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { sourceConfig.Hot, sourceConfig.Downloads };
            if (!(feedConfigs.Any(f => f.Enabled) || sourceConfig.FavoriteMappers.Enabled))
            {
                Logger.log.Info($"No feeds enabled for {reader.Name}");
                return sourceStats;
            }
            foreach (var feedConfig in feedConfigs.Where(c => c.Enabled))
            {
                Logger.log.Info($"  Starting {feedConfig.GetType().Name} feed...");
                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
                IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager);
                await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
                JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
                JobStats feedStats = new JobStats(jobResults);
                ProcessFinishedJobs(jobs, jobBuilder.SongTargets, config, feedConfig);
                Logger.log.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
                sourceStats += feedStats;
            }

            string[] mappers = sourceConfig.FavoriteMappers.Mappers ?? Array.Empty<string>();
            if (sourceConfig.FavoriteMappers.Enabled)
            {
                FeedConfigBase feedConfig = sourceConfig.FavoriteMappers;
                if (mappers.Length > 0)
                {
                    Logger.log.Info("  Starting FavoriteMappers feed...");
                    List<IPlaylist> playlists = new List<IPlaylist>();
                    List<IPlaylist> feedPlaylists = new List<IPlaylist>();
                    JobStats feedStats = new JobStats();
                    foreach (var mapper in mappers)
                    {

                        Logger.log.Info($"  Getting songs by {mapper}...");
                        playlists.Clear();
                        foreach (var targetWithPlaylist in jobBuilder.SongTargets.Where(t => t is ITargetWithPlaylists).Select(t => (ITargetWithPlaylists)t))
                        {
                            PlaylistManager? playlistManager = targetWithPlaylist.PlaylistManager;
                            if (playlistManager != null)
                            {
                                if (config.AllBeatSyncSongsPlaylist)
                                    playlists.Add(playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncAll));
                                if (sourceConfig.FavoriteMappers.CreatePlaylist)
                                {
                                    IPlaylist feedPlaylist;
                                    if (sourceConfig.FavoriteMappers.SeparateMapperPlaylists)
                                        feedPlaylist = playlistManager.GetOrCreateAuthorPlaylist(mapper);
                                    else
                                        feedPlaylist = playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSaverFavoriteMappers);
                                    feedPlaylists.Add(feedPlaylist);
                                    playlists.Add(feedPlaylist);
                                }
                            }
                        }
                        FeedResult results = await reader.GetSongsFromFeedAsync(sourceConfig.FavoriteMappers.ToFeedSettings(mapper)).ConfigureAwait(false);
                        IEnumerable<IJob>? jobs = CreateJobs(results, jobBuilder, jobManager);
                        JobResult[] jobResults = await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
                        JobStats mapperStats = new JobStats(jobResults);
                        feedStats += mapperStats;
                        if (jobs.Any(j => j.Result.Successful) && feedConfig.PlaylistStyle == PlaylistStyle.Replace)
                        {
                            foreach (var feedPlaylist in feedPlaylists)
                            {
                                feedPlaylist.Clear();
                                feedPlaylist.RaisePlaylistChanged();
                            }
                        }
                        ProcessFinishedJobs(jobs, playlists);

                        Logger.log.Info($"  Finished getting songs by {mapper}: ({mapperStats}).");
                    }
                    sourceStats += feedStats;
                    Logger.log.Info($"  Finished {feedConfig.GetType().Name} feed: ({feedStats}).");
                }
                else
                {
                    Logger.log.Warn("  No FavoriteMappers found, skipping...");
                }
            }

            Logger.log.Info($"  Finished BeatSaver reading: ({sourceStats}).");
            return sourceStats;
        }

        struct JobStats
        {
            public int Jobs;
            public int FailedJobs;
            public int SkippedJobs;

            public JobStats(int jobs, int failedJobs, int skippedJobs)
            {
                Jobs = jobs;
                FailedJobs = failedJobs;
                SkippedJobs = skippedJobs;
            }

            public JobStats(IEnumerable<JobResult> jobResults)
            {
                Jobs = 0;
                FailedJobs = 0;
                SkippedJobs = 0;
                foreach (var result in jobResults)
                {
                    Jobs++;
                    if (!result.Successful)
                        FailedJobs++;
                    else if (result.TargetResults.All(t => t.SongState != SongState.Wanted))
                        SkippedJobs++;
                }
            }

            public override string ToString()
            {
                if (SkippedJobs > 0)
                    return $"{Jobs - FailedJobs}/{Jobs}, {SkippedJobs} skipped";
                else
                    return $"{Jobs - FailedJobs}/{Jobs}";
            }

            public static JobStats operator -(JobStats a) => new JobStats(-a.Jobs, -a.FailedJobs, -a.SkippedJobs);
            public static JobStats operator +(JobStats a, JobStats b) =>
                new JobStats(a.Jobs + b.Jobs, a.FailedJobs + b.FailedJobs, a.SkippedJobs + b.SkippedJobs);
            public static JobStats operator -(JobStats a, JobStats b) => a + (-b);

        }

        public static void ProcessFinishedJobs(IEnumerable<IJob>? jobs, IEnumerable<SongTarget> songTargets, BeatSyncConfig beatSyncConfig, FeedConfigBase feedConfig)
        {
            List<IPlaylist> playlists = new List<IPlaylist>();
            List<IPlaylist> feedPlaylists = new List<IPlaylist>();
            foreach (var targetWithPlaylist in songTargets.Where(t => t is ITargetWithPlaylists).Select(t => (ITargetWithPlaylists)t))
            {
                PlaylistManager? playlistManager = targetWithPlaylist.PlaylistManager;
                if (playlistManager != null)
                {
                    if (beatSyncConfig.AllBeatSyncSongsPlaylist)
                        playlists.Add(playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncAll));
                    if (feedConfig.CreatePlaylist)
                    {
                        IPlaylist feedPlaylist = playlistManager.GetOrAddPlaylist(feedConfig.FeedPlaylist);
                        playlists.Add(feedPlaylist);
                        feedPlaylists.Add(feedPlaylist);
                    }
                }
            }
            if (jobs.Any(j => j.Result.Successful) && feedConfig.PlaylistStyle == PlaylistStyle.Replace)
            {
                foreach (var feedPlaylist in feedPlaylists)
                {
                    feedPlaylist.Clear();
                    feedPlaylist.RaisePlaylistChanged();
                }
            }
            ProcessFinishedJobs(jobs, playlists);
        }

        public static void ProcessFinishedJobs(IEnumerable<IJob>? jobs, IEnumerable<IPlaylist> playlists)
        {

            DateTime addedTime = DateTime.Now;
            TimeSpan offset = new TimeSpan(0, 0, 0, 0, 1);
            foreach (var job in jobs.Where(j => j.DownloadResult != null && j.DownloadResult.Status != DownloadResultStatus.NetNotFound))
            {
                foreach (var playlist in playlists)
                {
                    IPlaylistSong? addedSong = playlist.Add(job.Song);
                    if (addedSong != null)
                    {
                        addedSong.DateAdded = addedTime;
                        addedTime -= offset;
                    }
                }
            }
            foreach (var playlist in playlists)
            {
                playlist.Sort();
                playlist.RaisePlaylistChanged();
            }
        }

        public static IEnumerable<IJob>? CreateJobs(FeedResult feedResult, IJobBuilder jobBuilder, JobManager jobManager)
        {
            if (!feedResult.Successful)
                return null;
            if (feedResult.Songs.Count == 0)
            {
                Logger.log.Info("No songs");
                return Array.Empty<IJob>();
            }
            List<IJob> jobs = new List<IJob>(feedResult.Count);

            foreach (ScrapedSong song in feedResult.Songs.Values)
            {
                Job newJob = jobBuilder.CreateJob(song);
                jobManager.TryPostJob(newJob, out IJob? postedJob);
                if (postedJob != null)
                    jobs.Add(postedJob);
                else
                    Logger.log.Info($"Posted job is null for {song}, this shouldn't happen.");
            }
            return jobs;
        }
        private static ConfigManager? ConfigManager;

        private static void SetupLogging()
        {
            var consoleWriter = new ConsoleLogWriter
            {
                LogLevel = BeatSyncLib.Logging.LogLevel.Info
            };
            LogManager.AddLogWriter(consoleWriter);
            BeatSyncLib.Logger.log = new BeatSyncLogger("BeatSyncLib");
            try
            {
                string logFilePath = Path.Combine("logs", "log.txt");
                Directory.CreateDirectory("logs");
                LogManager.AddLogWriter(new FileLogWriter(logFilePath));
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Error creating FileLogWriter: {ex.Message}");
            }
        }

        static async Task Main(string[] args)
        {
            try
            {
                SetupLogging();
                string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
                Logger.log.Info($"Starting BeatSyncConsole v{version}");
                ConfigManager = new ConfigManager(ConfigDirectory);
                bool validConfig = await ConfigManager.InitializeConfigAsync().ConfigureAwait(false);
                Config? config = ConfigManager.Config;
                if (validConfig && config != null)
                {
                    SongFeedReaders.WebUtils.Initialize(new WebUtilities.HttpClientWrapper.HttpClientWrapper());
                    SongFeedReaders.WebUtils.WebClient.SetUserAgent("BeatSyncConsole/" + version);
                    JobManager manager = new JobManager(config.BeatSyncConfig.MaxConcurrentDownloads);
                    manager.Start(CancellationToken.None);
                    IJobBuilder jobBuilder = await CreateJobBuilderAsync(config).ConfigureAwait(false);
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    Task<JobStats>[] tasks = new Task<JobStats>[]
                    {
                        GetBeatSaverAsync(config.BeatSyncConfig, jobBuilder, manager),
                        GetBeastSaberAsync(config.BeatSyncConfig, jobBuilder, manager),
                        GetScoreSaberAsync(config.BeatSyncConfig, jobBuilder, manager)
                    };
                    JobStats beatSyncStats = (await Task.WhenAll(tasks).ConfigureAwait(false)).Aggregate((a, b) => a + b);
                    await manager.CompleteAsync().ConfigureAwait(false);
                    foreach (var target in jobBuilder.SongTargets)
                    {
                        if (target is ITargetWithPlaylists targetWithPlaylists)
                        {
                            targetWithPlaylists.PlaylistManager?.StoreAllPlaylists();
                        }
                        if (target is ITargetWithHistory targetWithHistory)
                        {
                            try
                            {
                                targetWithHistory.HistoryManager?.WriteToFile();
                            }
                            catch (Exception ex)
                            {
                                Logger.log.Info($"Unable to save history at '{targetWithHistory.HistoryManager?.HistoryPath}': {ex.Message}");
                            }
                        }
                    }
                    sw.Stop();
                    Logger.log.Info($"Finished after {sw.Elapsed.TotalSeconds}s: {beatSyncStats}");
                }
                else
                {
                    Logger.log.Info("BeatSyncConsole cannot run without a valid config, exiting.");
                }
                LogManager.GetUserInput("Press Enter to continue...");
            }
            catch (Exception ex)
            {
                string message = $"Fatal Error in BeatSyncConsole: {ex.Message}\n{ex.StackTrace}";
                if (LogManager.IsAlive && LogManager.HasWriters && Logger.log != null)
                {
                    Logger.log.Error(message);
                }
                else
                {
                    ConsoleColor previousColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(message);
                    Console.ForegroundColor = previousColor;
                }
                LogManager.Stop();
                Console.WriteLine("Press Enter to continue...");
                Console.Read();
            }
            finally
            {
                LogManager.Stop();
            }
        }
    }
}
