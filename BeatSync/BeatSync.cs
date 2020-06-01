using BeatSyncLib.Configs;
using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Downloading;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Hashing;
using BeatSyncLib.History;
using BeatSyncPlaylists;
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
                        Logger.log?.Info("Pausing BeatSync.");
                }
                else
                {
                    SongFeedReaders.Utilities.UnPause();
                    if (IsRunning)
                        Logger.log?.Info("Resuming BeatSync.");
                }
            }
        }

        private static WaitUntil WaitForUnPause = new WaitUntil(() => !Paused);
        private bool _destroying;
        public CancellationToken CancelAllToken { get; set; }

        private void SetupComponents()
        {
            string historyPath = Path.Combine(ConfigPath, "BeatSyncHistory.json");
            string playlistDirectory = Path.Combine(GamePath, "Playlists");
            try
            {
                Directory.CreateDirectory(ConfigPath);
                historyManager = new HistoryManager(historyPath);
                historyManager.Initialize();
            }
            catch (Exception ex)
            {
                Logger.log.Info($"Unable to initialize HistoryManager at '{historyPath}': {ex.Message}");
            }
            try
            {
                Directory.CreateDirectory(playlistDirectory);
                playlistManager = new PlaylistManager(playlistDirectory);
            }
            catch (Exception ex)
            {
                Logger.log.Error($"Unable to create playlist directory: {ex.Message}");
                Logger.log.Debug(ex);
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

            SongTarget songTarget = new DirectoryTarget(CustomLevelsDirectory, overwriteTarget, songHasher, historyManager, playlistManager);
            //SongTarget songTarget = new MockSongTarget();
            jobBuilder.AddTarget(songTarget);

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
            Logger.log?.Debug($"Waiting for BeatSyncController to finish.");
            yield return new WaitUntil(() => IsRunning == false);
            Logger.log?.Debug($"Destroying BeatSyncController");
            GameObject.Destroy(this);
        }

        #region MonoBehaviour

        public void Awake()
        {
            //Logger.log?.Debug("BeatSync Awake()");
            var previousInstance = Instance;
            if (previousInstance != null)
            {
                if (!previousInstance._destroying)
                {
                    Logger.log?.Debug("BeatSync component already exists, destroying this one.");
                    GameObject.DestroyImmediate(this);
                }
                else
                    Logger.log?.Warn($"Creating a new BeatSync controller before the old finished destroying itself.");
            }
            Instance = this;
            _destroying = false;
#if DEBUG
            var instances = GameObject.FindObjectsOfType<BeatSync>().ToList();
            Logger.log?.Critical($"Number of controllers: {instances.Count}");
#endif
            //FinishedHashing += OnHashingFinished;
        }


        public async void Start()
        {
            Logger.log?.Debug("BeatSync Start()");
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
                        Logger.log?.Info($"Removed {removedCount} old songs from the RecentPlaylist.");
                        recentPlaylist.RaisePlaylistChanged();
                        try
                        {
                            playlistManager.StorePlaylist(recentPlaylist);
                        }
                        catch (Exception ex)
                        {
                            Logger.log?.Warn($"Unable to write {recentPlaylist.Filename}: {ex.Message}");
                            Logger.log?.Debug(ex);
                        }
                    }
                    else
                        Logger.log?.Info("Didn't remove any songs from RecentPlaylist.");

                }
            }
            var syncInterval = new TimeSpan(Plugin.config.TimeBetweenSyncs.Hours, Plugin.config.TimeBetweenSyncs.Minutes, 0);
            var nowTime = DateTime.Now;
            if (Plugin.config.LastRun + syncInterval <= nowTime)
            {
                if (Plugin.config.LastRun != DateTime.MinValue)
                    Logger.log?.Info($"BeatSync ran {TimeSpanToString(nowTime - Plugin.config.LastRun)} ago");
                if (songHasher != null)
                {
                    await songHasher.InitializeAsync().ConfigureAwait(false);
                    Logger.log?.Info($"Hashed {songHasher.HashDictionary.Count} songs in {CustomLevelsDirectory}.");
                }
                else
                    Logger.log?.Error($"SongHasher was null.");
                // Start downloader
                IJobBuilder jobBuilder = CreateJobBuilder(Plugin.config);
                SongDownloader songDownloader = new SongDownloader();
                JobManager JobManager = new JobManager(Plugin.config.MaxConcurrentDownloads);
                JobManager.Start(CancelAllToken);
                await songDownloader.RunAsync(Plugin.config, jobBuilder, JobManager); // TODO: CancellationToken
                // If successful, update Plugin.config.LastRun
            }
            else
            {
                Logger.log?.Info($"BeatSync ran {TimeSpanToString(nowTime - Plugin.config.LastRun)} ago, skipping because TimeBetweenSyncs is {Plugin.config.TimeBetweenSyncs}");
            }
        }

        #endregion
    }
}

