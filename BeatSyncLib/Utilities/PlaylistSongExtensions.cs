using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberPlaylistsLib.Types;
using SongFeedReaders;
using SongFeedReaders.Data;

namespace BeatSyncLib.Utilities
{
    public static class PlaylistSongExtensions
    {
        public static async Task<bool> UpdateSongKeyAsync(this IPlaylistSong song, bool overwrite = false)
        {
            if (song?.Hash == null || string.IsNullOrEmpty(song.Hash) || (!overwrite && !string.IsNullOrEmpty(song.Key)))
                return false;

            var result = await WebUtils.SongInfoManager.GetSongByHashAsync(song.Hash, CancellationToken.None).ConfigureAwait(false);
            var scrapedSong = result.Song;
            if (scrapedSong == null)
                return false;
            song.Key = scrapedSong.Key;
            return true;
        }
        public static IPlaylistSong? Add<T, TSong>(this T playlist, SongFeedReaders.Data.ISong song) 
            where T : IPlaylist<TSong>, new()
            where TSong : IPlaylistSong, new()
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "ScrapedSong cannot be null for ToPlaylistSong()");
            TSong pSOng = new TSong() { Hash = song.Hash, Name = song.Name, Key = song.Key, LevelAuthorName = song.LevelAuthorName };
            return playlist.Add(pSOng);
        }
        public static IPlaylistSong ToPlaylistSong<T>(this SongFeedReaders.Data.ISong song) where T : IPlaylistSong, new()
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "ScrapedSong cannot be null for ToPlaylistSong()");

            return new T() { Hash = song.Hash, Name = song.Name, Key = song.Key, LevelAuthorName = song.LevelAuthorName };
        }
        public static IPlaylistSong ToPlaylistSong<T>(this BeatSaberPlaylistsLib.Types.ISong song) where T : IPlaylistSong, new()
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "ScrapedSong cannot be null for ToPlaylistSong()");

            return new T() { Hash = song.Hash, Name = song.Name, Key = song.Key, LevelAuthorName = song.LevelAuthorName };
        }
    }
}
