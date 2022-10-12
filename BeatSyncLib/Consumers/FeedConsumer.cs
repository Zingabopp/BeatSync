using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberPlaylistsLib.Types;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Filtering;
using BeatSyncLib.Utilities;
using SongFeedReaders.Feeds;
using SongFeedReaders.Logging;
using SongFeedReaders.Utilities;

namespace BeatSyncLib.Consumers
{
    public sealed class FeedConsumer
    {
        private readonly PauseToken _pauseToken;
        private readonly IBeatmapFilter[] _globalFilters;
        private readonly IBeatmapTarget[] _targets;
        private readonly ILogger? _logger;
        public FeedConsumer(IEnumerable<IBeatmapTarget> beatmapTargets,
            IEnumerable<IBeatmapFilter> globalFilters, ILogger? logger,
            PauseToken pauseToken)
        {
            _targets = beatmapTargets.ToArray();
            _globalFilters = globalFilters.ToArray();
            _logger = logger;
            _pauseToken = pauseToken;
        }

        public async Task<FeedConsumerResult> ConsumeFeed(IFeed feed, CancellationToken cancellationToken)
        {
            IEnumerable<TargetResult>? targetResults = null;
            try
            {
                if (!feed.HasValidSettings || !(feed.GetFeedSettings() is { } feedSettings))
                {
                    throw new ArgumentException("Feed must have valid settings", nameof(feed));
                }

                await feed.InitializeAsync(cancellationToken).ConfigureAwait(false);
                FeedResult feedResult = await feed.ReadAsync(_pauseToken, cancellationToken).ConfigureAwait(false);
                Task<TargetResult>[] targetTasks =
                    _targets.Select(t => t.ProcessFeedResult(feedResult, _pauseToken, cancellationToken))
                        .ToArray();

                await Task.WhenAll(targetTasks).ConfigureAwait(false);

                targetResults = targetTasks.Select(t => t.Result);

                FeedConsumerResult result = new FeedConsumerResult(targetResults);
                return result;
            }
            catch (Exception ex)
            {
                return new FeedConsumerResult(targetResults ?? Array.Empty<TargetResult>(), ex);
            }

        }
    }
}