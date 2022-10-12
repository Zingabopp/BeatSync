using System;
using System.Collections.Generic;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Filtering;
using SongFeedReaders.Logging;
using SongFeedReaders.Utilities;

namespace BeatSyncLib.Consumers
{
    public class FeedConsumerBuilder
    {
        private ILogger? _logger;
        private PauseToken _pauseToken = PauseToken.None;
        private readonly HashSet<IBeatmapFilter> _beatmapFilters = new HashSet<IBeatmapFilter>();
        private readonly HashSet<IBeatmapTarget> _beatmapTargets = new HashSet<IBeatmapTarget>();

        public FeedConsumerBuilder WithLogger(ILogger logger)
        {
            _logger = logger;
            return this;
        }
        public FeedConsumerBuilder WithPauseToken(PauseToken pauseToken)
        {
            _pauseToken = pauseToken;
            return this;
        }
        public FeedConsumerBuilder WithGlobalFilter(IBeatmapFilter beatmapFilter)
        {
            _beatmapFilters.Add(beatmapFilter);
            return this;
        }
        public FeedConsumerBuilder WithGlobalFilters(params IBeatmapFilter[] beatmapFilters)
        {
            foreach (IBeatmapFilter filter in beatmapFilters)
            {
                WithGlobalFilter(filter);
            }
            return this;
        }
        public FeedConsumerBuilder WithTarget(IBeatmapTarget beatmapTarget)
        {
            _beatmapTargets.Add(beatmapTarget);
            return this;
        }
        public FeedConsumerBuilder WithTargets(params IBeatmapTarget[] beatmapTargets)
        {
            foreach (IBeatmapTarget target in beatmapTargets)
            {
                WithTarget(target);
            }
            return this;
        }
        public FeedConsumer Build()
        {
            if (_beatmapTargets.Count == 0)
            {
                throw new InvalidOperationException("FeedConsumer must be configured with at least one target");
            }
            return new FeedConsumer(_beatmapTargets, _beatmapFilters, _logger, _pauseToken);
        }
    }
}