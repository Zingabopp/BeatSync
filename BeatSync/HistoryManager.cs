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

        /// <summary>
        /// Path to the history json file.
        /// </summary>
        public string HistoryPath { get; private set; }

        /// <summary>
        /// Key: Hash (upper case), Value: (SongKey) SongTitle by MapperName
        /// </summary>
        private ConcurrentDictionary<string, string> SongHistory;

        /// <summary>
        /// Number of entries in history.
        /// </summary>
        public int Count
        {
            get
            {
                return SongHistory?.Count ?? 0;
            }
        }

        /// <summary>
        /// HistoryManager has been initialized and is ready to use.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Creates a new HistoryManager. Uses the default history path if one isn't provided.
        /// </summary>
        /// <param name="historyPath"></param>
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
                if (string.IsNullOrEmpty(historyPath))
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
        /// Tries to add the provided songHash and songInfo to the history. Returns false if the hash is null/empty.
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
        /// Tries to add the provided PlaylistSong to the SongHistory. Returns false if the PlaylistSong is null, or its hash is null/empty.
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
            return TryAdd(song.Hash, $"({song.Key}) {song.Name} by {song.LevelAuthorName}");
        }

        /// <summary>
        /// Returns true if the provided songHash exists in history. Returns false if the songHash doesn't exist or is null/empty.
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
        /// Tries to retrieve the value of the associated songHash. Returns false with a null value if it fails.
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
        /// Tries to remove a hash from the dictionary.
        /// </summary>
        /// <param name="songHash"></param>
        /// <returns></returns>
        /// /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public bool TryRemove(string songHash)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            return SongHistory.TryRemove(songHash.ToUpper(), out _);
        }

        /// <summary>
        /// Writes the contents of HistoryPath to file. Throws an exception if it fails.
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
