using BeatSyncLib.Utilities;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BeatSyncLib.Hashing
{
    public abstract class SongHasher
    {
        public Type SongHashType { get; protected set; }
        public bool Initialized { get; protected set; }
        public ConcurrentDictionary<string, ISongHashData> HashDictionary;
        public ConcurrentDictionary<string, string> ExistingSongs;

        /// <summary>
        /// Directory where custom levels folders are.
        /// </summary>
        public string CustomLevelsPath { get; protected set; }

        /// <summary>
        /// Hashes songs that aren't in the cache. Returns the number of hashed songs.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if the set song directory doesn't exist.</exception>
        public async Task<int> HashDirectoryAsync()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            DirectoryInfo songDir = new DirectoryInfo(CustomLevelsPath);
            if (!songDir.Exists)
                throw new DirectoryNotFoundException($"Song Hasher's song directory doesn't exist: {songDir.FullName}");
            Logger.log?.Info($"SongDir is {songDir.FullName}");
            int hashedSongs = 0;
            var tasks = songDir.GetDirectories().Where(d => !HashDictionary.ContainsKey(d.FullName)).ToList().Select(async d =>
            {
                ISongHashData data = null;
                try
                {
                    data = await GetSongHashDataAsync(d.FullName);
                }
                catch (DirectoryNotFoundException)
                {
                    Logger.log?.Warn($"Directory {d.FullName} does not exist, this will [probably] never happen.");
                    return;
                }
                catch (ArgumentNullException)
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
            await Task.WhenAll(tasks).ConfigureAwait(false);
            sw.Stop();
            Initialized = true;
            Logger.log?.Debug($"Finished hashing in {sw.ElapsedMilliseconds}ms.");
            return hashedSongs;
        }

        /// <summary>
        /// Gets the directory and song hash for the specified directory.
        /// Returns null for the hash if the directory's contents aren't in the correct format.
        /// </summary>
        /// <param name="songDirectory"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when path is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when path's directory doesn't exist.</exception>
        public static Task<ISongHashData> GetSongHashDataAsync(string songDirectory)
        {
            return Task.Run<ISongHashData>(() => new SongHashData() { songHash = Util.GenerateHash(songDirectory) });
        }
    }

    public class SongHasher<T>
        : SongHasher
        where T : ISongHashData, new()
    {

        /// <summary>
        /// Creates a new SongHasher with the specified customLevelsPath.
        /// </summary>
        /// <param name="customLevelsPath"></param>
        public SongHasher(string customLevelsPath)
        {
            SongHashType = typeof(T);
            CustomLevelsPath = customLevelsPath;
            HashDictionary = new ConcurrentDictionary<string, ISongHashData>();
            ExistingSongs = new ConcurrentDictionary<string, string>();
        }
    }
}
