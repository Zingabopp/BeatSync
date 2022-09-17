using BeatSyncLib.Configs;
using SongFeedReaders.Logging;
using SongFeedReaders.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader.Targets
{
    public abstract class BeatmapTarget : IBeatmapsTarget
    {
        protected readonly ILogger? Logger;
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

        protected BeatmapTarget(ILogFactory? logFactory)
        {
            DestinationId = GetNextDestinationId();
            Logger = logFactory?.GetLogger(GetType().Name);
        }

        public abstract Task<TargetResult> TransferAsync(ISong song, Stream sourceStream, CancellationToken cancellationToken);
        public abstract Task<BeatmapState> GetTargetSongStateAsync(string songHash, CancellationToken cancellationToken);
        public abstract Task OnFeedJobsFinished(IEnumerable<JobResult> jobResults, BeatSyncConfig beatSyncConfig, FeedConfigBase? feedConfig, CancellationToken cancellationToken);
    }
}
