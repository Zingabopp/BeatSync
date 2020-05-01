using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncPlaylists
{
    public static class PlaylistExtensions
    {
        public static T ConvertTo<T>(this IPlaylistSong song) where T : IPlaylistSong, new()
        {
            if (song == null)
                return default(T);
            T ret = new T();
            ret.Populate(song);
            return ret;
        }

        public static void Populate(this IPlaylistSong target, ISong song)
        {
            target.Hash = song.Hash;
            target.Key = song.Key;
            if (song is IPlaylistSong playlistSong)
                target.DateAdded = playlistSong.DateAdded;
            else
                target.DateAdded = DateTime.Now;
            target.Name = song.Name;
            target.LevelAuthorName = song.LevelAuthorName;
        }
    }
}
