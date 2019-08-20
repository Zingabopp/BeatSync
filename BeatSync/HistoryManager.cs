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
    public class HistoryManager
    {
        public static string DefaultHistoryPath => Path.Combine(Plugin.UserDataPath, "BeatSyncHistory.json");
        public string HistoryPath { get; private set; }

        public int Count
        {
            get
            {
                return SongHistory?.Count ?? 0;
            }
        }

        public bool IsInitialized { get; private set; }

        public HistoryManager(string historyPath = "")
        {
            if (!string.IsNullOrEmpty(historyPath))
            {
                HistoryPath = Path.GetFullPath(historyPath);
            }
            else
            {
                HistoryPath = DefaultHistoryPath;
            }
            SongHistory = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Must be called before doing any other operations. Attempts to load the song history from the json.
        /// If already Initialized and the historyPath isn't changed, does nothing. If the historyPath is changed,
        ///  current history is cleared and loaded from file.
        /// </summary>
        /// <param name="historyPath"></param>
        public void Initialize(string historyPath = "")
        {
            if (IsInitialized)
            {
                if (string.IsNullOrEmpty(historyPath) && HistoryPath.Equals(DefaultHistoryPath))
                    return;
                else if (historyPath != null && HistoryPath.Equals(historyPath))
                    return;
            }
            if (!string.IsNullOrEmpty(historyPath))
            {
                HistoryPath = Path.GetFullPath(historyPath);
            }
            // Load from file.
            SongHistory.Clear();
            if (File.Exists(HistoryPath))
            {
                JsonConvert.PopulateObject(FileIO.LoadStringFromFile(HistoryPath), SongHistory);
            }
            else
            {
                SongHistory = new ConcurrentDictionary<string, string>();
            }
            IsInitialized = true;
        }

        /// <summary>
        /// Key: Hash (upper case), Value: SongTitle - MapperName
        /// </summary>
        private ConcurrentDictionary<string, string> SongHistory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="songHash"></param>
        /// <param name="songInfo"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public bool TryAdd(string songHash, string songInfo)
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
        public bool TryAdd(Playlists.PlaylistSong song)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            if (song == null || string.IsNullOrEmpty(song.Hash))
                return false; // This will never happen because PlaylistSong.Hash can never be null or empty.
            return SongHistory.TryAdd(song.Hash, $"({song.Key}) {song.Name} by {song.LevelAuthorName}");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="songHash"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public bool ContainsKey(string songHash)
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
        public bool TryGetValue(string songHash, out string value)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            return SongHistory.TryGetValue(songHash.ToUpper(), out value);
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        /// <exception cref="IOException">Thrown when there's a file system problem writing to file.</exception>
        public void WriteToFile()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            if (File.Exists(HistoryPath))
            {
                File.Copy(HistoryPath, HistoryPath + ".bak", true);
                File.Delete(HistoryPath);
            }
            var file = new FileInfo(HistoryPath);
            file.Directory.Create();
            using (var sw = File.CreateText(HistoryPath))
            {
                var serializer = new JsonSerializer() { Formatting = Formatting.Indented };
                serializer.Serialize(sw, SongHistory);
            }
            File.Delete(HistoryPath + ".bak");
        }
        

    }
}
