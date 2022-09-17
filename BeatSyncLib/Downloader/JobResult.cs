using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Targets;
using SongFeedReaders.Models;
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
                if (DownloadResult.Successful == false || TargetResults == null)
                    return false;
                if (DownloadResult.Status != DownloadResultStatus.Success
                    && DownloadResult.Status != DownloadResultStatus.Skipped)
                    return false;
                if (TargetResults.Any(r => !r.Success))
                    return false;
                return true;
            }
        }
        public JobResult() { }

        public JobState JobState { get; set; }
        public ISong? Song { get; set; }
        public string? HashAfterDownload { get; set; }
        public DownloadResult DownloadResult { get; set; }
        public TargetResult[]? TargetResults { get; set; }
        public Exception? Exception { get; set; }

        public override string ToString()
        {
            string[]? targetResults = TargetResults?.Select(r => r.Success ? $"{r.Target.TargetName} successful" : $"{r.Target.TargetName} failed").ToArray();
            return $"{Song?.Key}, Download Status: {DownloadResult.Status}, Target Results: {(targetResults != null ? string.Join(" | ", targetResults) : "<None>")}";
        }
    }
}
