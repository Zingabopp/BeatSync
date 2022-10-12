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
        private readonly List<IBeatmapFilter> _beatmapFilters = new List<IBeatmapFilter>();
        private readonly List<IBeatmapTarget> _beatmapTargets = new List<IBeatmapTarget>();

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
        public FeedConsumerBuilder WithGlobalFilters(params IBeatmapFilter[] beatmapFilters)
        {
            _beatmapFilters.AddRange(beatmapFilters);
            return this;
        }
        public FeedConsumerBuilder WithTargets(params IBeatmapTarget[] beatmapTargets)
        {
            _beatmapTargets.AddRange(beatmapTargets);
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