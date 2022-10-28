using BeatSyncLib.Playlists;
using Newtonsoft.Json;
using SongFeedReaders.Readers;
using SongFeedReaders.Readers.BeastSaber;

namespace BeatSyncLib.Configs
{
    public abstract class BeastSaberConfigBase : PagedFeedConfigBase
    {

        protected virtual int DefaultSongsPerPage => 50;
        [JsonIgnore]
        private int? _songsPerPage;
        public int SongsPerPage
        {
            get
            {
                if (_songsPerPage == null || _songsPerPage <= 0)
                {
                    _songsPerPage = DefaultSongsPerPage;
                    SetConfigChanged();
                }
                return _songsPerPage ??= DefaultSongsPerPage;
            }
            set
            {
                if (_songsPerPage == value)
                {
                    return;
                }

                _songsPerPage = value;
                SetConfigChanged();
            }
        }

        public override void FillDefaults()
        {
            base.FillDefaults();
            _ = SongsPerPage;
        }
    }

    public class BeastSaberFollowings : BeastSaberConfigBase
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
                StartingPage = this.StartingPage,
                SongsPerPage = this.SongsPerPage
            };
        }
    }

    public class BeastSaberBookmarks : BeastSaberConfigBase
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
                StartingPage = this.StartingPage,
                SongsPerPage = this.SongsPerPage
            };
        }
    }

    public class BeastSaberCuratorRecommended : BeastSaberConfigBase
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
                MaxSongs = this.MaxSongs,
                SongsPerPage = this.SongsPerPage
            };
        }
    }
}
