using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SongFeedReaders;
using BeatSync.Configs;
using Newtonsoft.Json;
using SongCore.Data;
using System.Collections.Concurrent;
using System.IO;
using System.Diagnostics;
using BeatSync.Playlists;
using BeatSync.Utilities;
using SongFeedReaders.DataflowAlternative;
using System.IO.Compression;

namespace BeatSync
{
    public class BeatSync : MonoBehaviour
    {
        public static BeatSync Instance { get; set; }
        public static bool PauseWork { get; set; }
        private const string BeatSaverDownloadUrlBase = "https://beatsaver.com/api/download/hash/";
        private ConcurrentDictionary<string, SongHashData> HashDictionary;
        private ConcurrentDictionary<string, string> ExistingSongs;
        private ConcurrentQueue<PlaylistSong> DownloadQueue;
        private static readonly string SongTempPath = Path.GetFullPath(Path.Combine("UserData", "BeatSyncTemp"));
        private TransformBlock<PlaylistSong, PlaylistSong> DownloadBatch;

        public void Awake()
        {
            if (Instance != null)
                GameObject.DestroyImmediate(this);
            Instance = this;
            Logger.log.Warn("BeatSync Awake");
            HashDictionary = new ConcurrentDictionary<string, SongHashData>();
            ExistingSongs = new ConcurrentDictionary<string, string>();
            DownloadQueue = new ConcurrentQueue<PlaylistSong>();

            FinishedHashing += OnHashingFinished;


        }

        private async Task<PlaylistSong> DownloadJob(PlaylistSong song)
        {

            bool directoryCreated = false;
            string tempFile = null;
            bool overwrite = true;
            string extractDirectory = null;
            try
            {
                var songDirPath = Path.GetFullPath(Path.Combine(CustomLevelPathHelper.customLevelsDirectoryPath, song.DirectoryName));
                directoryCreated = !Directory.Exists(songDirPath);
                // Won't remove if it fails, why bother with the HashDictionary TryAdd check if we're overwriting, incrementing folder name
                if (HashDictionary.TryAdd(songDirPath, new SongHashData(0, song.Hash)))
                {
                    var downloadUri = new Uri(BeatSaverDownloadUrlBase + song.Hash.ToLower());
                    var downloadTarget = Path.Combine(SongTempPath, song.Key);
                    tempFile = await FileIO.DownloadFileAsync(downloadUri, downloadTarget, true).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(tempFile))
                    {
                        extractDirectory = Path.GetFullPath(await FileIO.ExtractZipAsync(tempFile, songDirPath, song.Key, true, overwrite));
                        if (!overwrite && !songDirPath.Equals(extractDirectory))
                        {
                            Logger.log.Debug($"songDirPath {songDirPath} != {extractDirectory}, updating dictionary.");
                            directoryCreated = true;
                            ExistingSongs[song.Hash] = extractDirectory;
                        }
                    }

                }

            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (File.Exists(tempFile))
                    await FileIO.TryDeleteAsync(tempFile).ConfigureAwait(false);
            }


            return song;
        }

        public void Start()
        {
            Logger.log.Debug("BeatSync Start()");
            LoadCachedSongHashesAsync(Plugin.CachedHashDataPath);
            Logger.log.Critical($"Read {HashDictionary.Count} cached songs.");
            var hashTask = Task.Run(() => AddMissingHashes());
            //Logger.log.Info("Converting legacy playlists.");
            //PlaylistManager.ConvertLegacyPlaylists();
            FavoriteMappers.Initialize();
        }

        public void OnHashingFinished()
        {
            Logger.log.Info("Hashing finished.");
            Logger.log.Critical($"HashDictionary has {HashDictionary.Count} songs.");
            StartCoroutine(ScrapeSongsCoroutine());
        }

        public IEnumerator<WaitUntil> ScrapeSongsCoroutine()
        {
            Logger.log.Debug("Starting ScrapeSongsCoroutine");
            var readTask = RunReaders();
            var readWait = new WaitUntil(() => readTask.IsCompleted);
            yield return readWait;
            DownloadBatch = new TransformBlock<PlaylistSong, PlaylistSong>(DownloadJob, new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = DownloadQueue.Count + 100,
                MaxDegreeOfParallelism = Plugin.config.Value.MaxConcurrentDownloads,
                EnsureOrdered = false
            });
            Logger.log.Info($"Starting downloader.");
            var downloadTask = Task.Run(async () =>
            {
                var downloadedSongs = new List<string>();
                while (DownloadQueue.TryDequeue(out var song))
                {
                    if (DownloadBatch.TryReceiveAll(out var songsCompleted))
                    {
                        downloadedSongs.AddRange(songsCompleted.Select(s => s.Hash));
                    }
                    await DownloadBatch.SendAsync(song).ConfigureAwait(false);
                }
                DownloadBatch.Complete();
                await DownloadBatch.Completion().ConfigureAwait(false);

                if (DownloadBatch.TryReceiveAll(out var songs))
                {
                    downloadedSongs.AddRange(songs.Select(s => s.Hash));
                }
                return downloadedSongs;
            });
            var downloadWait = new WaitUntil(() => downloadTask.IsCompleted);
            yield return downloadWait;
            //TestPrintReaderResults(beatSaverTask, bsaberTask, scoreSaberTask);

        }

        public void PrintSongs(string source, IEnumerable<ScrapedSong> songs)
        {
            foreach (var song in songs)
            {
                Logger.log.Warn($"{source}: {song.SongName} by {song.MapperName}, hash: {song.Hash}");
            }
        }


        public void LoadCachedSongHashesAsync(string cachedHashPath)
        {
            if (!File.Exists(cachedHashPath))
            {
                Logger.log.Warn($"Couldn't find cached songs at {cachedHashPath}");
                return;
            }
            try
            {
                using (var fs = File.OpenText(cachedHashPath))
                using (var js = new JsonTextReader(fs))
                {
                    var ser = new JsonSerializer();
                    var songHashes = ser.Deserialize<Dictionary<string, SongHashData>>(js);
                    foreach (var songHash in songHashes)
                    {
                        var success = HashDictionary.TryAdd(songHash.Key, songHash.Value);
                        ExistingSongs.TryAdd(songHash.Value.songHash, songHash.Key);
                        if (!success)
                            Logger.log.Warn($"Couldn't add {songHash.Key} to the HashDictionary");
                    }
                    Logger.log.Info($"Added {HashDictionary.Count} song hashes from SongCore's cache.");
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error("Exception in LoadCachedSongHashesAsync");
                Logger.log.Error(ex);
            }
            Logger.log.Debug("Finished adding cached song hashes to the dictionary");
        }

        private void AddMissingHashes()
        {
            Logger.log.Info("Starting AddMissingHashes");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var songDir = new DirectoryInfo(Plugin.CustomLevelsPath);
            Logger.log.Info($"SongDir is {songDir.FullName}");
            songDir.GetDirectories().Where(d => !HashDictionary.ContainsKey(d.FullName)).ToList().AsParallel().ForAll(d =>
            {
                var data = GetSongHashData(d.FullName).Result;
                if (data == null)
                {
                    Logger.log.Warn($"GetSongHashData({d.FullName}) returned null");
                    return;
                }
                else if (string.IsNullOrEmpty(data.songHash))
                {
                    Logger.log.Warn($"GetSongHashData({d.FullName}) returned a null string for hash.");
                    return;
                }
                //if (HashDictionary[d.FullName].songHash != data.songHash)
                //    Logger.log.Warn($"Hash doesn't match for {d.Name}");
                try
                {
                    ExistingSongs.TryAdd(data.songHash, d.FullName);
                }
                catch (Exception ex)
                {
                    Logger.log.Error($"Exception in AddMissingHashes.\n{ex.Message}\n{ex.StackTrace}");
                }
                if (!HashDictionary.TryAdd(d.FullName, data))
                {
                    Logger.log.Warn($"Couldn't add {d.FullName} to HashDictionary");
                }
                else
                {
                    //Logger.log.Info($"Added {d.Name} to the HashDictionary.");
                }
            });
            sw.Stop();
            Logger.log.Warn($"Finished hashing in {sw.ElapsedMilliseconds}ms, Triggering FinishedHashing");
            FinishedHashing?.Invoke();
        }

        private async Task<SongHashData> GetSongHashData(string songDirectory)
        {

            var directoryHash = await Task.Run(() => Util.GenerateDirectoryHash(songDirectory)).ConfigureAwait(false);
            string hash = await Task.Run(() => Util.GenerateHash(songDirectory)).ConfigureAwait(false);

            return new SongHashData(directoryHash, hash);
        }

        public event Action FinishedHashing;

        private async Task RunReaders()
        {
            List<Task<Dictionary<string, ScrapedSong>>> readerTasks = new List<Task<Dictionary<string, ScrapedSong>>>();
            var config = Plugin.config.Value;
            var beatSyncPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncAll);
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
                Logger.log.Error($"Error in reading feeds.\n{ex.Message}\n{ex.StackTrace}");
            }

            Logger.log.Info($"Finished reading feeds.");
            var songsToDownload = new Dictionary<string, ScrapedSong>();
            foreach (var readTask in readerTasks)
            {
                if (!readTask.DidCompleteSuccessfully())
                {
                    Logger.log.Warn("Task not successful, skipping.");
                    continue;
                }
                Logger.log.Warn($"Queuing songs from task.");
                songsToDownload.Merge(await readTask);
            }
            Logger.log.Info($"Found {songsToDownload.Count} unique songs.");
            var allPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncAll);
            var recentPlaylist = config.RecentPlaylistDays > 0 ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncRecent) : null;
            foreach (var scrapedSong in songsToDownload.Values)
            {
                var playlistSong = new PlaylistSong(scrapedSong.Hash, scrapedSong.SongName, scrapedSong.SongKey, scrapedSong.MapperName);
                if (ExistingSongs.TryAdd(scrapedSong.Hash, ""))
                {
                    Logger.log.Info($"Queuing {scrapedSong.SongKey} - {scrapedSong.SongKey} by {scrapedSong.MapperName}");
                    DownloadQueue.Enqueue(playlistSong);
                }

                allPlaylist?.TryAdd(playlistSong);
                recentPlaylist?.TryAdd(playlistSong);
            }
            allPlaylist?.TryWriteFile();
            if (recentPlaylist != null && config.RecentPlaylistDays > 0)
            {
                var minDate = DateTime.Now - new TimeSpan(config.RecentPlaylistDays, 0, 0, 0);
                int removedCount = recentPlaylist.Songs.RemoveAll(s => s.DateAdded < minDate);
                Logger.log.Info($"Removed {removedCount} old songs from the RecentPlaylist.");
                recentPlaylist.TryWriteFile();
            }


        }

        private async Task<Dictionary<string, ScrapedSong>> ReadFeed(IFeedReader reader, IFeedSettings settings, Playlist feedPlaylist = null)
        {
            var feedName = reader.GetFeedName(settings);
            Logger.log.Info($"Getting songs from {feedName} feed.");
            var songs = await reader.GetSongsFromFeedAsync(settings).ConfigureAwait(false) ?? new Dictionary<string, ScrapedSong>();
            foreach (var scrapedSong in songs.Reverse()) // Reverse so the last songs have the oldest DateTime
            {
                if (string.IsNullOrEmpty(scrapedSong.Value.SongKey))
                {
                    try
                    {
                        // ScrapedSong doesn't have a Beat Saver key associated with it, probably scraped from ScoreSaber
                        scrapedSong.Value.UpdateFrom(await BeatSaverReader.GetSongByHashAsync(scrapedSong.Key), false);
                    }
                    catch (ArgumentNullException)
                    {
                        Logger.log.Warn($"Unable to find {scrapedSong.Value?.SongName} by {scrapedSong.Value?.MapperName} on Beat Saver ({scrapedSong.Key})");
                    }
                }
                var song = new PlaylistSong(scrapedSong.Value.Hash, scrapedSong.Value.SongName, scrapedSong.Value.SongKey, scrapedSong.Value.MapperName);

                feedPlaylist?.TryAdd(song);
            }
            feedPlaylist?.TryWriteFile();
            var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log.Info($"Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")} in the {feedName} feed.");
            return songs;
        }

        #region Feed Read Functions
        private async Task<Dictionary<string, ScrapedSong>> ReadBeastSaber()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log.Info("Starting BeastSaber reading");

            var config = Plugin.config.Value.BeastSaber;
            BeastSaberReader reader = null;
            try
            {
                reader = new BeastSaberReader(config.Username, config.MaxConcurrentPageChecks);
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
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
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (ArgumentException ex)
                {
                    Logger.log.Critical("Exception in BeastSaber Bookmarks: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Exception in ReadBeastSaber, Bookmarks.");
                    Logger.log.Error(ex);
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
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (ArgumentException ex)
                {
                    Logger.log.Critical("Exception in BeastSaber Follows: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Exception in ReadBeastSaber, Follows.");
                    Logger.log.Error(ex);
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
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Exception in ReadBeastSaber, Curator Recommended.");
                    Logger.log.Error(ex);
                }
            }
            sw.Stop();
            var totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log.Info($"Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} from {reader.Name} in {sw.Elapsed.ToString()}");
            return readerSongs;
        }

        private async Task<Dictionary<string, ScrapedSong>> ReadBeatSaver(Playlist allPlaylist = null)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log.Info("Starting BeatSaver reading");

            var config = Plugin.config.Value.BeatSaver;
            BeatSaverReader reader = null;
            try
            {
                reader = new BeatSaverReader();
            }
            catch (Exception ex)
            {
                Logger.log.Error("Exception creating BeatSaverReader in ReadBeatSaver.");
                Logger.log.Error(ex);
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
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log.Critical("Exception in ReadBeatSaver, FavoriteMappers: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Exception in ReadBeatSaver, FavoriteMappers.");
                    Logger.log.Error(ex);
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
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log.Critical("Exception in ReadBeatSaver, Hot: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Exception in ReadBeatSaver, Hot.");
                    Logger.log.Error(ex);
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
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (InvalidCastException ex)
                {
                    Logger.log.Error($"This should never happen in ReadBeatSaver.\n{ex.Message}");
                }
                catch (ArgumentNullException ex)
                {
                    Logger.log.Critical("Exception in ReadBeatSaver, Downloads: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Exception in ReadBeatSaver, Downloads.");
                    Logger.log.Error(ex);
                }
            }
            sw.Stop();
            var totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log.Info($"Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} from {reader.Name} in {sw.Elapsed.ToString()}");
            return readerSongs;
        }

        private async Task<Dictionary<string, ScrapedSong>> ReadScoreSaber(Playlist allPlaylist = null)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log.Info("Starting ScoreSaber reading");

            var config = Plugin.config.Value.ScoreSaber;
            ScoreSaberReader reader = null;
            try
            {
                reader = new ScoreSaberReader();
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
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
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Exception in ReadScoreSaber, Top Ranked.");
                    Logger.log.Error(ex);
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
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Exception in ReadScoreSaber, Trending.");
                    Logger.log.Error(ex);
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
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Exception in ReadScoreSaber, Top Played.");
                    Logger.log.Error(ex);
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
                    var songs = await ReadFeed(reader, feedSettings, feedPlaylist).ConfigureAwait(false);
                    readerSongs.Merge(songs);
                }
                catch (Exception ex)
                {
                    Logger.log.Error("Exception in ReadScoreSaber, Latest Ranked.");
                    Logger.log.Error(ex);
                }
            }

            sw.Stop();
            var totalPages = readerSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log.Info($"Found {readerSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} from {reader.Name} in {sw.Elapsed.ToString()}");
            return readerSongs;
        }
        #endregion
    }
}

