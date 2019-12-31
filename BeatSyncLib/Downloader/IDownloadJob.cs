using BeatSyncLib.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public delegate void JobFinishedEvent(IDownloadJob sender, JobResult result);

    public interface IDownloadJob
    {
        string SongHash { get; }
        string SongKey { get; }
        string SongName { get; }
        string LevelAuthorName { get; }
        string SongDirectory { get; }
        Exception Exception { get; }
        JobResult Result { get; }
        JobStatus Status { get; }
        event EventHandler<JobFinishedEventArgs> OnJobFinished;
        event EventHandler<JobStartedEventArgs> OnJobStarted;

        Task RunAsync();
        Task RunAsync(CancellationToken cancellationToken);

    }

    public enum JobStatus
    {
        NotStarted = 0,
        Downloading = 1,
        Extracting = 2,
        Finished = 3,
        Canceled = 4,
        Faulted = 5
    }

    public class JobStartedEventArgs : EventArgs
    {
        public string SongHash { get; private set; }
        public string SongKey { get; private set; }
        public string SongName { get; private set; }
        public string LevelAuthorName { get; private set; }
        public JobStartedEventArgs(string songHash, string songKey, string songName, string levelAuthorName)
        {
            SongHash = songHash;
            SongKey = songKey;
            SongName = SongName;
            LevelAuthorName = LevelAuthorName;
        }
    }

    public class JobFinishedEventArgs : EventArgs
    {
        public string SongHash { get; private set; }
        public bool JobSuccessful { get; private set; }
        public DownloadResultStatus DownloadResult { get; private set; }
        public ZipExtractResultStatus ZipExtractResult { get; private set; }
        public string SongDirectory { get; private set; }

        public JobFinishedEventArgs(JobResult jobResult)
        {
            SongHash = jobResult.SongHash;
            DownloadResult = jobResult.DownloadResult.Status;
            ZipExtractResult = jobResult.ZipResult.ResultStatus;
            SongDirectory = jobResult.SongDirectory;
            JobSuccessful = jobResult.Successful;
        }

        public JobFinishedEventArgs(string songHash, bool successful, DownloadResultStatus downloadResult, ZipExtractResultStatus zipResult, string songDir)
        {
            SongHash = songHash;
            JobSuccessful = successful;
            DownloadResult = downloadResult;
            ZipExtractResult = zipResult;
            SongDirectory = songDir;
        }
    }
}
