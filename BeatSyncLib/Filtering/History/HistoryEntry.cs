using System;
using BeatSyncLib.Configs.Converters;
using Newtonsoft.Json;
using SongFeedReaders.Models;

namespace BeatSyncLib.Filtering.History
{
    public class HistoryEntry
    {
        public HistoryEntry() { }
        public HistoryEntry(string? songInfo, HistoryFlag flag = 0)
        {
            SongInfo = songInfo;
            Date = DateTime.Now;
            Flag = flag;
        }
        public HistoryEntry(string? songName, string? mapper, HistoryFlag flag = 0)
        {
            if (songName != null && songName.Length > 0)
            {
                if (mapper != null && mapper.Length > 0)
                    SongInfo = $"{songName} by {mapper}";
                else
                    SongInfo = $"{songName}";
            }
            Date = DateTime.Now;
            Flag = flag;
        }

        public HistoryEntry(ISong song, HistoryFlag flag = 0)
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
        public string? SongInfo { get; set; }
        [JsonProperty("Flag")]
        [JsonConverter(typeof(HistoryFlagConverter))]
        public HistoryFlag Flag { get; set; }
        [JsonProperty("Date")]
        public DateTime Date { get; set; }

        [JsonIgnore]
        public virtual bool AllowRetry
        {
            get
            {
                return Flag switch
                {
                    HistoryFlag.None => true,
                    HistoryFlag.Downloaded => false,
                    HistoryFlag.Deleted => false,
                    HistoryFlag.Missing => false,
                    HistoryFlag.PreExisting => false,
                    HistoryFlag.Error => true,
                    HistoryFlag.BeatSaverNotFound => true,
                    _ => true
                };
            }
        }

        public virtual HistoryEntry UpdateFrom(HistoryEntry source)
        {
            if (source.SongInfo != null && source.SongInfo.Length > 0)
                SongInfo = source.SongInfo;
            Flag = source.Flag;
            if (source.Date != DateTime.MinValue)
                Date = source.Date;
            return this;
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
