using BeatSyncLib.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using WebUtilities.DownloadContainers;
using static BeatSyncLib.Utilities.Util;

namespace BeatSyncLib.Downloader.Downloading
{
    public interface IDownloadJob
    {
        string FileLocation { get; }
        string SongHash { get; }
        string SongKey { get; }
        string SongName { get; }
        string LevelAuthorName { get; }
        bool CanPause { get; }
        bool SupportsProgressUpdates { get; }
        DownloadJobStatus Status { get; }
        DownloadResult DownloadResult { get; }
        Exception Exception { get; }

        event EventHandler<DownloadJobFinishedEventArgs> JobFinished;
        event EventHandler<DownloadJobStartedEventArgs> JobStarted;
        event EventHandler<DownloadJobProgressChangedEventArgs> JobProgressChanged;
        event EventHandler<DownloadJobStatusChangedEventArgs> JobStatusChanged;

        void AddDownloadFinishedCallback(DownloadFinishedCallback callback);
        void Pause();
        void Unpause();

        Task<DownloadResult> RunAsync();
        Task<DownloadResult> RunAsync(CancellationToken cancellationToken);
    }

    public delegate Task DownloadFinishedCallback(IDownloadJob job);

    public enum DownloadJobStatus
    {
        NotStarted = 0,
        Paused = 1,
        Downloading = 2,
        Finished = 3,
        Canceled = 4,
        Faulted = 5
    }

    public class DownloadJobStartedEventArgs : EventArgs
    {
        public string? SongHash { get; private set; }
        public string? SongKey { get; private set; }
        public string? SongName { get; private set; }
        public string? LevelAuthorName { get; private set; }
        public DownloadJobStartedEventArgs(string? songHash, string? songKey, string? songName, string? levelAuthorName)
        {
            SongHash = songHash;
            SongKey = songKey;
            SongName = songName;
            LevelAuthorName = levelAuthorName;
        }
    }

    public class DownloadJobFinishedEventArgs : EventArgs
    {
        public string SongHash { get; protected set; }
        public bool JobSuccessful => DownloadResult == DownloadResultStatus.Success;
        public DownloadResultStatus DownloadResult { get; protected set; }
        public DownloadContainer? DownloadContainer { get; protected set; }

        public DownloadJobFinishedEventArgs(string songHash, DownloadResult jobResult)
        {
            SongHash = songHash;
            DownloadResult = jobResult.Status;
            DownloadContainer = jobResult?.DownloadContainer;
        }

        public DownloadJobFinishedEventArgs(string songHash, DownloadResultStatus downloadResult, DownloadContainer? downloadContainer)
        {
            SongHash = songHash;
            DownloadResult = downloadResult;
            DownloadContainer = downloadContainer;
        }
    }

    public class DownloadJobProgressChangedEventArgs : EventArgs
    {
        public DownloadJobStatus DownloadJobStatus { get; protected set; }
        public ProgressValue? DownloadProgress { get; protected set; }
        public DownloadJobProgressChangedEventArgs(DownloadJobStatus downloadJobStatus)
        {
            DownloadJobStatus = downloadJobStatus;
            DownloadProgress = null;
        }
        public DownloadJobProgressChangedEventArgs(DownloadJobStatus downloadJobStatus, ProgressValue downloadProgress)
        {
            DownloadJobStatus = downloadJobStatus;
            DownloadProgress = downloadProgress; 
        }
        public DownloadJobProgressChangedEventArgs(DownloadJobStatus downloadJobStatus, long totalBytesDownloaded, long? totalFileSize)
        {
            DownloadJobStatus = downloadJobStatus;
            DownloadProgress = new ProgressValue(totalBytesDownloaded, totalFileSize);
        }
    }

    public class DownloadJobStatusChangedEventArgs : EventArgs
    {
        public DownloadJobStatus OldStatus { get; protected set; }
        public DownloadJobStatus NewStatus { get; protected set; }

        public DownloadJobStatusChangedEventArgs(DownloadJobStatus oldStatus, DownloadJobStatus newStatus)
        {
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }

    public struct DownloadProgresss
    {
        public long? TotalFileSize;
        public long TotalBytesDownloaded;
        public double? ProgressPercentage => TotalFileSize != null && TotalFileSize != 0 ? TotalBytesDownloaded / TotalFileSize : null;
        public DownloadProgresss(long totalBytesDownloaded, long? totalFileSize)
        {
            TotalBytesDownloaded = totalBytesDownloaded;
            TotalFileSize = totalFileSize;
            _stringVal = null;
        }
        private ByteUnit ByteSize => ByteUnit.Megabyte;

        private string? _stringVal;
        private string stringVal
        {
            get
            {
                if (_stringVal != null && _stringVal.Length > 0)
                    return _stringVal;
                if (TotalFileSize == null)
                    _stringVal = $"{ConvertByteValue(TotalBytesDownloaded, ByteSize).ToString("N2")} MB/?";
                else
                {
                    string progressPercentString = "";
                    if (ProgressPercentage.HasValue)
                        progressPercentString = $"({ProgressPercentage.Value.ToString("P2")})";
                    _stringVal = $"{progressPercentString}{ConvertByteValue(TotalBytesDownloaded, ByteSize).ToString("N2")} MB/{ConvertByteValue(TotalFileSize.Value, ByteSize).ToString("N2")} MB";
                }
                return _stringVal;
            }
        }
        public override string ToString()
        {
            return stringVal;
        }
    }
}
