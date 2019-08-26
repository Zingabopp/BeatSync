using BeatSync.Playlists;
using BeatSync.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

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
        /// Key: Hash (upper case), Value: HistoryEntry with PlaylistSong.ToString() and HistoryFlag
        /// </summary>
        private ConcurrentDictionary<string, HistoryEntry> SongHistory;

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
            SongHistory = new ConcurrentDictionary<string, HistoryEntry>();
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
                var histStr = FileIO.LoadStringFromFile(HistoryPath);
                var token = JToken.Parse(histStr);
                foreach (JObject entry in token.Children())
                {
                    var historyEntry = new HistoryEntry();
                    var hash = entry["Key"].Value<string>();
                    historyEntry.SongInfo = entry["Value"]["SongInfo"].Value<string>();
                    historyEntry.Flag = (HistoryFlag)(entry["Value"]["Flag"].Value<int>());
                    historyEntry.Date = entry["Value"]["Date"].Value<DateTime>();

                    SongHistory.TryAdd(hash, historyEntry);
                }

            }
            IsInitialized = true;
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
            var orderedDictionary = SongHistory.OrderByDescending(kvp => kvp.Value.Date).ToArray();
            using (var sw = File.CreateText(HistoryPath))
            {
                var serializer = new JsonSerializer() { Formatting = Formatting.Indented };
                serializer.Serialize(sw, orderedDictionary);
            }
            File.Delete(HistoryPath + ".bak");
        }

        /// <summary>
        /// Tries to add the provided songHash and songInfo to the history. Returns false if the hash is null/empty.
        /// </summary>
        /// <param name="songHash"></param>
        /// <param name="songInfo"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public bool TryAdd(string songHash, string songInfo, HistoryFlag flag)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            if (string.IsNullOrEmpty(songHash))
                return false;
            return SongHistory.TryAdd(songHash.ToUpper(), new HistoryEntry(songInfo, flag));
        }

        /// <summary>
        /// Tries to add the provided PlaylistSong to the SongHistory. Returns false if the PlaylistSong is null, or its hash is null/empty.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public bool TryAdd(PlaylistSong song, HistoryFlag flag)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            if (song == null)
                return false;
            return TryAdd(song.Hash, song.ToString(), flag);
        }

        /// <summary>
        /// If the provided song hash exists in history, change the flag to the one provided. Returns true if the song was found.
        /// </summary>
        /// <param name="songHash"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public bool TryUpdateFlag(string songHash, HistoryFlag flag)
        {
            songHash = songHash.ToUpper();
            if (!SongHistory.ContainsKey(songHash))
                return false;
            SongHistory[songHash].Flag = flag;
            SongHistory[songHash].Date = DateTime.Now;
            return true;
        }

        /// <summary>
        /// If the provided song exists in history, change the flag to the one provided. Returns true if the song was found.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public bool TryUpdateFlag(PlaylistSong song, HistoryFlag flag)
        {
            if (song == null)
                return false;
            return TryUpdateFlag(song.Hash, flag);
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
        public bool TryGetValue(string songHash, out HistoryEntry value)
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
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public bool TryRemove(string songHash)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            return SongHistory.TryRemove(songHash.ToUpper(), out _);
        }

        public bool TryUpdateDate(string songHash, DateTime newDate)
        {
            songHash = songHash.ToUpper();
            if (!SongHistory.ContainsKey(songHash))
                return false;
            SongHistory[songHash].Date = newDate;
            return true;
        }

        public bool TryUpdateDate(PlaylistSong song, DateTime newDate)
        {
            if (song == null)
                return false;
            return TryUpdateDate(song.Hash, newDate);
        }


    }

    public class HistoryEntry
    {
        public HistoryEntry() { }
        public HistoryEntry(string songInfo, HistoryFlag flag = 0)
        {
            SongInfo = songInfo;
            Flag = flag;
            Date = DateTime.Now;
        }

        public HistoryEntry(PlaylistSong song, HistoryFlag flag = 0)
        {
            SongInfo = song.ToString();
            Flag = flag;
            Date = DateTime.Now;
        }

        public string SongInfo { get; set; }
        public HistoryFlag Flag { get; set; }
        public DateTime Date { get; set; }
    }

    public enum HistoryFlag
    {
        /// <summary>
        /// Not set.
        /// </summary>
        None = 0,
        /// <summary>
        /// Downloaded by BeatSync.
        /// </summary>
        Downloaded = 1,
        /// <summary>
        /// Confirmed deleted.
        /// </summary>
        Deleted = 2,
        /// <summary>
        /// Used to exist, now it doesn't
        /// </summary>
        Missing = 3,
        /// <summary>
        /// Downloaded without BeatSync
        /// </summary>
        PreExisting = 4,
        /// <summary>
        /// Not found on Beat Saver.
        /// </summary>
        NotFound = 404

    }
}
