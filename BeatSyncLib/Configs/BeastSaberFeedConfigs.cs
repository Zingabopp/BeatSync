using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSyncLib.Playlists;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeastSaber;

namespace BeatSyncLib.Configs
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
            return new BeastSaberFeedSettings((int)BeastSaberFeedName.Following)
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
            return new BeastSaberFeedSettings((int)BeastSaberFeedName.Bookmarks)
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
            return new BeastSaberFeedSettings((int)BeastSaberFeedName.CuratorRecommended)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }
}
