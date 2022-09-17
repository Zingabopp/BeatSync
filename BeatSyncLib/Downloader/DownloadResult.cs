using System;

namespace BeatSyncLib.Downloader
{
    public struct DownloadResult
    {
        public DownloadResult(DownloadResultStatus status, int httpStatus, string? reason = null, Exception? exception = null)
        {
            Status = status;
            HttpStatusCode = httpStatus;
            Reason = reason;
            Exception = exception;
        }
        public bool Successful => Status == DownloadResultStatus.Success;
        public string? Reason { get; private set; }
        public DownloadResultStatus Status { get; private set; }
        public int HttpStatusCode { get; private set; }
        public Exception? Exception { get; private set; }
    }
}
