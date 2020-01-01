using BeatSyncLib.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public interface IDownloadJob
    {
        string FileLocation { get; }
        string SongHash { get; }
        string SongKey { get; }
        string SongName { get; }
        string LevelAuthorName { get; }
        DownloadJobStatus Status { get; }
        DownloadManager DownloadManager { get; set; }
        Exception Exception { get; }

        event EventHandler<DownloadJobFinishedEventArgs> OnJobFinished;
        event EventHandler<DownloadJobStartedEventArgs> OnJobStarted;

        Task RunAsync();
        Task RunAsync(CancellationToken cancellationToken);

    }

    public enum DownloadJobStatus
    {
        NotStarted = 0,
        Downloading = 1,
        Finished = 2,
        Canceled = 3,
        Faulted = 4
    }

    public class DownloadJobStartedEventArgs : EventArgs
    {
        public string SongHash { get; private set; }
        public string SongKey { get; private set; }
        public string SongName { get; private set; }
        public string LevelAuthorName { get; private set; }
        public DownloadJobStartedEventArgs(string songHash, string songKey, string songName, string levelAuthorName)
        {
            SongHash = songHash;
            SongKey = songKey;
            SongName = SongName;
            LevelAuthorName = LevelAuthorName;
        }
    }

    public class DownloadJobFinishedEventArgs : EventArgs
    {
        public string SongHash { get; private set; }
        public bool JobSuccessful => DownloadResult == DownloadResultStatus.Success;
        public DownloadResultStatus DownloadResult { get; private set; }
        public string FileLocation { get; private set; }

        public DownloadJobFinishedEventArgs(string songHash, DownloadResult jobResult)
        {
            SongHash = songHash;
            DownloadResult = jobResult.Status;
            FileLocation = jobResult.FilePath;
        }

        public DownloadJobFinishedEventArgs(string songHash, DownloadResultStatus downloadResult, string fileLocation)
        {
            SongHash = songHash;
            DownloadResult = downloadResult;
            FileLocation = fileLocation;
        }
    }
}
