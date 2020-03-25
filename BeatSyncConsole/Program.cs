using BeatSyncConsole.Configs;
using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.History;
using BeatSyncLib.Playlists;
using Newtonsoft.Json;
using SongFeedReaders.Data;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeatSaver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;

namespace BeatSyncConsole
{
    class Program
    {
        internal static readonly string ConfigPath = "config.json";
        internal static readonly string HistoryPath = "history.json";
        internal static readonly string ConfigBackupPath = "config.json.bak";

        internal static HistoryManager HistoryManager;
        internal static DownloadManager manager = new DownloadManager(3);
        internal static IJobBuilder JobBuilder;
        internal static Config Config;
        public static IJobBuilder CreateJobBuilder()
        {
            string tempDirectory = "Temp";
            string songsDirectory = "Songs";
            Directory.CreateDirectory(tempDirectory);
            Directory.CreateDirectory(songsDirectory);
            IDownloadJobFactory downloadJobFactory = new DownloadJobFactory(song =>
            {
                // return new DownloadMemoryContainer();
                return new DownloadFileContainer(Path.Combine(tempDirectory, (song.Key ?? song.Hash) + ".zip"));
            });
            ISongTargetFactorySettings targetFactorySettings = new DirectoryTargetFactorySettings() { OverwriteTarget = false };
            ISongTargetFactory targetFactory = new DirectoryTargetFactory(songsDirectory, targetFactorySettings);
            JobFinishedAsyncCallback jobFinishedCallback = new JobFinishedAsyncCallback(async c =>
            {
                HistoryEntry entry = c.CreateHistoryEntry();
                // Add entry to history.
                HistoryManager.TryAdd(c.Song.Hash, entry);
                if (c.Successful)
                {
                    // Add song to playlist.
                    Console.WriteLine($"Job completed successfully: {c.Song}");
                }
                else
                {
                    Console.WriteLine($"Job failed: {c.Song}");
                }
            });
            return new JobBuilder()
                .SetDownloadJobFactory(downloadJobFactory)
                .AddTargetFactory(targetFactory)
                .SetDefaultJobFinishedAsyncCallback(jobFinishedCallback);

        }

        public static async Task DownloadSongsAsync(IEnumerable<ISong> songs)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            manager.Start(cts.Token);
            HashSet<IJob> runningJobs = new HashSet<IJob>();
            int completedJobs = 0;
            foreach (ISong songToAdd in songs)
            {
                IJob job = JobBuilder.CreateJob(songToAdd);
                job.JobProgressChanged += (s, p) =>
                {
                    IJob j = (IJob)s;
                    runningJobs.Add(j);

                    //if (stageUpdates > 4)
                    //    cts.Cancel();
                    if (p.JobProgressType == JobProgressType.Finished)
                    {
                        int finished = ++completedJobs;
                        Console.WriteLine($"({finished} finished) Completed {j}: {p}");
                    }
                    else
                        Console.WriteLine($"({runningJobs.Count} jobs seen) Progress on {j}: {p}");
                };
                if (!manager.TryPostJob(job, out IJob j))
                {
                    Console.WriteLine($"Couldn't post duplicate: {j}");
                }
            }

            try
            {
                await manager.CompleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        static async Task InitializeConfigAsync()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    Config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync(ConfigPath).ConfigureAwait(false));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid config.json file, using defaults.");
            }
            if (Config == null)
            {
                Config = new Config();
            }
            Config.FillDefaults();
            if (Config.ConfigChanged)
            {
                try
                {
                    if (File.Exists(ConfigPath))
                        File.Copy(ConfigPath, ConfigBackupPath);
                    await File.WriteAllTextAsync(ConfigPath, JsonConvert.SerializeObject(Config, Formatting.Indented)).ConfigureAwait(false);
                    if (File.Exists(ConfigBackupPath))
                        File.Delete(ConfigBackupPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error updating config file.");
                    Console.WriteLine(ex);
                }
            }
        }

        static async Task GetBeatSaverAsync()
        {
            var config = Config.BeatSyncConfig.BeatSaver;
            var reader = new SongFeedReaders.Readers.BeatSaver.BeatSaverReader(config.MaxConcurrentPageChecks);
            List<IPlaylist> playlists = new List<IPlaylist>();
            if (config.FavoriteMappers.Enabled)
            {
                var favoriteMappersSettings = (BeatSaverFeedSettings)config.FavoriteMappers.ToFeedSettings();
                //ProcessResults(await reader.GetSongsFromFeedAsync(config.FavoriteMappers.ToFeedSettings()).ConfigureAwait(false));
            }
            if (config.Hot.Enabled)
            {
                playlists.Clear();
                if (Config.BeatSyncConfig.AllBeatSyncSongsPlaylist)
                    playlists.Add(PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncAll));
                if (config.Hot.CreatePlaylist)
                    playlists.Add(PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSaverHot));
                ProcessResults(await reader.GetSongsFromFeedAsync(config.Hot.ToFeedSettings()).ConfigureAwait(false), playlists);
            }
            if (config.Downloads.Enabled)
            {
                playlists.Clear();
                if (Config.BeatSyncConfig.AllBeatSyncSongsPlaylist)
                    playlists.Add(PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncAll));
                if (config.Downloads.CreatePlaylist)
                    playlists.Add(PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSaverDownloads));
                ProcessResults(await reader.GetSongsFromFeedAsync(config.Downloads.ToFeedSettings()).ConfigureAwait(false), playlists);
            }
        }

        public static void ProcessResults(FeedResult feedResult, IEnumerable<IPlaylist> playlists)
        {
            if (!feedResult.Successful)
                return;
            if(feedResult.Songs.Count == 0)
            {
                Console.WriteLine("No songs");
                return;
            }
            if (playlists == null)
                playlists = Array.Empty<IPlaylist>();
            void JobFinishedCallback(object sender, JobResult jobResult)
            {
                if (jobResult.Successful)
                {
                    var song = jobResult.Song;
                    Console.WriteLine($"Downloaded {song.ToString()} successfully.");
                    foreach (var playlist in playlists)
                    {
                        playlist.TryAdd(song.Hash, song.Name, song.Key, song.LevelAuthorName);
                    }
                }
                else
                    Console.WriteLine($"Failed to download {jobResult.Song.ToString()}.");
            }
            foreach (var song in feedResult.Songs.Values)
            {
                if(HistoryManager.TryGetValue(song.Hash, out HistoryEntry existing))
                {
                    if (!existing.AllowRetry)
                    {
                        Console.WriteLine($"Skipping song: {song}");
                        if(existing.Flag == HistoryFlag.Downloaded)
                        {
                            foreach (var playlist in playlists)
                            {
                                playlist.TryAdd(song.Hash, song.Name, song.Key, song.LevelAuthorName);
                            }
                        }
                        continue;
                    }
                }
                var newJob = JobBuilder.CreateJob(song);
                manager.TryPostJob(newJob, out IJob postedJob);
                postedJob.JobFinished += JobFinishedCallback; // TODO: Race condition here, might run callback twice.
                if(postedJob.JobState == JobState.Finished)
                {
                    postedJob.JobFinished -= JobFinishedCallback;
                    JobFinishedCallback(postedJob, postedJob.Result);
                }
            }
        }

        static async Task Main(string[] args)
        {
            await InitializeConfigAsync().ConfigureAwait(false);
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
            manager = new DownloadManager(Config.BeatSyncConfig.MaxConcurrentDownloads);
            HistoryManager = new HistoryManager(HistoryPath);
            HistoryManager.Initialize();
            manager.Start(CancellationToken.None);
            JobBuilder = CreateJobBuilder();
            await GetBeatSaverAsync().ConfigureAwait(false);

            await manager.CompleteAsync().ConfigureAwait(false);
            PlaylistManager.WriteAllPlaylists();
            HistoryManager.WriteToFile();
            Console.WriteLine("Press any key to continue...");
            Console.Read();
        }
    }
}
