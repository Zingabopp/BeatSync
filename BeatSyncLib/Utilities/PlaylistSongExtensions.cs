using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatSyncLib.Downloader;
using BeatSyncLib.Playlists;
using SongFeedReaders;
using SongFeedReaders.Readers.BeatSaver;
using SongFeedReaders.Readers;
using SongFeedReaders.Data;

namespace BeatSyncLib.Utilities
{
    public static class PlaylistSongExtensions
    {
        public static async Task<bool> UpdateSongKeyAsync(this IPlaylistSong song, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(song?.Hash) || (!overwrite && !string.IsNullOrEmpty(song.Key)))
                return false;

            var result = await BeatSaverReader.GetSongByHashAsync(song.Hash, CancellationToken.None).ConfigureAwait(false);
            var scrapedSong = result?.Songs?.FirstOrDefault();
            if (scrapedSong == null)
                return false;
            song.Key = scrapedSong.Key;
            return true;
        }

        public static IPlaylistSong ToPlaylistSong<T>(this ISong song) where T : IPlaylistSong, new()
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "ScrapedSong cannot be null for ToPlaylistSong()");

            return new T() { Hash = song.Hash, Name = song.Name, Key = song.Key, LevelAuthorName = song.LevelAuthorName };
        }

        public static IFeedSong ToFeedSong<T>(this ISong song) where T : IFeedSong, new()
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "ScrapedSong cannot be null for ToPlaylistSong()");

            return new T() { Hash = song.Hash, Name = song.Name, Key = song.Key, LevelAuthorName = song.LevelAuthorName };
        }
    }
}
