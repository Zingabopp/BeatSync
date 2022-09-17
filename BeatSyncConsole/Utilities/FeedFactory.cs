using Microsoft.Extensions.DependencyInjection;
using SongFeedReaders.Feeds;
using SongFeedReaders.Services;
using System;

namespace BeatSyncConsole.Utilities
{
    public class FeedFactory : FeedFactoryBase
    {
        private readonly IServiceProvider _serviceProvider;
        public FeedFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override IFeed? InstantiateFeed(Type feedType)
        {
            return _serviceProvider.GetRequiredService(feedType) as IFeed;
        }
    }
}
