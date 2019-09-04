using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSync.Playlists;
using SongFeedReaders;

namespace BeatSync.Utilities
{
    public static class PlaylistSongExtensions
    {
        public static async Task<bool> UpdateSongKeyAsync(this PlaylistSong song, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(song?.Hash) || (!overwrite && !string.IsNullOrEmpty(song.Key)))
                return false;

            var scrape = await BeatSaverReader.GetSongByHashAsync(song.Hash).ConfigureAwait(false);
            if (scrape == null)
                return false;
            song.Key = scrape.SongKey;
            return true;
        }

        public static PlaylistSong ToPlaylistSong(this ScrapedSong song)
        {
            return new PlaylistSong(song.Hash, song.SongName, song.SongKey, song.MapperName);
        }
    }
}
