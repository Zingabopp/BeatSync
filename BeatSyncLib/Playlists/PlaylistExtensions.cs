using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Playlists
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

        public static void Populate(this IPlaylistSong target, IPlaylistSong song)
        {
            target.Hash = song.Hash;
            target.Key = song.Key;
            target.DateAdded = song.DateAdded;
            target.Name = song.Name;
            target.LevelAuthorName = song.LevelAuthorName;
        }
    }
}
