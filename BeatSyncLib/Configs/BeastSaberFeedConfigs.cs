using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberPlaylistsLib;
using SongFeedReaders.Feeds;
using SongFeedReaders.Feeds.BeastSaber;
using BeatSyncLib.Playlists;

namespace BeatSyncLib.Configs
{
    public class BeastSaberFollowings : PagedFeedConfigBase
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
            return new BeastSaberFollowsSettings()
            {
                MaxSongs = this.MaxSongs,
                StartingPage = this.StartingPage
            };
        }
    }

    public class BeastSaberBookmarks : PagedFeedConfigBase
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
            return new BeastSaberBookmarksSettings()
            {
                MaxSongs = this.MaxSongs,
                StartingPage = this.StartingPage
            };
        }
    }

    public class BeastSaberCuratorRecommended : PagedFeedConfigBase
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
            return new BeastSaberCuratorSettings()
            {
                StartingPage = this.StartingPage,
                MaxSongs = this.MaxSongs
            };
        }
    }
}
