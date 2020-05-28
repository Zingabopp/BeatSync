using BeatSyncPlaylists;
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
        private static object _idLock = new object();
        private static int _nextDestinationId = 0;
        protected static int GetNextDestinationId()
        {
            int nextId = 0;
            lock (_idLock)
            {
                nextId = _nextDestinationId;
                _nextDestinationId++;
            }
            return nextId;
        }
        public abstract string TargetName { get; }
        public int DestinationId { get; }
        public TargetResult? TargetResult { get; protected set; }

        protected SongTarget()
        {
            DestinationId = GetNextDestinationId();
        }

        public abstract Task<SongState> CheckSongExistsAsync(string songHash);

        public virtual Task<SongState> CheckSongExistsAsync(ISong song) => CheckSongExistsAsync(song.Hash);

        /// <summary>
        /// Transfers a song zip file to a target. All exceptions are caught and returned in <see cref="TargetResult.Exception"/>.
        /// </summary>
        /// <param name="song">Metadata for the song.</param>
        /// <param name="sourceStream">Stream of the zip file containing the song.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<TargetResult> TransferAsync(ISong song, Stream sourceStream, CancellationToken cancellationToken);
        /// <summary>
        /// Transfers a song zip file to a target. All exceptions are caught and returned in <see cref="TargetResult.Exception"/>.
        /// </summary>
        /// <param name="song">Metadata for the song.</param>
        /// <param name="sourceStream">Stream of the zip file containing the song.</param>
        /// <returns></returns>
        public Task<TargetResult> TransferAsync(ISong song, Stream sourceStream) => TransferAsync(song, sourceStream, CancellationToken.None);
    }

    public enum SongState
    {
        Wanted = 0,
        Exists = 1,
        NotWanted = 2
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
