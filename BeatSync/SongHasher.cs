using BeatSync.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SongCore.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync
{
    public class SongHasher
    {
        public ConcurrentDictionary<string, SongHashData> HashDictionary;
        public ConcurrentDictionary<string, string> ExistingSongs;

        public SongHasher()
        {
            HashDictionary = new ConcurrentDictionary<string, SongHashData>();
            ExistingSongs = new ConcurrentDictionary<string, string>();
        }

        public void OnHashingFinished()
        {
            Logger.log?.Info("Hashing finished.");
            Logger.log?.Critical($"HashDictionary has {HashDictionary.Count} songs.");
            //StartCoroutine(ScrapeSongsCoroutine());
        }

        private async Task<SongHashData> GetSongHashData(string songDirectory)
        {

            var directoryHash = await Task.Run(() => Util.GenerateDirectoryHash(songDirectory)).ConfigureAwait(false);
            string hash = await Task.Run(() => Util.GenerateHash(songDirectory)).ConfigureAwait(false);

            return new SongHashData(directoryHash, hash);
        }

        public void LoadCachedSongHashesAsync(string cachedHashPath)
        {
            if (!File.Exists(cachedHashPath))
            {
                Logger.log?.Warn($"Couldn't find cached songs at {cachedHashPath}");
                return;
            }
            try
            {
                using (var fs = File.OpenText(cachedHashPath))
                using (var js = new JsonTextReader(fs))
                {
                    var ser = new JsonSerializer();
                    var token = JToken.ReadFrom(js);
                    //Better performance this way
                    foreach (JProperty item in token.Children())
                    {
                        var songHashData = item.Value.ToObject<SongHashData>();
                        var success = HashDictionary.TryAdd(item.Name, songHashData);
                        ExistingSongs.TryAdd(songHashData.songHash, item.Name);
                        if (!success)
                            Logger.log?.Warn($"Couldn't add {item.Name} to the HashDictionary");
                    }
                    //var songHashes = ser.Deserialize<Dictionary<string, SongHashData>>(js);
                    //foreach (var songHash in songHashes)
                    //{
                    //    var success = HashDictionary.TryAdd(songHash.Key, songHash.Value);
                    //    ExistingSongs.TryAdd(songHash.Value.songHash, songHash.Key);
                    //    if (!success)
                    //        Logger.log?.Warn($"Couldn't add {songHash.Key} to the HashDictionary");
                    //}
                    Logger.log?.Info($"Added {HashDictionary.Count} song hashes from SongCore's cache.");
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Error("Exception in LoadCachedSongHashesAsync");
                Logger.log?.Error(ex);
            }
            Logger.log?.Debug("Finished adding cached song hashes to the dictionary");
        }

        private void AddMissingHashes()
        {
            Logger.log?.Info("Starting AddMissingHashes");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var songDir = new DirectoryInfo(Plugin.CustomLevelsPath);
            Logger.log?.Info($"SongDir is {songDir.FullName}");
            songDir.GetDirectories().Where(d => !HashDictionary.ContainsKey(d.FullName)).ToList().AsParallel().ForAll(d =>
            {
                var data = GetSongHashData(d.FullName).Result;
                if (data == null)
                {
                    Logger.log?.Warn($"GetSongHashData({d.FullName}) returned null");
                    return;
                }
                else if (string.IsNullOrEmpty(data.songHash))
                {
                    Logger.log?.Warn($"GetSongHashData({d.FullName}) returned a null string for hash.");
                    return;
                }
                //if (HashDictionary[d.FullName].songHash != data.songHash)
                //    Logger.log?.Warn($"Hash doesn't match for {d.Name}");
                try
                {
                    ExistingSongs.TryAdd(data.songHash, d.FullName);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Exception in AddMissingHashes.\n{ex.Message}\n{ex.StackTrace}");
                }
                if (!HashDictionary.TryAdd(d.FullName, data))
                {
                    Logger.log?.Warn($"Couldn't add {d.FullName} to HashDictionary");
                }
                else
                {
                    //Logger.log?.Info($"Added {d.Name} to the HashDictionary.");
                }
            });
            sw.Stop();
            Logger.log?.Warn($"Finished hashing in {sw.ElapsedMilliseconds}ms, Triggering FinishedHashing");
            FinishedHashing?.Invoke();
        }

        public event Action FinishedHashing;

    }
}
