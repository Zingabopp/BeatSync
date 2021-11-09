using BeatSyncLib.Downloader.Downloading;
using BeatSyncLib.Downloader.Targets;
using SongFeedReaders.Models;
using SongFeedReaders.Services;
using System;
using System.Collections.Generic;
using WebUtilities;

namespace BeatSyncLib.Downloader
{
    public class JobBuilder : IJobBuilder
    {
        private List<SongTarget> _songTargets = new List<SongTarget>();
        private IDownloadJobFactory _downloadJobFactory;
        private JobFinishedAsyncCallback? _finishedCallback;
        IWebClient _webClient;
        ISongInfoManager _infoManager;
        public IEnumerable<SongTarget> SongTargets => _songTargets.ToArray();

        public JobBuilder(IWebClient webClient, ISongInfoManager infoManager, IDownloadJobFactory jobFactory)
        {
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            _infoManager = infoManager ?? throw new ArgumentNullException(nameof(infoManager));
            _downloadJobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
        }

        public void EnsureValidState()
        {
            if (_songTargets.Count == 0)
                throw new InvalidOperationException($"Invalid JobBuilder state: no ISongTargetFactories have been added.");
            if (_downloadJobFactory == null)
                throw new InvalidOperationException($"Invalid JobBuilder state: an IDownloadJobFactory has not been set.");
        }


        public Job CreateJob(ISong song, IProgress<JobProgress>? progress = null, JobFinishedAsyncCallback? finishedCallback = null)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), $"{nameof(song)} cannot be null for {nameof(CreateJob)}");
            if (finishedCallback == null)
                finishedCallback = _finishedCallback;
            EnsureValidState();
            IDownloadJob downloadJob = _downloadJobFactory.CreateDownloadJob(song);
            List<SongTarget> songTargets = new List<SongTarget>();
            foreach (SongTarget songTarget in _songTargets)
            {
                songTargets.Add(songTarget);
            }
            Job job = new Job(song, downloadJob, songTargets, _infoManager, finishedCallback, progress);
            return job;
        }

        public IJobBuilder AddTarget(SongTarget songTarget)
        {
            if (songTarget == null)
                throw new ArgumentNullException(nameof(songTarget), $"{nameof(songTarget)} cannot be null for {nameof(AddTarget)}");
            _songTargets.Add(songTarget);
            return this;
        }

        public IJobBuilder SetDefaultJobFinishedAsyncCallback(JobFinishedAsyncCallback jobFinishedCallback)
        {
            _finishedCallback = jobFinishedCallback;
            return this;
        }
    }
}
