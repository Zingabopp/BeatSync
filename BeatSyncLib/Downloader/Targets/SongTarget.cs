using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader.Targets
{
    public abstract class SongTarget
    {
        public abstract string TargetName { get; }
        public int DestinationId { get; }
        public bool TransferComplete { get; protected set; }
        public TargetResult TargetResult { get; protected set; }

        protected SongTarget(int destinationId)
        {
            DestinationId = destinationId;
        }

        public abstract Task<bool> CheckSongExistsAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<TargetResult> TransferAsync(Stream sourceStream, CancellationToken cancellationToken);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <returns></returns>
        public Task<TargetResult> TransferAsync(Stream sourceStream) => TransferAsync(sourceStream, CancellationToken.None);
    }

    public class TargetResult
    {
        public bool Success { get; protected set; }
        public SongTarget Target { get; }
        public Exception Exception { get; protected set; }
        public TargetResult(SongTarget target, bool success, Exception exception)
        {
            Target = target;
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
