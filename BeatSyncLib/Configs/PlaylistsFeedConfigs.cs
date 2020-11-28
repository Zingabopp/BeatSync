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
    public class PlaylistFeeds : FeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => false;

        protected override int DefaultMaxSongs => 0;

        protected override bool DefaultCreatePlaylist => false;

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
}
