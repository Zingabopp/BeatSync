using BeatSyncLib.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
        
        public bool DeDuplicate { get; protected set; }

        private Task<int> _initializingTask;
        private object _initializingLock = new object();

        public Task<int> InitializeAsync()
        {
            lock (_initializingLock)
            {
                if (_initializingTask == null)
                    _initializingTask = HashDirectoryAsync();
            }
            return _initializingTask;
        }

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
            //Logger.log?.Info($"SongDir is {songDir.FullName}");
            int hashedSongs = 0;
            IEnumerable<Task>? directoryTasks = songDir.GetDirectories().Where(d => !HashDictionary.ContainsKey(d.FullName)).ToList().Select(async d =>
            {
                ISongHashData? data = null;
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
                catch (JsonException ex)
                {
                    Logger.log?.Warn($"Invalid JSON in beatmap at '{d.FullName}', skipping. {ex.Message}");
                    return;
                }
                catch (Exception ex)
                {
                    Logger.log?.Warn($"Unhandled exception hashing beatmap at '{d.FullName}', skipping. {ex.Message}");
                    Logger.log?.Debug(ex);
                    return;
                }

                if (data == null)
                {
                    Logger.log?.Warn($"GetSongHashData({d.FullName}) returned null");
                    return;
                }
                else if (data.songHash == null || data.songHash.Length == 0)
                {
                    Logger.log?.Warn($"GetSongHashData(\"{d.Name}\") returned a null string for hash (No info.dat?).");
                    return;
                }

                if (!ExistingSongs.TryAdd(data.songHash, d.FullName))
                {
                    Logger.log?.Debug(
                        $"Duplicate song detected: {ExistingSongs[data.songHash]?.Split('\\', '/').LastOrDefault()} : {d.Name}");
                    if (DeDuplicate)
                    {
                        try
                        {
                            d.Delete(true);
                        }
                        catch (Exception ex)
                        {
                            Logger.log?.Warn($"Couldn't delete duplicate song: {ExistingSongs[data.songHash]?.Split('\\', '/').LastOrDefault()} : {d.Name}");
                        }
                    }
                }

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
            await Task.WhenAll(directoryTasks).ConfigureAwait(false);
            var files = songDir.GetFiles().Where(f => !HashDictionary.ContainsKey(f.FullName) && f.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
            IEnumerable<Task>? fileTasks = files.Select(async f =>
            {
                ISongHashData? data = null;
                try
                {
                    data = await GetZippedSongHashAsync(f.FullName).ConfigureAwait(false);
                }
                catch (DirectoryNotFoundException)
                {
                    Logger.log?.Warn($"Zip file '{f.FullName}' does not exist, this will [probably] never happen.");
                    return;
                }
                catch (ArgumentNullException)
                {
                    Logger.log?.Warn("Somehow the file is null in AddMissingHashes, this will [probably] never happen.");
                    return;
                }
                catch (Exception ex)
                {
                    Logger.log?.Warn($"Unhandled exception hashing beatmap at '{f.FullName}', skipping. {ex.Message}");
                    Logger.log?.Debug(ex);
                    return;
                }

                if (data == null)
                {
                    Logger.log?.Warn($"GetZippedSongHashAsync({f.FullName}) returned null");
                    return;
                }
                else if (data.songHash == null || data.songHash.Length == 0)
                {
                    Logger.log?.Warn($"GetZippedSongHashAsync(\"{f.Name}\") returned a null string for hash (No info.dat?).");
                    return;
                }

                if (!ExistingSongs.TryAdd(data.songHash, f.FullName))
                {
                    Logger.log?.Debug(
                        $"Duplicate song detected: {ExistingSongs[data.songHash]?.Split('\\', '/').LastOrDefault()} : {f.Name}");
                    
                    if (DeDuplicate)
                    {
                        try
                        {
                            f.Delete();
                        }
                        catch (Exception ex)
                        {
                            Logger.log?.Warn($"Couldn't delete duplicate song: {ExistingSongs[data.songHash]?.Split('\\', '/').LastOrDefault()} : {f.Name}");
                        }
                    }
                }

                if (!HashDictionary.TryAdd(f.FullName, data))
                {
                    Logger.log?.Warn($"Couldn't add {f.FullName} to HashDictionary");
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
            await Task.WhenAll(fileTasks).ConfigureAwait(false);
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

        public static Task<ISongHashData> GetZippedSongHashAsync(string zipPath, string existingHash = "")
            => Task.Run<ISongHashData>(() => new SongHashData() { songHash = GetZippedSongHash(zipPath) });

        public static string? GetZippedSongHash(string zipPath, string existingHash = "")
        {
            if (!File.Exists(zipPath))
                return null;
            ZipArchive zip;
            try
            {
                zip = ZipFile.OpenRead(zipPath);
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Unable to hash beatmap zip '{zipPath}': {ex.Message}");
                Logger.log?.Debug(ex);
                return null;
            }
            ZipArchiveEntry infoFile = zip.Entries.FirstOrDefault(e => e.FullName.Equals("info.dat", StringComparison.OrdinalIgnoreCase));
            if (infoFile == null)
            {
                Logger.log?.Debug($"'{zipPath}' does not have an 'info.dat' file.");
                return null;
            }
            try
            {
                List<byte> combinedBytes = new List<byte>((int)infoFile.Length);
                using (Stream infoStream = infoFile.Open())
                {
                    int current = infoStream.ReadByte();
                    while (current != -1)
                    {
                        combinedBytes.Add((byte)current);
                        current = infoStream.ReadByte();
                    }
                }
                string infoString;
                using (Stream infoStream = infoFile.Open())
                {
                    using StreamReader reader = new StreamReader(infoStream);
                    infoString = reader.ReadToEnd();
                }

                JToken? token = JToken.Parse(infoString);
                JToken? beatMapSets = token["_difficultyBeatmapSets"];
                int numChars = beatMapSets?.Children().Count() ?? 0;
                for (int i = 0; i < numChars; i++)
                {
                    JToken? diffs = beatMapSets.ElementAt(i);
                    int numDiffs = diffs["_difficultyBeatmaps"]?.Children().Count() ?? 0;
                    for (int i2 = 0; i2 < numDiffs; i2++)
                    {
                        JToken? diff = diffs["_difficultyBeatmaps"].ElementAt(i2);
                        string? beatmapFileName = diff["_beatmapFilename"]?.Value<string>();
                        ZipArchiveEntry beatmapFile = zip.Entries.FirstOrDefault(e => e.Name.Equals(beatmapFileName, StringComparison.OrdinalIgnoreCase));
                        if (beatmapFile != null)
                        {
                            using Stream beatmapStream = beatmapFile.Open();
                            int current = beatmapStream.ReadByte();
                            while (current != -1)
                            {
                                combinedBytes.Add((byte)current);
                                current = beatmapStream.ReadByte();
                            }
                        }
                        else
                            Logger.log?.Debug($"Missing difficulty file {beatmapFileName} in {zipPath}");
                    }
                }
                zip.Dispose();
                string hash = Util.CreateSha1FromBytes(combinedBytes.ToArray());
                if (!string.IsNullOrEmpty(existingHash) && existingHash != hash)
                    Logger.log?.Warn($"Hash doesn't match the existing hash for {zipPath}");
                return hash;
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Unable to hash beatmap zip '{zipPath}': {ex.Message}");
                Logger.log?.Debug(ex);
                return null;
            }
        }

        public static long GetDirectoryHash(string directory)
        {
            long hash = 0;
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            foreach (FileInfo f in directoryInfo.GetFiles())
            {
                hash ^= f.CreationTimeUtc.ToFileTimeUtc();
                hash ^= f.LastWriteTimeUtc.ToFileTimeUtc();
                hash ^= GetStringHash(f.Name);
                hash ^= f.Length;
            }
            return hash;
        }

        public static long GetQuickZipHash(string zipPath)
        {
            long hash = 6271;
            FileInfo f = new FileInfo(zipPath);
            hash ^= f.CreationTimeUtc.ToFileTimeUtc();
            hash ^= f.LastWriteTimeUtc.ToFileTimeUtc();
            hash ^= GetStringHash(f.Name);
            hash ^= f.Length;
            return hash;
        }

        private static int GetStringHashSafe(string str)
        {
            char[] src = str.ToCharArray();

            int hash1 = 5381;
            int hash2 = hash1;
            int c;
            int s = 0;
            try
            {
                while ((c = src[s]) != 0)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ c;
                    c = src[s];
                    if (c == 0)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ c;
                    s += 2;
                }
                return hash1 + (hash2 * 1566083941);
            }
            catch (Exception)
            {

                return 0;
            }
        }

        private static int GetStringHash(string str)
        {
            unsafe
            {
                fixed (char* src = str)
                {
                    int hash1 = 5381;
                    int hash2 = hash1;
                    int c;
                    char* s = src;
                    while ((c = s[0]) != 0)
                    {
                        hash1 = ((hash1 << 5) + hash1) ^ c;
                        c = s[1];
                        if (c == 0)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ c;
                        s += 2;
                    }
                    return hash1 + (hash2 * 1566083941);
                }
            }
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
        public SongHasher(string customLevelsPath, bool deDuplicate = false)
        {
            SongHashType = typeof(T);
            DeDuplicate = deDuplicate;
            CustomLevelsPath = customLevelsPath;
            HashDictionary = new ConcurrentDictionary<string, ISongHashData>();
            ExistingSongs = new ConcurrentDictionary<string, string>();
        }
    }
}
