using SongFeedReaders.Feeds;
using SongFeedReaders.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeatSyncLib.Downloader
{
    public interface IJobFactory
    {
        // IJob needs ISongHashCollection
        IJob CreateJob(ISong song, IFeed feed);
    }
}
