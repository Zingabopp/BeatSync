using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Text;
using WebUtilities;

namespace BeatSyncLib.Downloader
{
    public class DownloadJobFactory : IDownloadJobFactory
    {
        protected Func<ScrapedSong, DownloadContainer> ContainerFactory = null;
        public DownloadJobFactory(Func<ScrapedSong, DownloadContainer> containerFactory)
        {
            ContainerFactory = containerFactory ?? throw new ArgumentNullException(nameof(containerFactory));
        }

        public IDownloadJob CreateDownloadJob(ScrapedSong song)
        {
            DownloadContainer downloadContainer = ContainerFactory(song);
            return new DownloadJob(song, downloadContainer);
        }

    }
}
