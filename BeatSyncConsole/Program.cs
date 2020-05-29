using BeatSyncConsole.Configs;
using BeatSyncConsole.Utilities;
using BeatSyncLib.Configs;
using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Downloading;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Hashing;
using BeatSyncLib.History;
using BeatSyncPlaylists;
using Newtonsoft.Json;
using SongFeedReaders.Data;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeastSaber;
using SongFeedReaders.Readers.BeatSaver;
using SongFeedReaders.Readers.ScoreSaber;
using System;
using System.Collections.Generic;
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

        internal static JobManager manager = new JobManager(3);
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        internal static IJobBuilder JobBuilder;
        internal static Config Config;
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public static IJobBuilder CreateJobBuilder()
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
            songLocations.AddRange(Config.BeatSaberInstallLocations.Where(l => l.Enabled && l.IsValid()));
            songLocations.AddRange(Config.CustomSongsPaths.Where(l => l.Enabled && l.IsValid()));
            
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
                        Console.WriteLine($"Unable to initialize HistoryManager at '{historyPath}': {ex.Message}");
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
                songHasher = new SongHasher<SongHashData>(songsDirectory);
                songHasher.InitializeAsync().GetAwaiter().GetResult();
                Directory.CreateDirectory(songsDirectory);
                SongTarget songTarget = new DirectoryTarget(songsDirectory, overwriteTarget, songHasher, historyManager, playlistManager);
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
                        Console.WriteLine($"      Job skipped: {c.Song} not wanted by any targets.");
                    else
                        Console.WriteLine($"      Job completed successfully: {c.Song}");
                }
                else
                {
                    Console.WriteLine($"      Job failed: {c.Song}");
                }
            });
            jobBuilder.SetDefaultJobFinishedAsyncCallback(jobFinishedCallback);
            return jobBuilder;

        }

        
        


        static async Task GetScoreSaberAsync()
        {
            ScoreSaberConfig config = Config.BeatSyncConfig.ScoreSaber;
            if (!config.Enabled)
                return;
            ScoreSaberReader reader = new ScoreSaberReader();
            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { config.TopRanked, config.LatestRanked, config.Trending, config.TopPlayed };
            foreach (var feedConfig in feedConfigs.Where(c => c.Enabled))
            {
                Console.WriteLine($"  Starting {feedConfig.GetType().Name} feed...");
                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
                IEnumerable<IJob>? jobs = CreateJobs(results);
                await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());

                ProcessFinishedJobs(jobs, JobBuilder.SongTargets, feedConfig);
                Console.WriteLine($"  Finished {feedConfig.GetType().Name} feed...");
            }
        }

        static async Task GetBeastSaberAsync()
        {
            BeastSaberConfig config = Config.BeatSyncConfig.BeastSaber; 
            if (!config.Enabled)
                return;
            BeastSaberReader reader = new BeastSaberReader(config.Username, config.MaxConcurrentPageChecks);
            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { config.Bookmarks, config.Follows, config.CuratorRecommended };
            List<IPlaylist> playlists = new List<IPlaylist>();
            foreach (var feedConfig in feedConfigs.Where(c => c.Enabled))
            {
                Console.WriteLine($"  Starting {feedConfig.GetType().Name} feed...");
                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
                IEnumerable<IJob>? jobs = CreateJobs(results);
                await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());
                ProcessFinishedJobs(jobs, JobBuilder.SongTargets, feedConfig);
                Console.WriteLine($"  Finished {feedConfig.GetType().Name} feed...");
            }
        }

        static async Task GetBeatSaverAsync()
        {
            BeatSaverConfig config = Config.BeatSyncConfig.BeatSaver;
            if (!config.Enabled)
                return;
            BeatSaverReader reader = new BeatSaverReader(config.MaxConcurrentPageChecks);
            FeedConfigBase[] feedConfigs = new FeedConfigBase[] { config.Hot, config.Downloads };
            foreach (var feedConfig in feedConfigs.Where(c => c.Enabled))
            {
                Console.WriteLine($"  Starting {feedConfig.GetType().Name} feed...");
                FeedResult results = await reader.GetSongsFromFeedAsync(feedConfig.ToFeedSettings()).ConfigureAwait(false);
                IEnumerable<IJob>? jobs = CreateJobs(results);
                await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());

                ProcessFinishedJobs(jobs, JobBuilder.SongTargets, feedConfig);
                Console.WriteLine($"  Finished {feedConfig.GetType().Name} feed...");
            }

            string[] mappers = config.FavoriteMappers.Mappers ?? Array.Empty<string>();
            if (config.FavoriteMappers.Enabled)
            {
                FeedConfigBase feedConfig = config.FavoriteMappers;
                if (mappers.Length > 0)
                {
                    Console.WriteLine("  Starting FavoriteMappers feed...");
                    List<IPlaylist> playlists = new List<IPlaylist>();
                    List<IPlaylist> feedPlaylists = new List<IPlaylist>();
                    foreach (var mapper in mappers)
                    {

                        Console.WriteLine($"  Getting songs by {mapper}...");
                        playlists.Clear();
                        foreach (var targetWithPlaylist in JobBuilder.SongTargets.Where(t => t is ITargetWithPlaylists).Select(t => (ITargetWithPlaylists)t))
                        {
                            PlaylistManager? playlistManager = targetWithPlaylist.PlaylistManager;
                            if (playlistManager != null)
                            {
                                if (Config.BeatSyncConfig.AllBeatSyncSongsPlaylist)
                                    playlists.Add(playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncAll));
                                if (config.FavoriteMappers.CreatePlaylist)
                                {
                                    IPlaylist feedPlaylist;
                                    if (config.FavoriteMappers.SeparateMapperPlaylists)
                                        feedPlaylist = playlistManager.GetOrCreateAuthorPlaylist(mapper);
                                    else
                                        feedPlaylist = playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSaverFavoriteMappers);
                                    feedPlaylists.Add(feedPlaylist);
                                    playlists.Add(feedPlaylist);
                                }
                            }
                        }
                        FeedResult results = await reader.GetSongsFromFeedAsync(config.FavoriteMappers.ToFeedSettings(mapper)).ConfigureAwait(false);
                        IEnumerable<IJob>? jobs = CreateJobs(results);
                        await Task.WhenAll(jobs.Select(j => j.JobTask).ToArray());

                        if (jobs.Any(j => j.Result.Successful) && feedConfig.PlaylistStyle == PlaylistStyle.Replace)
                        {
                            foreach (var feedPlaylist in feedPlaylists)
                            {
                                feedPlaylist.Clear();
                                feedPlaylist.RaisePlaylistChanged();
                            }
                        }
                        ProcessFinishedJobs(jobs, playlists);

                        Console.WriteLine($"  Finished getting songs by {mapper}...");
                    }

                    Console.WriteLine($"  Finished {feedConfig.GetType().Name} feed...");
                }
                else
                {
                    Console.WriteLine("  No FavoriteMappers found, skipping...");
                }
            }
        }
        public static void ProcessFinishedJobs(IEnumerable<IJob>? jobs, IEnumerable<SongTarget> songTargets, FeedConfigBase feedConfig)
        {
            List<IPlaylist> playlists = new List<IPlaylist>();
            List<IPlaylist> feedPlaylists = new List<IPlaylist>();
            foreach (var targetWithPlaylist in songTargets.Where(t => t is ITargetWithPlaylists).Select(t => (ITargetWithPlaylists)t))
            {
                PlaylistManager? playlistManager = targetWithPlaylist.PlaylistManager;
                if (playlistManager != null)
                {
                    if (Config.BeatSyncConfig.AllBeatSyncSongsPlaylist)
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

        public static IEnumerable<IJob>? CreateJobs(FeedResult feedResult)
        {
            if (!feedResult.Successful)
                return null;
            if (feedResult.Songs.Count == 0)
            {
                Console.WriteLine("No songs");
                return Array.Empty<IJob>();
            }
            List<IJob> jobs = new List<IJob>(feedResult.Count);

            foreach (ScrapedSong song in feedResult.Songs.Values)
            {
                Job newJob = JobBuilder.CreateJob(song);
                manager.TryPostJob(newJob, out IJob? postedJob);
                if (postedJob != null)
                    jobs.Add(postedJob);
                else
                    Console.WriteLine($"Posted job is null for {song}, this shouldn't happen.");
            }
            return jobs;
        }
        private static ConfigManager? ConfigManager;
        static async Task Main(string[] args)
        {
            BeatSyncLib.Logger.log = new Loggers.BeatSyncLibLogger();
            ConfigManager = new ConfigManager(ConfigDirectory);
            bool validConfig = await ConfigManager.InitializeConfigAsync().ConfigureAwait(false);
            if (!validConfig)
            {
                Console.WriteLine("BeatSyncConsole cannot run without a valid config, exiting.");
                Console.WriteLine("Press any key to continue...");
                Console.Read();
                return;
            }
            Config = ConfigManager.Config;
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.HttpClientWrapper.HttpClientWrapper());
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
            SongFeedReaders.WebUtils.WebClient.SetUserAgent("BeatSyncConsole/" + version);
            manager = new JobManager(Config.BeatSyncConfig.MaxConcurrentDownloads);
            manager.Start(CancellationToken.None);
            JobBuilder = CreateJobBuilder();
            Task[] tasks = new Task[] { GetBeatSaverAsync(), GetBeastSaberAsync(), GetScoreSaberAsync() };
            await Task.WhenAll(tasks).ConfigureAwait(false);

            await manager.CompleteAsync().ConfigureAwait(false);
            foreach (var target in JobBuilder.SongTargets)
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
                        Console.WriteLine($"Unable to save history at '{targetWithHistory.HistoryManager?.HistoryPath}': {ex.Message}");
                    }
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.Read();
        }
    }
}
