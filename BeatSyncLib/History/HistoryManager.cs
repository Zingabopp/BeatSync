using SongFeedReaders.Data;
using BeatSyncLib.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using BeatSyncLib.Configs.Converters;

namespace BeatSyncLib.History
{
    public class HistoryManager
    {
        /// <summary>
        /// Path to the history json file.
        /// </summary>
        public string HistoryPath { get; private set; }

        /// <summary>
        /// Key: Hash (upper case), Value: HistoryEntry with PlaylistSong.ToString() and HistoryFlag
        /// </summary>
        private ConcurrentDictionary<string, HistoryEntry> SongHistory;

        public string[] GetSongHashes()
        {
            return SongHistory.Keys.ToArray();
        }

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
        /// Creates a new HistoryManager.
        /// </summary>
        /// <param name="historyPath"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public HistoryManager(string historyPath)
        {
            if (string.IsNullOrEmpty(historyPath))
                throw new ArgumentNullException(nameof(historyPath), "historyPath cannot be null when creating a new HistoryManager.");
            HistoryPath = Path.GetFullPath(historyPath);
            SongHistory = new ConcurrentDictionary<string, HistoryEntry>();
        }

        /// <summary>
        /// Must be called before doing any other operations. Attempts to load the song history from the json.
        /// If already Initialized and the historyPath isn't changed, does nothing. If the historyPath is changed,
        ///  current history is cleared and loaded from file.
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }
            // Load from file.
            SongHistory.Clear();
            try
            {
                if (File.Exists(HistoryPath))
                {
                    var histStr = FileIO.LoadStringFromFile(HistoryPath);
                    var token = JToken.Parse(histStr);
                    foreach (JObject entry in token.Children())
                    {
                        string? keyHash = entry?["Key"]?.Value<string>();
                        HistoryEntry? historyEntry = entry?["Value"]?.ToObject<HistoryEntry>();
                        if (keyHash != null && historyEntry != null)
                        {
                            SongHistory.TryAdd(keyHash, historyEntry);
                        }
                        else
                        {
                            Logger.log?.Warn($"Invalid HistoryEntry: {keyHash}");
                        }
                    }
                }
                else
                    Logger.log?.Warn($"History file not found at {HistoryPath}");
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"HistoryManager failed to initialize: {ex.Message}");
                Logger.log?.Debug(ex.StackTrace);
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

        public bool TryWriteToFile(out Exception exception)
        {
            try
            {
                WriteToFile();
                exception = null;
                return true;
            }
            catch (Exception ex)
            {
                exception = ex;
                return false;
            }
        }

        public bool TryWriteToFile(bool logError = true)
        {
            bool successful = TryWriteToFile(out var exception);
            if (!successful && logError)
            {
                Logger.log?.Error($"Error writing history to file: {exception.Message}");
                Logger.log?.Debug(exception);
            }
            return successful;
        }


        public bool TryAdd(string? songHash, HistoryEntry historyEntry)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            if (historyEntry == null)
                throw new ArgumentNullException(nameof(historyEntry), "Cannot add a null HistoryEntry.");
            if (songHash == null || songHash.Length == 0)
                return false;
            return SongHistory.TryAdd(songHash.ToUpper(), historyEntry);
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
        public bool TryAdd(string songHash, string songName, string mapper, HistoryFlag flag)
        {
            return TryAdd(songHash, $"{songName} by {mapper}", flag);
        }

        /// <summary>
        /// Tries to add the provided PlaylistSong to the SongHistory. Returns false if the PlaylistSong is null, or its hash is null/empty.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public bool TryAdd(ISong song, HistoryFlag flag)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            if (song == null || song.Hash == null || song.Hash.Length == 0)
                return false;
            return SongHistory.TryAdd(song.Hash.ToUpper(), new HistoryEntry(song, flag));
        }

        public HistoryEntry GetOrAdd(string songHash, Func<string, HistoryEntry> AddValueFactory)
        {
            return SongHistory.GetOrAdd(songHash, AddValueFactory);
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
            if (SongHistory.TryGetValue(songHash, out var entry))
            {
                entry.Flag = flag;
                entry.Date = DateTime.Now;
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// If the provided song exists in history, change the flag to the one provided. Returns true if the song was found.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public bool TryUpdateFlag(ISong song, HistoryFlag flag)
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
                return false;
            songHash = songHash.ToUpper();
            return SongHistory.ContainsKey(songHash);
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
            songHash = songHash.ToUpper();
            return SongHistory.TryGetValue(songHash.ToUpper(), out value);
        }

        /// <summary>
        /// Tries to remove a hash from the dictionary.
        /// </summary>
        /// <param name="songHash"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when trying to access data before Initialize is called on HistoryManager.</exception>
        public bool TryRemove(string songHash, out HistoryEntry entry)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("HistoryManager is not initialized.");
            songHash = songHash.ToUpper();
            return SongHistory.TryRemove(songHash.ToUpper(), out entry);
        }

        public bool TryUpdateDate(string songHash, DateTime newDate)
        {
            songHash = songHash.ToUpper();
            if (SongHistory.TryGetValue(songHash, out var entry))
            {
                entry.Date = newDate;
                return true;
            }
            else
                return false;
        }

        public bool TryUpdateDate(ISong song, DateTime newDate)
        {
            if (song == null || song.Hash == null || song.Hash.Length == 0)
                return false;
            return TryUpdateDate(song.Hash, newDate);
        }


    }


}
