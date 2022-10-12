using System;
using System.Collections.Generic;
using System.Linq;
using BeatSyncLib.Downloader.Targets;

namespace BeatSyncLib.Consumers
{
    public class FeedConsumerResult
    {
        public IEnumerable<TargetResult> TargetResults { get; }
        public Exception? Exception { get; }

        public FeedConsumerResult(IEnumerable<TargetResult> targetResults, Exception? exception = null)
        {
            TargetResults = targetResults.ToArray();
            Exception = exception;
        }
    }
}