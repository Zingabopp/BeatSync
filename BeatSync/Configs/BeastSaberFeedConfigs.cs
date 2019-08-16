using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Configs
{
    public class BeastSaberFollowings : FeedConfigBase
    {
        public BeastSaberFollowings()
        {
            FeedPlaylist = Playlists.BuiltInPlaylist.BeastSaberFollows;
        }

        public SongFeedReaders.BeastSaberFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.BeastSaberFeedSettings((int)SongFeedReaders.BeastSaberFeed.Following)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }

    public class BeastSaberBookmarks : FeedConfigBase
    {
        public BeastSaberBookmarks()
        {
            FeedPlaylist = Playlists.BuiltInPlaylist.BeastSaberBookmarks;
        }
        public SongFeedReaders.BeastSaberFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.BeastSaberFeedSettings((int)SongFeedReaders.BeastSaberFeed.Bookmarks)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }

    public class BeastSaberCuratorRecommended : FeedConfigBase
    {
        public BeastSaberCuratorRecommended()
        {
            FeedPlaylist = Playlists.BuiltInPlaylist.BeastSaberCurator;
        }
        public SongFeedReaders.BeastSaberFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.BeastSaberFeedSettings((int)SongFeedReaders.BeastSaberFeed.CuratorRecommended)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }
}
