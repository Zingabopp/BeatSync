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
                    SongDownloader songDownloader = new SongDownloader();
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    JobStats[] sourceStats = await songDownloader.RunAsync(config, jobBuilder, manager).ConfigureAwait(false);
                    JobStats beatSyncStats = sourceStats.Aggregate((a, b) => a + b);
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
