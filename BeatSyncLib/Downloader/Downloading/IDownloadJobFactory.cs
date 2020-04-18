using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Downloader.Downloading
{
    public interface IDownloadJobFactory
    {
        IDownloadJob CreateDownloadJob(ISong song);
    }
}
