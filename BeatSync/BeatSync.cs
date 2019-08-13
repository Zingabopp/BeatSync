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
        }

        public void OnHashFinished()
        {
            Logger.log.Info("Hashing finished.");
            Logger.log.Critical($"HashDictionary has {HashDictionary.Count} songs.");
            StartCoroutine(ScrapeSongsCoroutine());
        }

        public IEnumerator<WaitForSeconds> ScrapeSongsCoroutine()
        {
            yield return null;
            Logger.log.Debug("Starting ScrapeSongsCoroutine");
            var beatSaverReader = new SongFeedReaders.BeatSaverReader();
            var bsaberReader = new BeastSaberReader("Zingabopp", 5);
            var scoreSaberReader = new ScoreSaberReader();
            Logger.log.Warn($"BS: {beatSaverReader != null}, BSa: {bsaberReader != null}, SS: {scoreSaberReader != null}");
            //var beatSaverTask = Task.Run(() => beatSaverReader.GetSongsFromFeedAsync(new BeatSaverFeedSettings((int)BeatSaverFeed.Hot) { MaxSongs = 70 }));
            //var bsaberTask = Task.Run(() => bsaberReader.GetSongsFromFeedAsync(new BeastSaberFeedSettings(0) { MaxSongs = 70 }));
            //var scoreSaberTask = Task.Run(() => scoreSaberReader.GetSongsFromFeedAsync(new ScoreSaberFeedSettings(0) { MaxSongs = 70 }));
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
            if(!File.Exists(cachedHashPath))
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
            }catch(Exception ex)
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
                if (HashFinished)
                    Logger.log.Warn("wtf, still hashing after HashFinished");
                if(!HashDictionary.TryAdd(d.FullName, data))
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
            var directoryHash = await Task.Run(() => Utilities.GenerateDirectoryHash(songDirectory)).ConfigureAwait(false);
            string hash = await Task.Run(() => Utilities.GenerateHash(songDirectory)).ConfigureAwait(false);

            return new SongHashData(directoryHash, hash);
        }

        public event Action FinishedHashing;
    }
}
