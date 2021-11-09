using BeatSyncLib.Utilities;
using SongFeedReaders.Models;
using System;
using System.Collections.Generic;
using System.Text;
using WebUtilities;
using WebUtilities.DownloadContainers;

namespace BeatSyncLib.Downloader.Downloading
{
    public class DownloadJobFactory : IDownloadJobFactory
    {
        protected readonly Func<ISong, DownloadContainer> ContainerFactory;
        protected readonly FileIO FileIO;
        public DownloadJobFactory(Func<ISong, DownloadContainer> containerFactory, FileIO fileIO)
        {
            ContainerFactory = containerFactory ?? throw new ArgumentNullException(nameof(containerFactory));
            FileIO = fileIO ?? throw new ArgumentNullException(nameof(fileIO));
        }

        public IDownloadJob CreateDownloadJob(ISong song)
        {
            DownloadContainer downloadContainer = ContainerFactory(song);
            return new DownloadJob(song, downloadContainer, FileIO);
        }

    }
}
