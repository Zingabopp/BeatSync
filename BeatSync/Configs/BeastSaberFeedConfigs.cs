using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSync.Playlists;
using SongFeedReaders.Readers;

namespace BeatSync.Configs
{
    public class BeastSaberFollowings : FeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => false;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.BeastSaberFollows;
        #endregion

        public override IFeedSettings ToFeedSettings()
        {
            return new BeastSaberFeedSettings((int)BeastSaberFeed.Following)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }

    public class BeastSaberBookmarks : FeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => false;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.BeastSaberBookmarks;
        #endregion

        public override IFeedSettings ToFeedSettings()
        {
            return new BeastSaberFeedSettings((int)BeastSaberFeed.Bookmarks)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }

    public class BeastSaberCuratorRecommended : FeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => false;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.BeastSaberCurator;
        #endregion

        public override IFeedSettings ToFeedSettings()
        {
            return new BeastSaberFeedSettings((int)BeastSaberFeed.CuratorRecommended)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }
}
