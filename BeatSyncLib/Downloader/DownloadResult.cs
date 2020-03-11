using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public class DownloadResult
        : IDisposable
    {
        public DownloadResult(DownloadContainer container, DownloadResultStatus status, int httpStatus, string reason = null, Exception exception = null)
        {
            DownloadContainer = container;
            Status = status;
            HttpStatusCode = httpStatus;
            Reason = reason;
            Exception = exception;
        }
        public bool IsDisposed { get; protected set; }
        public DownloadContainer DownloadContainer { get; protected set; }
        public string Reason { get; protected set; }
        public DownloadResultStatus Status { get; protected set; }
        public int HttpStatusCode { get; protected set; }
        public Exception Exception { get; protected set; }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (!disposedValue)
            {
                if (disposing)
                {
                    DownloadContainer.Dispose();
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
        NetFailed = 2,
        IOFailed = 3,
        InvalidRequest = 4,
        NetNotFound = 5,
        Canceled = 6
    }
}
