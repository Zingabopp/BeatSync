using BeatSync.Playlists;
using SongFeedReaders;
using System;

namespace BeatSync.Configs
{
    public class BeatSaverFavoriteMappers : FeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => true;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.BeatSaverFavoriteMappers;
        #endregion
        //public bool SeparateMapperPlaylists { get; set; }

        public override void FillDefaults()
        {
            base.FillDefaults();
            // var _ = SeparateMapperPlaylists;
        }

        public override IFeedSettings ToFeedSettings()
        {
            return new BeatSaverFeedSettings((int)BeatSaverFeed.Author)
            {
                Authors = FavoriteMappers.Mappers,
                MaxSongs = this.MaxSongs
            };
        }
    }

    public class BeatSaverLatest : FeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => false;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.BeatSaverLatest;
        #endregion

        public override IFeedSettings ToFeedSettings()
        {
            return new BeatSaverFeedSettings((int)BeatSaverFeed.Latest)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }

    public class BeatSaverHot : FeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => false;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.BeatSaverHot;
        #endregion

        public override  IFeedSettings ToFeedSettings()
        {
            return new BeatSaverFeedSettings((int)BeatSaverFeed.Hot)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }

    [Obsolete("Only has really old play data.")]
    public class BeatSaverPlays : FeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => false;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.BeatSaverPlays;
        #endregion

        public override IFeedSettings ToFeedSettings()
        {
            return new BeatSaverFeedSettings((int)BeatSaverFeed.Plays)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }

    public class BeatSaverDownloads : FeedConfigBase
    {
        #region Defaults
        protected override bool DefaultEnabled => false;

        protected override int DefaultMaxSongs => 20;

        protected override bool DefaultCreatePlaylist => true;

        protected override PlaylistStyle DefaultPlaylistStyle => PlaylistStyle.Append;

        protected override BuiltInPlaylist DefaultFeedPlaylist => BuiltInPlaylist.BeatSaverDownloads;
        #endregion

        public override IFeedSettings ToFeedSettings()
        {
            return new BeatSaverFeedSettings((int)BeatSaverFeed.Downloads)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }


}
