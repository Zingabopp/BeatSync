using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Blist;
using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib.Types;
using BeatSyncConsole.Configs;
using BeatSyncConsole.Loggers;
using BeatSyncConsole.Utilities;
using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Downloading;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Hashing;
using BeatSyncLib.History;
using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities.DownloadContainers;

namespace BeatSyncConsole
{
    class Program
    {
        private const string ReleaseUrl = @"https://github.com/Zingabopp/BeatSync/releases";
        private static readonly string ConfigDirectory = Paths.GetFullPath("configs", PathRoot.AssemblyDirectory);
        internal static readonly string ConfigBackupPath = Paths.GetFullPath("config.json.bak", PathRoot.AssemblyDirectory);
        private static ConfigManager? ConfigManager;
        private static string? createdTempDir;

        public static async Task<IJobBuilder> CreateJobBuilderAsync(Config config)
        {
            string tempDirectory = Paths.TempDirectory;
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
                createdTempDir = tempDirectory;
            }
            IDownloadJobFactory downloadJobFactory = new DownloadJobFactory(song =>
            {
                // return new DownloadMemoryContainer();
                return new FileDownloadContainer(Path.Combine(tempDirectory, (song.Key ?? song.Hash) + ".zip"));
            });
            IJobBuilder jobBuilder = new JobBuilder().SetDownloadJobFactory(downloadJobFactory);
            List<ISongLocation> songLocations = new List<ISongLocation>();
            songLocations.AddRange(config.BeatSaberInstallLocations.Where(l => l.Enabled && l.IsValid()));
            songLocations.AddRange(config.AlternateSongsPaths.Where(l => l.Enabled && l.IsValid()));

            foreach (ISongLocation location in songLocations)
            {
                bool overwriteTarget = false;
                bool unzipBeatmaps = true;
                if(location is CustomSongLocation customLocation)
                {
                    unzipBeatmaps = customLocation.UnzipBeatmaps;
                }
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
                    playlistManager = new PlaylistManager(playlistDirectory, new LegacyPlaylistHandler(), new BlistPlaylistHandler());
                }
                string songsDirectory = location.SongsDirectory;
                if (!Path.IsPathFullyQualified(songsDirectory))
                    songsDirectory = Path.Combine(location.BasePath, songsDirectory);
                Directory.CreateDirectory(songsDirectory);
                songHasher = new SongHasher<SongHashData>(songsDirectory);
                Stopwatch sw = new Stopwatch();
                Logger.log.Info($"Hashing songs in '{Paths.ReplaceWorkingDirectory(songsDirectory)}'...");
                sw.Start();
                await songHasher.InitializeAsync().ConfigureAwait(false);
                sw.Stop();
                Logger.log.Info($"Hashed {songHasher.HashDictionary.Count} songs in {Paths.ReplaceWorkingDirectory(songsDirectory)} in {sw.Elapsed.Seconds}sec.");
                SongTarget songTarget = new DirectoryTarget(songsDirectory, overwriteTarget, unzipBeatmaps, songHasher, historyManager, playlistManager);
                //SongTarget songTarget = new MockSongTarget();
                jobBuilder.AddTarget(songTarget);
            }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            JobFinishedAsyncCallback jobFinishedCallback = new JobFinishedAsyncCallback(async (JobResult c) =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                HistoryEntry entry = c.CreateHistoryEntry();
                foreach (SongTarget target in jobBuilder.SongTargets)
                {
                    // Add entry to history, this should only succeed for jobs that didn't get to the targets.
                    if (target is ITargetWithHistory targetWithHistory
                        && targetWithHistory.HistoryManager != null)
                    {
                        string? songHash = c.Song?.Hash;
                        if (songHash != null && songHash.Length > 0)
                        {
                            targetWithHistory.HistoryManager.AddOrUpdate(songHash, entry);
                        }
                    }
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
        private static ConsoleLogWriter SetupLogging()
        {
            ConsoleLogWriter consoleWriter = new ConsoleLogWriter
            {
                LogLevel = BeatSyncLib.Logging.LogLevel.Info
            };
            LogManager.AddLogWriter(consoleWriter);
            BeatSyncLib.Logger.log = new BeatSyncLogger("BeatSyncLib");
            SongFeedReaderLogger feedReaderLogger = new SongFeedReaderLogger("SongFeedReader");
            SongFeedReaders.Logging.LoggingController.DefaultLogger = feedReaderLogger;
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
            return consoleWriter;
        }
        static async Task CheckVersion()
        {
            try
            {
                VersionChecker versionChecker = new VersionChecker
                {
                    ReleaseFilter = VersionChecker.HasAsset("BeatSyncConsole")
                };
                GithubVersion latest = await versionChecker.GetLatestVersionAsync("Zingabopp", "BeatSync").ConfigureAwait(false);
                if (!latest.IsValid)
                {
                    Logger.log.Warn($"Unable to get information on the latest version.");
                    return;
                }
                Version? current = Assembly.GetExecutingAssembly().GetName().Version;
                if (current != null)
                {
                    int compare = latest.CompareTo(current);
                    if (compare > 0)
                        Logger.log.Warn($"There is a new version of BeatSyncConsole available: ({latest}). Download the latest release from '{ReleaseUrl}'.");
                    else if (compare < 0)
                        Logger.log.Info($"Running a build of BeatSyncConsole from the future! Current released version is {latest}.");
                    else
                        Logger.log.Info($"Running the latest release of BeatSyncConsole.");
                }
            }
            catch (Exception ex)
            {
                Logger.log.Warn($"Error checking for latest version: {ex.Message}.");
                Logger.log.Debug(ex);
            }

        }

        static async Task Main(string[] args)
        {
            Config? config = null;
            try
            {
                ConsoleLogWriter? consoleLogger = SetupLogging();
                string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
                Logger.log.Info($"Starting BeatSyncConsole v{version} |{VersionInfo.Description}");
                await CheckVersion().ConfigureAwait(false);
                ConfigManager = new ConfigManager(ConfigDirectory);
                bool validConfig = await ConfigManager.InitializeConfigAsync().ConfigureAwait(false);
                if (consoleLogger != null)
                    consoleLogger.LogLevel = ConfigManager.Config?.ConsoleLogLevel ?? BeatSyncLib.Logging.LogLevel.Info;
                config = ConfigManager.Config;
                if (validConfig && config != null && config.BeatSyncConfig != null)
                {
                    SongFeedReaders.WebUtils.Initialize(new WebUtilities.HttpClientWrapper.HttpClientWrapper());
                    SongFeedReaders.WebUtils.WebClient.SetUserAgent($"BeatSyncConsole/{version} ({RuntimeInformation.OSDescription}){VersionInfo.Description}");
                    JobManager manager = new JobManager(config.BeatSyncConfig.MaxConcurrentDownloads);
                    manager.Start(CancellationToken.None);
                    IJobBuilder jobBuilder = await CreateJobBuilderAsync(config).ConfigureAwait(false);
                    SongDownloader songDownloader = new SongDownloader();
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    JobStats[] sourceStats = await songDownloader.RunAsync(config.BeatSyncConfig, jobBuilder, manager).ConfigureAwait(false);
                    JobStats beatSyncStats = sourceStats.Aggregate((a, b) => a + b);
                    await manager.CompleteAsync().ConfigureAwait(false);
                    int recentPlaylistDays = config.BeatSyncConfig.RecentPlaylistDays;
                    DateTime cutoff = DateTime.Now - new TimeSpan(recentPlaylistDays, 0, 0, 0);
                    foreach (SongTarget? target in jobBuilder.SongTargets)
                    {
                        if (target is ITargetWithPlaylists targetWithPlaylists)
                        {
                            PlaylistManager? targetPlaylistManager = targetWithPlaylists.PlaylistManager;
                            if (recentPlaylistDays > 0)
                            {
                                IPlaylist? recent = targetPlaylistManager?.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncRecent);
                                if (recent != null && recent.Count > 0)
                                {
                                    int songsRemoved = recent.RemoveAll(s => s.DateAdded < cutoff);
                                    if (songsRemoved > 0)
                                        recent.RaisePlaylistChanged();
                                }
                            }
                            try
                            {
                                targetPlaylistManager?.StoreAllPlaylists();
                            }
                            catch (AggregateException ex)
                            {
                                Logger.log.Error($"Error storing playlists: {ex.Message}");
                                foreach (var e in ex.InnerExceptions)
                                {
                                    Logger.log.Debug(e);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.log.Error($"Error storing playlists: {ex.Message}");
                                Logger.log.Debug(ex);
                            }
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
                    config.BeatSyncConfig.LastRun = DateTime.Now;
                    await ConfigManager.StoreBeatSyncConfig().ConfigureAwait(false);
                }
                else
                {
                    Logger.log.Info("BeatSyncConsole cannot run without a valid config, exiting.");
                }
                Cleanup();
                LogManager.Stop();
                LogManager.Wait();
                if (!(config?.CloseWhenFinished ?? false))
                {
                    Console.WriteLine("Press Enter to continue...");
                    Console.Read();
                }
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
                Cleanup();
                LogManager.Stop();
                LogManager.Wait();
                if (!(config?.CloseWhenFinished ?? false))
                {
                    Console.WriteLine("Press Enter to continue...");
                    Console.Read();
                }
            }
            finally
            {
                LogManager.Abort();
            }
        }

        public static void Cleanup()
        {
            if (createdTempDir != null && Directory.Exists(createdTempDir))
            {
                try
                {
                    Directory.Delete(createdTempDir, true);
                }
                catch (Exception ex)
                {
                    string message = $"Error deleting temp directory '{createdTempDir}': {ex.Message}";
                    if (LogManager.IsAlive && LogManager.HasWriters && Logger.log != null)
                    {

                        Logger.log?.Error(message);
                        Logger.log?.Debug(ex);
                    }
                    else
                    {
                        ConsoleColor previousColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(message);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(ex);
                        Console.ForegroundColor = previousColor;
                    }
                }
            }
        }
    }
}
