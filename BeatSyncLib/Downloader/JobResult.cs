using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
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
        public string SongDirectory { get; set; }
        public string SongHash { get; set; }
        public string SongKey { get; set; }
        public string HashAfterDownload { get; set; }
        public DownloadResult DownloadResult { get; set; }
        public TargetResult[] TargetResults { get; set; }
        public Exception Exception { get; set; }

        public override string ToString()
        {
            string[] targetResults = TargetResults.Select(r => r.Success ? $"{r.TargetName} successful" : $"{r.TargetName} failed").ToArray();
            return $"{SongKey}, Download Status: {DownloadResult?.Status}, Target Results: {(string.Join(" | ", targetResults))}";
        }
    }

}
