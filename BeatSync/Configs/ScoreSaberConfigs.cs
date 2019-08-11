using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Configs
{
    public class ScoreSaberTrending : FeedConfigBase
    {

        public bool RankedOnly { get; set; }

        public SongFeedReaders.ScoreSaberFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.ScoreSaberFeedSettings((int)SongFeedReaders.ScoreSaberFeed.Trending)
            {
                MaxSongs = this.MaxSongs,
                SongsPerPage = this.MaxSongs,
                RankedOnly = this.RankedOnly
            };
        }
    }

    public class ScoreSaberLatestRanked : FeedConfigBase
    {
        public SongFeedReaders.ScoreSaberFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.ScoreSaberFeedSettings((int)SongFeedReaders.ScoreSaberFeed.LatestRanked)
            {
                MaxSongs = this.MaxSongs,
                SongsPerPage = this.MaxSongs,
            };
        }
    }

    public class ScoreSaberTopPlayed : FeedConfigBase
    {
        public bool RankedOnly { get; set; }

        public SongFeedReaders.ScoreSaberFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.ScoreSaberFeedSettings((int)SongFeedReaders.ScoreSaberFeed.TopPlayed)
            {
                MaxSongs = this.MaxSongs,
                SongsPerPage = this.MaxSongs,
                RankedOnly = this.RankedOnly
            };
        }
    }

    public class ScoreSaberTopRanked : FeedConfigBase
    {
        public SongFeedReaders.ScoreSaberFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.ScoreSaberFeedSettings((int)SongFeedReaders.ScoreSaberFeed.TopRanked)
            {
                MaxSongs = this.MaxSongs,
                SongsPerPage = this.MaxSongs,
            };
        }
    }

}
