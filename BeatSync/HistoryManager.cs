using BeatSync.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync
{
    public static class HistoryManager
    {
        public static string DefaultHistoryPath => Path.Combine(Plugin.UserDataPath, "BeatSyncHistory.json");
        public static string HistoryPath { get; private set; }

        public static int Count
        {
            get
            {
                return SongHistory?.Count ?? 0;
            }
        }

        public static bool IsInitialized { get; private set; }

        static HistoryManager()
        {
            HistoryPath = DefaultHistoryPath;
            SongHistory = new ConcurrentDictionary<string, string>();
        }
        /// <summary>
        /// Key: Hash (upper case), Value: SongTitle - MapperName
        /// </summary>
        private static ConcurrentDictionary<string, string> SongHistory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="songHash"></param>
        /// <param name="songInfo"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public static bool TryAdd(string songHash, string songInfo)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            if (string.IsNullOrEmpty(songHash))
                return false;
                //throw new ArgumentNullException(nameof(songHash), "songHash cannot be null for HistoryManager.TryAdd");
            return SongHistory.TryAdd(songHash.ToUpper(), songInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public static bool TryAdd(Playlists.PlaylistSong song)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            if (song == null || string.IsNullOrEmpty(song.Hash))
                return false;
            return SongHistory.TryAdd(song.Hash, $"({song.Key}) {song.Name} by {song.LevelAuthorName}");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="songHash"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public static bool ContainsKey(string songHash)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            if (string.IsNullOrEmpty(songHash))
                return false; // May not need this.
            return SongHistory.ContainsKey(songHash.ToUpper());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="songHash"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public static bool TryGetValue(string songHash, out string value)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            return SongHistory.TryGetValue(songHash.ToUpper(), out value);
        }

        public static void Initialize(string historyPath = "")
        {
            if(IsInitialized)
            {
                if (string.IsNullOrEmpty(historyPath) && HistoryPath.Equals(DefaultHistoryPath))
                    return;
                else if (historyPath != null && HistoryPath.Equals(historyPath))
                    return;
            }
            if(!string.IsNullOrEmpty(historyPath))
            {
                HistoryPath = Path.GetFullPath(historyPath);
            }
            // Load from file.
            if (File.Exists(HistoryPath))
            {
                JsonConvert.PopulateObject(FileIO.LoadStringFromFile(HistoryPath), SongHistory);
            }
            else
                SongHistory = new ConcurrentDictionary<string, string>();
            IsInitialized = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public static void WriteToDisk()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            if (File.Exists(HistoryPath))
            {
                File.Copy(HistoryPath, HistoryPath + ".bak", true);
                File.Delete(HistoryPath);
            }
            using (var sw = File.CreateText(HistoryPath))
            {
                var serializer = new JsonSerializer() { Formatting = Formatting.Indented };
                serializer.Serialize(sw, SongHistory);
            }
            File.Delete(HistoryPath + ".bak");
        }
        

    }
}
