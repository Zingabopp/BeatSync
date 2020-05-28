using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeatSync.Downloader;
using BeatSync.Playlists;
using SongFeedReaders;
using SongFeedReaders.Readers.BeatSaver;
using SongFeedReaders.Readers;
using SongFeedReaders.Data;

namespace BeatSync.Utilities
{
    public static class PlaylistSongExtensions
    {
        public static async Task<bool> UpdateSongKeyAsync(this PlaylistSong song, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(song?.Hash) || (!overwrite && !string.IsNullOrEmpty(song.Key)))
                return false;

            var result = await BeatSaverReader.GetSongByHashAsync(song.Hash, CancellationToken.None).ConfigureAwait(false);
            var scrapedSong = result?.Songs?.FirstOrDefault();
            if (scrapedSong == null)
                return false;
            song.Key = scrapedSong.SongKey;
            return true;
        }

        public static PlaylistSong ToPlaylistSong(this ScrapedSong song)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "ScrapedSong cannot be null for ToPlaylistSong()");
            return new PlaylistSong(song.Hash, song.SongName, song.SongKey, song.MapperName);
        }
    }
}
