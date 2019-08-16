using System;

namespace BeatSync.Configs
{
    public class BeatSaverFavoriteMappers : FeedConfigBase
    {
        public BeatSaverFavoriteMappers()
        {
            FeedPlaylist = Playlists.BuiltInPlaylist.BeatSaverFavoriteMappers;
        }
        public bool SeparateMapperPlaylists { get; set; }

        public SongFeedReaders.BeatSaverFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.BeatSaverFeedSettings((int)SongFeedReaders.BeatSaverFeed.Author)
            {
                Authors = FavoriteMappers.Mappers,
                MaxSongs = this.MaxSongs
            };
        }
    }

    public class BeatSaverLatest : FeedConfigBase
    {
        public BeatSaverLatest()
        {
            FeedPlaylist = Playlists.BuiltInPlaylist.BeatSaverLatest;
        }
        public SongFeedReaders.BeatSaverFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.BeatSaverFeedSettings((int)SongFeedReaders.BeatSaverFeed.Latest)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }

    public class BeatSaverHot : FeedConfigBase
    {
        public BeatSaverHot()
        {
            FeedPlaylist = Playlists.BuiltInPlaylist.BeatSaverHot;
        }
        public SongFeedReaders.BeatSaverFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.BeatSaverFeedSettings((int)SongFeedReaders.BeatSaverFeed.Hot)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }

    [Obsolete("Only has really old play data.")]
    public class BeatSaverPlays : FeedConfigBase
    {
        public BeatSaverPlays()
        {
            FeedPlaylist = Playlists.BuiltInPlaylist.BeatSaverPlays;
        }
        public SongFeedReaders.BeatSaverFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.BeatSaverFeedSettings((int)SongFeedReaders.BeatSaverFeed.Plays)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }

    public class BeatSaverDownloads : FeedConfigBase
    {
        public BeatSaverDownloads()
        {
            FeedPlaylist = Playlists.BuiltInPlaylist.BeatSaverDownloads;
        }
        public SongFeedReaders.BeatSaverFeedSettings ToFeedSettings()
        {
            return new SongFeedReaders.BeatSaverFeedSettings((int)SongFeedReaders.BeatSaverFeed.Downloads)
            {
                MaxSongs = this.MaxSongs
            };
        }
    }


}
