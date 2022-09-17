using SongFeedReaders.Feeds;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public interface IFeedConsumer
    {
        Task<JobResult[]> ConsumeFeed(IFeed feed, CancellationToken cancellationToken);
    }
}
