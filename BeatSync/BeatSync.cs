using BeatSyncLib.Configs;
using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Downloading;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Hashing;
using BeatSyncLib.History;
using BeatSyncLib.Playlists;
using BeatSaberPlaylistsLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WebUtilities.DownloadContainers;
using static BeatSync.Utilities.Util;
using System.Diagnostics;
#nullable enable
namespace BeatSync
{
    public class BeatSync : MonoBehaviour
    {
        private static readonly string GamePath = IPA.Utilities.UnityGame.InstallPath;
        private static readonly string ConfigPath = IPA.Utilities.UnityGame.UserDataPath;
        private static readonly string CustomLevelsDirectory = Path.Combine(GamePath, "Beat Saber_Data", "CustomLevels");
        private static readonly string HistoryPath = Path.Combine(ConfigPath, "BeatSyncHistory.json");
        private static readonly string PlaylistDirectory = Path.Combine(GamePath, "Playlists");
        private HistoryManager? historyManager = null;
        private SongHasher? songHasher = null;
        private PlaylistManager? playlistManager = null;
        public static BeatSync? Instance { get; set; }
        private static bool _paused;
        public static bool IsRunning { get; private set; }
        public static bool Paused
        {
            get
            {
                return _paused;
            }
            set
            {
                if (_paused == value)
                {
                    return;
                }

                _paused = value;
                if (_paused)
                {
                    SongFeedReaders.Utilities.Pause();
                    if (IsRunning)
                        Plugin.log?.Info("Pausing BeatSync.");
                }
                else
                {
                    SongFeedReaders.Utilities.UnPause();
                    if (IsRunning)
                        Plugin.log?.Info("Resuming BeatSync.");
                }
            }
        }

        private static readonly WaitUntil WaitForUnPause = new WaitUntil(() => !Paused);
        private bool _destroying;
        public CancellationToken CancelAllToken { get; set; }

        private void SetupComponents()
        {
            string historyPath = HistoryPath;
            string playlistDirectory = PlaylistDirectory;
            try
            {
                Directory.CreateDirectory(ConfigPath);
                historyManager = new HistoryManager(historyPath);
                historyManager.Initialize();
            }
            catch (Exception ex)
            {
                Plugin.log.Info($"Unable to initialize HistoryManager at '{historyPath}': {ex.Message}");
            }
            try
            {
                Directory.CreateDirectory(playlistDirectory);
                playlistManager = PlaylistManager.DefaultManager;
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Unable to create playlist directory: {ex.Message}");
                Plugin.log.Debug(ex);
            }
            Directory.CreateDirectory(CustomLevelsDirectory);
            songHasher = new SongHasher<SongHashData>(CustomLevelsDirectory);
        }

        public IJobBuilder CreateJobBuilder(BeatSyncConfig config)
        {
            //string tempDirectory = "Temp";
            //Directory.CreateDirectory(tempDirectory);
            IDownloadJobFactory downloadJobFactory = new DownloadJobFactory(song =>
            {
                return new MemoryDownloadContainer();
                //return new FileDownloadContainer(Path.Combine(tempDirectory, (song.Key ?? song.Hash) + ".zip"));
            });
            IJobBuilder jobBuilder = new JobBuilder().SetDownloadJobFactory(downloadJobFactory);

            bool overwriteTarget = false;
            Plugin.log.Info($"Adding target for '{CustomLevelsDirectory}'");
            SongTarget songTarget = new DirectoryTarget(CustomLevelsDirectory, overwriteTarget, songHasher, historyManager, playlistManager);
            //SongTarget songTarget = new MockSongTarget();
            jobBuilder.AddTarget(songTarget);

            JobFinishedAsyncCallback jobFinishedCallback = new JobFinishedAsyncCallback(async (JobResult c) =>
            {
                HistoryEntry entry = c.CreateHistoryEntry();
                if (c.TargetResults != null && c.TargetResults.Length > 0)
                {
                    foreach (TargetResult targetResult in c.TargetResults)
                    {
                        if (targetResult == null)
                        {
                            Plugin.log?.Warn($"TargetResult is null.");
                            continue;
                        }
                        else if (targetResult.Success)
                        {
                            Plugin.log?.Info($"Target transfer successful for {targetResult.Target.DestinationId}|{targetResult.SongState}");
                        }
                        else
                        {
                            Plugin.log?.Warn($"Target transfer unsuccessful for {targetResult.Target.DestinationId}");
                            if (targetResult.Exception != null)
                                Plugin.log?.Debug(targetResult.Exception);
                        }
                        // Add entry to history, this should only succeed for jobs that didn't get to the targets.
                        if (targetResult.Target is ITargetWithHistory targetWithHistory && targetWithHistory.HistoryManager != null)
                            targetWithHistory.HistoryManager.TryAdd(c.Song.Hash, entry);
                        Plugin.log?.Info($"Song {c.Song} transferred to {targetResult.Target.DestinationId}.");
                    }
                }
                else
                {
                    Plugin.log?.Warn($"{c.Song} has no target results.");
                }
                if (c.Successful)
                {
                    if (c.DownloadResult != null && c.DownloadResult.Status == DownloadResultStatus.Skipped)
                    {
                        Plugin.log?.Info($"      Job skipped: {c.Song} not wanted by any targets.");
                    }
                    else
                    {
                        Plugin.log?.Info($"      Job completed successfully: {c.Song}");
                    }
                }
                else
                {
                    Plugin.log?.Info($"      Job failed: {c.Song}");
                }
            });
            jobBuilder.SetDefaultJobFinishedAsyncCallback(jobFinishedCallback);
            return jobBuilder;

        }



        public static IEnumerator<WaitUntil?> UpdateLevelPacks()
        {
            yield return WaitForUnPause;
            //BeatSaverDownloader.Misc.PlaylistsCollection.ReloadPlaylists(true);
            if (!SongCore.Loader.AreSongsLoaded)
            {
                yield break;
            }
            if (SongCore.Loader.AreSongsLoading)
            {
                while (SongCore.Loader.AreSongsLoading)
                    yield return null;
            }
            SongCore.Loader.Instance?.RefreshLevelPacks();
            SongCore.Loader.Instance?.RefreshSongs(true);
        }

        public IEnumerator<WaitUntil> DestroyAfterFinishing()
        {
            _destroying = true;
            Instance = null;
            Plugin.log?.Debug($"Waiting for BeatSyncController to finish.");
            yield return new WaitUntil(() => IsRunning == false);
            Plugin.log?.Debug($"Destroying BeatSyncController");
            GameObject.Destroy(this);
        }

        #region MonoBehaviour

        public void Awake()
        {
            //Plugin.log?.Debug("BeatSync Awake()");
            var previousInstance = Instance;
            if (previousInstance != null)
            {
                if (!previousInstance._destroying)
                {
                    Plugin.log?.Debug("BeatSync component already exists, destroying this one.");
                    GameObject.DestroyImmediate(this);
                }
                else
                    Plugin.log?.Warn($"Creating a new BeatSync controller before the old finished destroying itself.");
            }
            Instance = this;
            _destroying = false;
#if DEBUG
            var instances = GameObject.FindObjectsOfType<BeatSync>().ToList();
            Plugin.log?.Critical($"Number of controllers: {instances.Count}");
#endif
            //FinishedHashing += OnHashingFinished;
        }


        public async void Start()
        {
            Plugin.log?.Debug("BeatSync Start()");
            IsRunning = true;
            SetupComponents();

            if (playlistManager != null)
            {
                var recentPlaylist = Plugin.config.RecentPlaylistDays > 0 ? playlistManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncRecent) : null;
                if (recentPlaylist != null && Plugin.config.RecentPlaylistDays > 0)
                {
                    var minDate = DateTime.Now - new TimeSpan(Plugin.config.RecentPlaylistDays, 0, 0, 0);
                    int removedCount = recentPlaylist.RemoveAll(s => s.DateAdded < minDate);
                    if (removedCount > 0)
                    {
                        Plugin.log?.Info($"Removed {removedCount} old songs from the RecentPlaylist.");
                        recentPlaylist.RaisePlaylistChanged();
                        try
                        {
                            playlistManager.StorePlaylist(recentPlaylist);
                        }
                        catch (Exception ex)
                        {
                            Plugin.log?.Warn($"Unable to write {recentPlaylist.Filename}: {ex.Message}");
                            Plugin.log?.Debug(ex);
                        }
                    }
                    else
                        Plugin.log?.Info("Didn't remove any songs from RecentPlaylist.");

                }
            }
            var syncInterval = new TimeSpan(Plugin.modConfig.TimeBetweenSyncs.Hours, Plugin.modConfig.TimeBetweenSyncs.Minutes, 0);
            var nowTime = DateTime.Now;
            if (Plugin.config.LastRun + syncInterval <= nowTime)
            {
                if (Plugin.config.LastRun != DateTime.MinValue)
                    Plugin.log?.Info($"BeatSync ran {TimeSpanToString(nowTime - Plugin.config.LastRun)} ago");
                if (songHasher != null)
                {
                    await songHasher.InitializeAsync().ConfigureAwait(false);
                    Plugin.log?.Info($"Hashed {songHasher.HashDictionary.Count} songs in {CustomLevelsDirectory}.");
                }
                else
                    Plugin.log?.Error($"SongHasher was null.");
                // Start downloader
                IJobBuilder jobBuilder = CreateJobBuilder(Plugin.config);
                SongDownloader songDownloader = new SongDownloader();
                JobManager JobManager = new JobManager(Plugin.config.MaxConcurrentDownloads);
                JobManager.Start(CancelAllToken);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                if (jobBuilder.SongTargets.Count() == 0)
                    Plugin.log?.Error("jobBuilder has no SongTargets.");
                JobStats[] sourceStats = await songDownloader.RunAsync(Plugin.config, jobBuilder, JobManager).ConfigureAwait(false); // TODO: CancellationToken
                JobStats beatSyncStats = sourceStats.Aggregate((a, b) => a + b);
                await JobManager.CompleteAsync();
                int recentPlaylistDays = Plugin.config.RecentPlaylistDays;
                DateTime cutoff = DateTime.Now - new TimeSpan(recentPlaylistDays, 0, 0, 0);
                foreach (SongTarget target in jobBuilder.SongTargets)
                {
                    if (target is ITargetWithPlaylists targetWithPlaylists)
                    {
                        PlaylistManager? targetPlaylistManager = targetWithPlaylists.PlaylistManager;
                        if (recentPlaylistDays > 0)
                        {
                            BeatSaberPlaylistsLib.Types.IPlaylist? recent = targetPlaylistManager?.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncRecent);
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
                            Plugin.log?.Error($"Error storing playlists: {ex.Message}");
                            foreach (var e in ex.InnerExceptions)
                            {
                                Plugin.log?.Debug(e);
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.log?.Error($"Error storing playlists: {ex.Message}");
                            Plugin.log?.Debug(ex);
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
                            Plugin.log?.Info($"Unable to save history at '{targetWithHistory.HistoryManager?.HistoryPath}': {ex.Message}");
                        }
                    }
                }
                sw.Stop();
                Plugin.log?.Info($"Finished after {sw.Elapsed.TotalSeconds}s: {beatSyncStats}");
                Plugin.config.LastRun = DateTime.Now;
                Plugin.ConfigManager.SaveConfig();
                SongCore.Loader loader = SongCore.Loader.Instance;
                SongCore.Loader.SongsLoadedEvent -= Loader_SongsLoadedEvent;
                SongCore.Loader.SongsLoadedEvent += Loader_SongsLoadedEvent;
                if (!SongCore.Loader.AreSongsLoading)
                {
                    SongCore.Loader.SongsLoadedEvent -= Loader_SongsLoadedEvent;
                    if (SongCore.Loader.AreSongsLoaded)
                        loader.RefreshSongs();
                }
            }
            else
            {
                Plugin.log?.Info($"BeatSync ran {TimeSpanToString(nowTime - Plugin.config.LastRun)} ago, skipping because TimeBetweenSyncs is {Plugin.modConfig.TimeBetweenSyncs}");
            }
        }

        private void Loader_SongsLoadedEvent(SongCore.Loader arg1, Dictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            SongCore.Loader.SongsLoadedEvent -= Loader_SongsLoadedEvent;
            SongCore.Loader.Instance.RefreshSongs();
        }

        #endregion
    }
}

