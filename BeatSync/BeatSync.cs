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

namespace BeatSync
{
    public class BeatSync : MonoBehaviour
    {
        public static BeatSync Instance { get; set; }
        public static bool PauseWork { get; set; }
        private ConcurrentDictionary<string, SongHashData> HashDictionary;

        public void Awake()
        {
            if (Instance != null)
                GameObject.DestroyImmediate(this);
            Instance = this;
            Logger.log.Warn("BeatSync Awake");
            HashDictionary = new ConcurrentDictionary<string, SongHashData>();

            FinishedHashing += OnHashFinished;

        }
        public void Start()
        {
            Logger.log.Debug("BeatSync Start()");
            LoadCachedSongHashesAsync(Plugin.CachedHashDataPath);
            Logger.log.Critical($"Read {HashDictionary.Count} cached songs.");
            var hashTask = Task.Run(() => AddMissingHashes());
            FavoriteMappers.Initialize();
        }

        public void OnHashFinished()
        {
            Logger.log.Info("Hashing finished.");
            Logger.log.Critical($"HashDictionary has {HashDictionary.Count} songs.");
            StartCoroutine(ScrapeSongsCoroutine());
        }

        public IEnumerator<WaitUntil> ScrapeSongsCoroutine()
        {
            Logger.log.Debug("Starting ScrapeSongsCoroutine");
            var readTask = RunReaders();
            yield return new WaitUntil(() => readTask.IsCompleted);

            //TestPrintReaderResults(beatSaverTask, bsaberTask, scoreSaberTask);

        }

        public void PrintSongs(string source, IEnumerable<ScrapedSong> songs)
        {
            foreach (var song in songs)
            {
                Logger.log.Warn($"{source}: {song.SongName} by {song.MapperName}, hash: {song.Hash}");
            }
        }

        public async Task TestPrintReaderResults(Task<Dictionary<string, ScrapedSong>> beatSaverTask,
            Task<Dictionary<string, ScrapedSong>> bsaberTask,
            Task<Dictionary<string, ScrapedSong>> scoreSaberTask)
        {
            await Task.WhenAll(beatSaverTask, bsaberTask, scoreSaberTask).ConfigureAwait(false);


            Logger.log.Info($"{beatSaverTask.Status.ToString()}");
            try
            {
                if (beatSaverTask.IsCompleted)
                    PrintSongs("BeatSaver", (await beatSaverTask).Values);
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
            }
            try
            {
                if (bsaberTask.IsCompleted)
                    PrintSongs("BeastSaber", (await beatSaverTask).Values);
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
            }
            try
            {
                if (scoreSaberTask.IsCompleted)
                    PrintSongs("ScoreSaber", (await beatSaverTask).Values);
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
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
                        if (!success)
                            Logger.log.Warn($"Couldn't add {songHash.Key} to the HashDictionary");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
            }
            Logger.log.Debug("Finished adding cached song hashes to the dictionary");
        }

        private void AddMissingHashes()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var songDir = new DirectoryInfo(Plugin.CustomLevelsPath);
            songDir.GetDirectories().Where(d => !HashDictionary.ContainsKey(d.FullName)).ToList().AsParallel().ForAll(d =>
            {
                var data = GetSongHashData(d.FullName).Result;
                //if (HashDictionary[d.FullName].songHash != data.songHash)
                //    Logger.log.Warn($"Hash doesn't match for {d.Name}");
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
            HashFinished = true;
            FinishedHashing?.Invoke();
        }
        private static bool HashFinished = false;
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
                //readerTasks.Add(ReadScoreSaber());
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
            var allPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncAll);
            var recentPlaylist = config.RecentPlaylistDays > 0 ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncRecent) : null;
            foreach (var scrapedSong in songsToDownload.Values)
            {
                var playlistSong = new PlaylistSong(scrapedSong.Hash, scrapedSong.SongName, scrapedSong.SongKey);
                allPlaylist?.TryAdd(playlistSong);
                recentPlaylist?.TryAdd(playlistSong);
            }
            allPlaylist?.TryWriteFile();
            if (recentPlaylist != null && config.RecentPlaylistDays > 0)
            {
                var minDate = DateTime.Now - new TimeSpan(config.RecentPlaylistDays, 0, 0, 0);
                recentPlaylist.Songs.ForEach(s => Console.WriteLine(s.DateAdded.ToString()));
                int removedCount = recentPlaylist.Songs.RemoveAll(s => s.DateAdded < minDate);
                Logger.log.Info($"Removed {removedCount} old songs from the RecentPlaylist.");
                recentPlaylist.TryWriteFile();
            }

        }

        private async Task<Dictionary<string, ScrapedSong>> RunReader(IFeedReader reader, IFeedSettings settings, Playlist feedPlaylist)
        {

            var songs = await reader.GetSongsFromFeedAsync(settings).ConfigureAwait(false) ?? new Dictionary<string, ScrapedSong>();
            foreach (var scrapedSong in songs)
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
                var song = new PlaylistSong(scrapedSong.Value.Hash, scrapedSong.Value.SongName, scrapedSong.Value.SongKey);

                feedPlaylist?.TryAdd(song);
            }

            return songs;
        }

        #region Feed Read Functions
        private async Task<Dictionary<string, ScrapedSong>> ReadBeastSaber()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log.Info("Starting BeastSaber reading");

            var config = Plugin.config.Value;
            BeastSaberReader reader = null;
            try
            {
                reader = new BeastSaberReader(config.BeastSaber.Username, config.BeastSaber.MaxConcurrentPageChecks);
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
                return null;
            }
            var beastSaberSongs = new Dictionary<string, ScrapedSong>();
            if (config.BeastSaber.Bookmarks.Enabled)
            {
                try
                {
                    Logger.log.Info("Getting songs from BeastSaber Bookmarks feed.");

                    var feedPlaylist = config.BeastSaber.Bookmarks.CreatePlaylist ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeastSaberBookmarks) : null;
                    var bookmarks = await RunReader(reader, config.BeastSaber.Bookmarks.ToFeedSettings(), feedPlaylist).ConfigureAwait(false);
                    feedPlaylist?.TryWriteFile();
                    beastSaberSongs.Merge(bookmarks);
                    var pages = bookmarks.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    Logger.log.Info($"Found {bookmarks.Count} songs from {pages} {(pages == 1 ? "page" : "pages")} in the BeastSaber Bookmarks feed.");

                }
                catch (ArgumentException ex)
                {
                    Logger.log.Critical("Exception in BeastSaber Bookmarks: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                }


            }
            if (config.BeastSaber.Followings.Enabled)
            {
                try
                {
                    Logger.log.Info("Getting songs from BeastSaber Followings feed.");

                    var feedPlaylist = config.BeastSaber.Followings.CreatePlaylist ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeastSaberFollows) : null;
                    var follows = await RunReader(reader, config.BeastSaber.Followings.ToFeedSettings(), feedPlaylist).ConfigureAwait(false);
                    feedPlaylist?.TryWriteFile();
                    beastSaberSongs.Merge(follows);
                    var pages = follows.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    Logger.log.Info($"Found {follows.Count} songs from {pages} {(pages == 1 ? "page" : "pages")} in the BeastSaber Followings feed.");

                }
                catch (ArgumentException ex)
                {
                    Logger.log.Critical("Exception in BeastSaber Followings: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                }
            }
            if (config.BeastSaber.CuratorRecommended.Enabled)
            {
                try
                {
                    Logger.log.Info("Getting songs from BeastSaber Curator Recommended feed.");
                    var feedPlaylist = config.BeastSaber.CuratorRecommended.CreatePlaylist ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeastSaberCurator) : null;
                    var curator = await RunReader(reader, config.BeastSaber.CuratorRecommended.ToFeedSettings(), feedPlaylist).ConfigureAwait(false);
                    feedPlaylist?.TryWriteFile();
                    beastSaberSongs.Merge(curator);
                    var pages = curator.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    Logger.log.Info($"Found {curator.Count} songs from {pages} {(pages == 1 ? "page" : "pages")} in the BeastSaber Curator Recommended feed.");

                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                }
            }
            sw.Stop();
            var totalPages = beastSaberSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log.Info($"Found {beastSaberSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} from {reader.Name} in {sw.Elapsed.ToString()}");
            return beastSaberSongs;
        }

        private async Task<Dictionary<string, ScrapedSong>> ReadBeatSaver(Playlist allPlaylist = null)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.log.Info("Starting BeatSaver reading");

            var config = Plugin.config.Value;
            BeatSaverReader reader = null;
            try
            {
                reader = new BeatSaverReader();
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex);
                return null;
            }
            var beatSaverSongs = new Dictionary<string, ScrapedSong>();
            if (config.BeatSaver.FavoriteMappers.Enabled)
            {
                Logger.log.Info("Getting songs from authors in FavoriteMappers.ini.");
                try
                {
                    var settings = config.BeatSaver.FavoriteMappers.ToFeedSettings();
                    if (settings.Authors.Length > 0)
                    {
                        var feedPlaylist = config.BeatSaver.FavoriteMappers.CreatePlaylist ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSaverFavoriteMappers) : null;
                        var favMappers = await RunReader(reader, config.BeatSaver.FavoriteMappers.ToFeedSettings(), feedPlaylist).ConfigureAwait(false);
                        feedPlaylist?.TryWriteFile();
                        beatSaverSongs.Merge(favMappers);
                        var pages = favMappers.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                        Logger.log.Info($"Found {favMappers.Count} songs from {pages} {(pages == 1 ? "page" : "pages")} in the BeatSaver FavoriteMappers feed.");
                    }
                    else
                    {
                        Logger.log.Warn($"BeatSaver FavoriteMappers feed is enabled, but no mappers were found in FavoriteMappers.ini.");
                    }

                }
                catch (ArgumentException ex)
                {
                    Logger.log.Critical("Exception in BeatSaver FavoriteMappers: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                }


            }
            if (config.BeatSaver.Hot.Enabled)
            {

                Logger.log.Info("Getting songs from BeatSaver Hot feed.");
                try
                {
                    var feedPlaylist = config.BeatSaver.Hot.CreatePlaylist ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSaverHot) : null;
                    var hot = await RunReader(reader, config.BeatSaver.Hot.ToFeedSettings(), feedPlaylist).ConfigureAwait(false);
                    feedPlaylist?.TryWriteFile();
                    beatSaverSongs.Merge(hot);
                    var pages = hot.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    Logger.log.Info($"Found {hot.Count} songs from {pages} {(pages == 1 ? "page" : "pages")} in the BeatSaver Hot feed.");

                }
                catch (ArgumentException ex)
                {
                    Logger.log.Critical("Exception in BeatSaver Hot: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                }
            }
            if (config.BeatSaver.Downloads.Enabled)
            {
                Logger.log.Info("Getting songs from BeatSaver Downloads feed.");
                try
                {
                    var feedPlaylist = config.BeatSaver.Downloads.CreatePlaylist ? PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSaverDownloads) : null;
                    var songs = await RunReader(reader, config.BeatSaver.Downloads.ToFeedSettings(), feedPlaylist).ConfigureAwait(false);
                    feedPlaylist?.TryWriteFile();
                    var pages = songs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
                    Logger.log.Info($"Found {songs.Count} songs from {pages} {(pages == 1 ? "page" : "pages")} in the BeatSaver Downloads feed.");
                }
                catch (Exception ex)
                {
                    Logger.log.Error(ex);
                }
            }


            sw.Stop();
            var totalPages = beatSaverSongs.Values.Select(s => s.SourceUri.ToString()).Distinct().Count();
            Logger.log.Info($"Found {beatSaverSongs.Count} songs on {totalPages} {(totalPages == 1 ? "page" : "pages")} from {reader.Name} in {sw.Elapsed.ToString()}");
            return beatSaverSongs;
        }
        #endregion
    }
}
