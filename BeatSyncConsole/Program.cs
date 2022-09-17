using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Blist;
using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib.Types;
using BeatSyncConsole.Configs;
using BeatSyncConsole.Loggers;
using SongFeedReaders.Logging;
using BeatSyncConsole.Utilities;
using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Hashing;
using BeatSyncLib.History;
using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
using CommandLine;
using SongFeedReaders.Services;
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
using WebUtilities;
using BeatSyncConsole.CommandParser;

namespace BeatSyncConsole
{
    class Program
    {
        public static ILogger? Logger { get; set; }
        private static ConfigManager? ConfigManager;
        private static string? createdTempDir;

        public static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalMinutes >= 1)
            {
                return string.Format("{0}m {1}s", (int)ts.TotalMinutes, ts.Seconds);
            }
            else
                return string.Format("{0}.{1:D2}s", ts.Seconds, (int)Math.Round(ts.Milliseconds / 10d));
        }
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            JobFinishedAsyncCallback jobFinishedCallback = new JobFinishedAsyncCallback(async (JobResult c) =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                HistoryEntry entry = c.CreateHistoryEntry();
                foreach (BeatmapTarget target in jobBuilder.SongTargets)
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


        static async Task Main(string[] args)
        {
            Config? config = null;
            bool breakEarly = false;
            try
            {
                Startup startup = new Startup();
                await startup.Run(args);

                // ----------
                bool validConfig = false;

                if (!breakEarly && validConfig && config != null && config.BeatSyncConfig != null)
                {
                   
                    var andruzzProvider = SongFeedReaders.WebUtils.SongInfoManager.AddProvider<AndruzzScrapedInfoProvider>("AndruzzScrapedInfo", 50);
                    andruzzProvider.FilePath = "songDetails";
                    andruzzProvider.CacheToDisk = true;
                    await andruzzProvider.GetSongByKeyAsync("b");
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
                    foreach (BeatmapTarget? target in jobBuilder.SongTargets)
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
                                Logger?.Error($"Error storing playlists: {ex.Message}");
                                foreach (var e in ex.InnerExceptions)
                                {
                                    Logger?.Debug(e);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger?.Error($"Error storing playlists: {ex.Message}");
                                Logger?.Debug(ex);
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
                                Logger.Info($"Unable to save history at '{targetWithHistory.HistoryManager?.HistoryPath}': {ex.Message}");
                            }
                        }
                    }
                    sw.Stop();
                    Logger?.Info($"Finished after {sw.Elapsed.TotalSeconds}s: {beatSyncStats}");
                    config.BeatSyncConfig.LastRun = DateTime.Now;
                    await ConfigManager.StoreBeatSyncConfig().ConfigureAwait(false);
                }
                else
                {
                    if (!breakEarly)
                        Logger.Info("BeatSyncConsole cannot run without a valid config, exiting.");
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
                if (LogManager.IsAlive && LogManager.HasWriters && Logger != null)
                {
                    Logger.Error(message);
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
                    if (LogManager.IsAlive && LogManager.HasWriters && Logger != null)
                    {

                        Logger.Error(message);
                        Logger.Debug(ex);
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
