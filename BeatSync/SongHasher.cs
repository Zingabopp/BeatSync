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

        private static readonly string DefaultSongCoreCachePath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"..\LocalLow\Hyperbolic Magnetism\Beat Saber\SongHashData.dat"));
        private static readonly string DefaultCustomLevelsPath = Path.GetFullPath(Path.Combine("Beat Saber_Data", "CustomLevels"));

        /// <summary>
        /// Path to the SongCore hash cache file.
        /// </summary>
        public string SongCoreCachePath { get; private set; }
        /// <summary>
        /// Directory where custom levels folders are.
        /// </summary>
        public string CustomLevelsPath { get; private set; }

        /// <summary>
        /// Creates a new SongHasher with the specified customLevelsPath and songCoreCachePath
        /// </summary>
        /// <param name="customLevelsPath"></param>
        /// <param name="songCoreCachePath"></param>
        public SongHasher(string customLevelsPath, string songCoreCachePath)
        {
            SongCoreCachePath = songCoreCachePath;
            CustomLevelsPath = customLevelsPath;
            HashDictionary = new ConcurrentDictionary<string, SongHashData>();
            ExistingSongs = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Creates a new SongHasher with the specified customLevelsPath and default SongCore cache path.
        /// </summary>
        /// <param name="customLevelsPath"></param>
        public SongHasher(string customLevelsPath)
        {
            SongCoreCachePath = DefaultSongCoreCachePath;
            CustomLevelsPath = customLevelsPath;
            HashDictionary = new ConcurrentDictionary<string, SongHashData>();
            ExistingSongs = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Creates a new SongHasher with the defaults for custom levels path and SongCore cache path
        /// </summary>
        public SongHasher()
            : this(DefaultCustomLevelsPath, DefaultSongCoreCachePath)
        {

        }

        /// <summary>
        /// Gets the directory and song hash for the specified directory.
        /// Returns null for the hash if the directory's contents aren't in the correct format.
        /// </summary>
        /// <param name="songDirectory"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when path's directory doesn't exist.</exception>
        public static async Task<SongHashData> GetSongHashDataAsync(string songDirectory)
        {
            var directoryHash = await Task.Run(() => Util.GenerateDirectoryHash(songDirectory)).ConfigureAwait(false);
            string hash = await Task.Run(() => Util.GenerateHash(songDirectory)).ConfigureAwait(false);
            return new SongHashData(directoryHash, hash);
        }

        /// <summary>
        /// Loads cached data from the file into the HashDictionary and ExistingSongs dictionary.
        /// Fails silently if the cache file doesn't exist. 
        /// </summary>
        public void LoadCachedSongHashes()
        {
            if (!File.Exists(SongCoreCachePath))
            {
                Logger.log?.Warn($"Couldn't find cached songs at {SongCoreCachePath}");
                return;
            }
            try
            {
                using (var fs = File.OpenText(SongCoreCachePath))
                using (var js = new JsonTextReader(fs))
                {
                    var ser = new JsonSerializer();
                    var token = JToken.ReadFrom(js);
                    //Better performance this way
                    foreach (JProperty item in token.Children())
                    {
                        var songHashData = item.Value.ToObject<SongHashData>();
                        var success = HashDictionary.TryAdd(Path.GetFullPath(item.Name), songHashData);
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

        /// <summary>
        /// Hashes songs that aren't in the cache. Returns the number of hashed songs.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the set song directory doesn't exist.</exception>
        public int AddMissingHashes()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var songDir = new DirectoryInfo(CustomLevelsPath);
            if (!songDir.Exists)
                throw new DirectoryNotFoundException($"Song Hasher's song directory doesn't exist: {songDir.FullName}");
            Logger.log?.Info($"SongDir is {songDir.FullName}");
            int hashedSongs = 0;
            songDir.GetDirectories().Where(d => !HashDictionary.ContainsKey(d.FullName)).ToList().AsParallel().ForAll(d =>
            {
                SongHashData data = null;
                try
                {
                    data = GetSongHashDataAsync(d.FullName).Result;
                }
                catch (DirectoryNotFoundException)
                {
                    Logger.log?.Warn($"Directory {d.FullName} does not exist, this will [probably] never happen.");
                    return;
                }
                catch(ArgumentNullException)
                {
                    Logger.log?.Warn("Somehow the directory is null in AddMissingHashes, this will [probably] never happen.");
                    return;
                }

                if (data == null)
                {
                    Logger.log?.Warn($"GetSongHashData({d.FullName}) returned null");
                    return;
                }
                else if (string.IsNullOrEmpty(data.songHash))
                {
                    Logger.log?.Warn($"GetSongHashData(\"{d.Name}\") returned a null string for hash (No info.dat?).");
                    return;
                }

                if (!ExistingSongs.TryAdd(data.songHash, d.FullName))
                    Logger.log?.Debug($"Duplicate song detected: {ExistingSongs[data.songHash].Split('\\', '/').LastOrDefault()} : {d.Name}");
                if (!HashDictionary.TryAdd(d.FullName, data))
                {
                    Logger.log?.Warn($"Couldn't add {d.FullName} to HashDictionary");
                }
                else
                {
                    hashedSongs++;
                }
                //else
                //{
                //    //Logger.log?.Info($"Added {d.Name} to the HashDictionary.");
                //}
            });
            sw.Stop();
            Logger.log?.Debug($"Finished hashing in {sw.ElapsedMilliseconds}ms.");
            return hashedSongs;
        }


    }
}
