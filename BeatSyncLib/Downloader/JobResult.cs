using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
using SongFeedReaders.Data;
using System;
using System.Linq;

namespace BeatSyncLib.Downloader
{
    public class JobResult
    {
        public bool Successful
        {
            get
            {
                if (DownloadResult == null || TargetResults == null)
                    return false;
                if (DownloadResult.Status != DownloadResultStatus.Success)
                    return false;
                if (TargetResults.Any(r => !r.Success))
                    return false;
                return true;
            }
        }
        public JobResult() { }

        public JobState JobState { get; set; }
        public ScrapedSong Song { get; set; }
        public string HashAfterDownload { get; set; }
        public DownloadResult DownloadResult { get; set; }
        public TargetResult[] TargetResults { get; set; }
        public Exception Exception { get; set; }

        public override string ToString()
        {
            string[] targetResults = TargetResults.Select(r => r.Success ? $"{r.TargetName} successful" : $"{r.TargetName} failed").ToArray();
            return $"{Song.SongKey}, Download Status: {DownloadResult?.Status}, Target Results: {(string.Join(" | ", targetResults))}";
        }
    }

}
