using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Text;
using WebUtilities;

namespace BeatSyncLib.Downloader
{
    public class DownloadJobFactory : IDownloadJobFactory
    {
        protected Func<ISong, DownloadContainer> ContainerFactory = null;
        public DownloadJobFactory(Func<ISong, DownloadContainer> containerFactory)
        {
            ContainerFactory = containerFactory ?? throw new ArgumentNullException(nameof(containerFactory));
        }

        public IDownloadJob CreateDownloadJob(ISong song)
        {
            DownloadContainer downloadContainer = ContainerFactory(song);
            return new DownloadJob(song, downloadContainer);
        }

    }
}
