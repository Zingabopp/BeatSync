using System;
using System.Collections.Generic;
using System.Text;
using BeatSyncLib.Downloader;
namespace BeatSyncLib.History
{
    public static class HistoryExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobResult"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static HistoryEntry CreateHistoryEntry(this JobResult jobResult)
        {
            if (jobResult == null)
                throw new ArgumentNullException(nameof(jobResult), $"{nameof(jobResult)} cannot be null for {nameof(CreateHistoryEntry)}");
            HistoryEntry entry = new HistoryEntry(jobResult.Song.Name, jobResult.Song.LevelAuthorName);
            if (jobResult.Successful)
                entry.Flag = HistoryFlag.Downloaded;
            else
            {
                if (jobResult.DownloadResult.Status == DownloadResultStatus.NetNotFound)
                    entry.Flag = HistoryFlag.BeatSaverNotFound;
                else
                    entry.Flag = HistoryFlag.Error;
            }
            return entry;
        }

        public static HistoryEntry ToFailedHistoryEntry(this IDownloadJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job), $"{nameof(job)} cannot be null for {nameof(ToFailedHistoryEntry)}");
            HistoryEntry entry = new HistoryEntry(job.SongName, job.LevelAuthorName);
            if (job.DownloadResult.Status == DownloadResultStatus.Success)
                throw new ArgumentException($"Calling {nameof(ToFailedHistoryEntry)} on a successful download.", nameof(job));
            else
            {
                if (job.DownloadResult.Status == DownloadResultStatus.NetNotFound)
                    entry.Flag = HistoryFlag.BeatSaverNotFound;
                else
                    entry.Flag = HistoryFlag.Error;
            }
            return entry;
        }
    }
}
