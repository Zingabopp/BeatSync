using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader.Targets
{
    public interface ISongTarget
    {
        string TargetName { get; }
        bool TransferComplete { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TargetResult> TransferAsync(Stream sourceStream, CancellationToken cancellationToken);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <returns></returns>
        Task<TargetResult> TransferAsync(Stream sourceStream);
    }

    public class TargetResult
    {
        public string TargetName { get; }
        public bool Success { get; protected set; }
        public Exception Exception { get; protected set; }
        public TargetResult(string targetName, bool success, Exception exception)
        {
            TargetName = targetName;
            Success = success;
            Exception = exception;
        }
    }

    public class SongTargetTransferException : Exception
    {
        public SongTargetTransferException()
            : base("An error occurred transferring the download to the target.")
        { }

        public SongTargetTransferException(string message)
            : base(message)
        { }

        public SongTargetTransferException(string message, Exception inner)
            : base(message, inner)
        { }
    }
    public enum TargetSource
    {
        None = 0,
        Zip = 1 << 0,
        Directory = 1 << 1
    }
}
