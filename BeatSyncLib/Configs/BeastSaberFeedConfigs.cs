using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberPlaylistsLib;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeastSaber;
using BeatSyncLib.Playlists;

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
                MaxSongs = this.MaxSongs,
                StartingPage = this.StartingPage
            };
        }
    }

    public class BeastSaberBookmarks : FeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => true;

        protected override int DefaultMaxSongs => 0;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.BeastSaberBookmarks;
        #endregion

        public override IFeedSettings ToFeedSettings()
        {
            return new BeastSaberFeedSettings((int)BeastSaberFeedName.Bookmarks)
            {
                MaxSongs = this.MaxSongs,
                StartingPage = this.StartingPage
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
                StartingPage = this.StartingPage,
                MaxSongs = this.MaxSongs
            };
        }
    }
}
