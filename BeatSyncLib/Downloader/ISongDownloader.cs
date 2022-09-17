using SongFeedReaders.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public interface ISongDownloader
    {
        /// <summary>
        /// Attempts to download the given song
        /// </summary>
        /// <param name="song"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        Task<DownloadedContainer> DownloadSongAsync(ISong song, CancellationToken cancellationToken);
    }
}
