using SongFeedReaders.Feeds;
using SongFeedReaders.Logging;
using SongFeedReaders.Models;
using SongFeedReaders.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public sealed class FeedConsumer : IFeedConsumer
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(3, 3);
        private readonly IJobFactory _jobFactory;
        private readonly ILogger? _logger;
        private readonly PauseToken _pauseToken;

        public FeedConsumer(IJobFactory jobFactory, ILogFactory? logFactory = null, PauseToken pauseToken = default(PauseToken))
        {
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
            _logger = logFactory?.GetLogger(GetType().Name);
            _pauseToken = pauseToken;
        }
        public async Task<JobResult[]> ConsumeFeed(IFeed feed, CancellationToken cancellationToken)
        {
            // read feed
            FeedResult feedResult = await feed.ReadAsync(_pauseToken, cancellationToken).ConfigureAwait(false);
            if(feedResult.Exception != null)
            {
                throw feedResult.Exception;
            }
            else if(!feedResult.Successful)
            {
                return Array.Empty<JobResult>();
            }
            // create and push job tasks to queue
            List<JobResult> jobResults = new List<JobResult>();
            foreach (ScrapedSong song in feedResult.GetSongs())
            {
                IJob job = _jobFactory.CreateJob(song, feed);
                try
                {
                    await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    JobResult jobResult = await job.RunAsync(cancellationToken).ConfigureAwait(false);
                    jobResults.Add(jobResult);
                    if(jobResult.Successful)
                    {
                        // update playlist(s)

                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            // return results
            return jobResults.ToArray();
        }
    }
}
