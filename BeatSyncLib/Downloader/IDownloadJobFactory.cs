using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Downloader
{
    public interface IDownloadJobFactory
    {
        IDownloadJob CreateDownloadJob(ScrapedSong song);
    }
}
