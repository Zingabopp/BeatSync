using System;
using WebUtilities;
using WebUtilities.DownloadContainers;

namespace BeatSyncLib.Downloader
{
    public class DownloadedContainer
        : IDisposable
    {
        public DownloadedContainer(DownloadContainer? container, DownloadResultStatus status, int httpStatus, string? reason = null, Exception? exception = null)
        {
            DownloadContainer = container;
            DownloadResult = new DownloadResult(status, httpStatus, reason, exception);
        }
        public bool IsDisposed { get; protected set; }
        public DownloadContainer? DownloadContainer { get; protected set; }
        public DownloadResult DownloadResult { get; protected set; }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (!disposedValue)
            {
                if (disposing)
                {
                    DownloadContainer?.Dispose();
                    DownloadContainer = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }

    public enum DownloadResultStatus
    {
        Unknown = 0,
        Success = 1,
        Skipped = 2,
        NetFailed = 3,
        IOFailed = 4,
        InvalidRequest = 5,
        NetNotFound = 6,
        Canceled = 7
    }
}
