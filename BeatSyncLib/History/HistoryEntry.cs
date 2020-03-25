using BeatSyncLib.Playlists;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.History
{
    public class HistoryEntry
    {
        public HistoryEntry() { }
        public HistoryEntry(string songInfo, HistoryFlag flag = 0)
        {
            SongInfo = songInfo;
            Date = DateTime.Now;
        }
        public HistoryEntry(string songName, string mapper, HistoryFlag flag = 0)
            : this($"{songName} by {mapper}", flag)
        {
            //Hash = hash;
            //SongName = songName;
            //Mapper = mapper;
        }

        public HistoryEntry(IPlaylistSong song, HistoryFlag flag = 0)
        {
            //Hash = song.Hash;
            //SongName = song.Name;
            //Mapper = song.LevelAuthorName;
            if (!string.IsNullOrEmpty(song.Key))
                SongInfo = $"({song.Key}) {song.Name} by {song.LevelAuthorName}";
            else
                SongInfo = $"{song.Name} by {song.LevelAuthorName}";
            Flag = flag;
            Date = DateTime.Now;
        }
        [JsonProperty("SongInfo")]
        public string SongInfo { get; set; }
        [JsonProperty("Flag")]
        public HistoryFlag Flag { get; set; }
        [JsonProperty("Date")]
        public DateTime Date { get; set; }

        [JsonIgnore]
        public virtual bool AllowRetry
        {
            get
            {
                if (Flag == HistoryFlag.Downloaded
                    || Flag == HistoryFlag.Deleted
                    || Flag == HistoryFlag.Missing
                    || Flag == HistoryFlag.BeatSaverNotFound)
                    return false;
                return true;
            }
        }
    }

    public enum HistoryFlag
    {
        /// <summary>
        /// Not set, should mean the song is in the download queue or is in progress.
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
        /// Error during download/extractions.
        /// </summary>
        Error = 5,
        /// <summary>
        /// Not found on Beat Saver.
        /// </summary>
        BeatSaverNotFound = 404

    }
}
