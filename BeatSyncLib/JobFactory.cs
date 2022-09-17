using BeatSyncLib.Downloader;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Utilities;
using SongFeedReaders.Feeds;
using SongFeedReaders.Logging;
using SongFeedReaders.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatSyncLib
{
    public class JobFactory : IJobFactory
    {
        private readonly ISongDownloader _songDownloader;
        private readonly IBeatmapsTarget[] _targets;
        private readonly IPauseManager? _pauseManager;
        private readonly ILogFactory? _logFactory;
        public JobFactory(ISongDownloader songDownloader, IEnumerable<IBeatmapsTarget> targets,
            IPauseManager? pauseManager = null, ILogFactory? logFactory = null)
        {
            _songDownloader = songDownloader ?? throw new ArgumentNullException(nameof(songDownloader));
            _targets = targets?.ToArray() ?? throw new ArgumentNullException(nameof(songDownloader));
            if (_targets.Length == 0)
                throw new ArgumentException("JobFactory must be provided at least one target", nameof(targets));
            _pauseManager = pauseManager;
            _logFactory = logFactory;
        }

        public IJob CreateJob(ISong song, IFeed feed)
        {
            return new Job(song ?? throw new ArgumentNullException(nameof(song)), _songDownloader, _targets, _pauseManager, _logFactory);
        }
    }
}
