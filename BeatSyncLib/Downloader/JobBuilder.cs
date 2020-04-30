using BeatSyncLib.Downloader.Downloading;
using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Playlists;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;

namespace BeatSyncLib.Downloader
{
    public class JobBuilder : IJobBuilder
    {
        private List<SongTarget> _songTargets = new List<SongTarget>();
        private IDownloadJobFactory _downloadJobFactory;
        private JobFinishedAsyncCallback _finishedCallback;

        public IEnumerable<SongTarget> SongTargets => _songTargets.ToArray();

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
            Job job = new Job(song, downloadJob, songTargets, finishedCallback, progress);
            return job;
        }

        public IJobBuilder AddTarget(SongTarget songTarget)
        {
            if (songTarget == null)
                throw new ArgumentNullException(nameof(songTarget), $"{nameof(songTarget)} cannot be null for {nameof(AddTarget)}");
            _songTargets.Add(songTarget);
            return this;
        }

        public IJobBuilder SetDownloadJobFactory(IDownloadJobFactory downloadJobFactory)
        {
            if (downloadJobFactory == null)
                throw new ArgumentNullException(nameof(downloadJobFactory), $"{nameof(downloadJobFactory)} cannot be null for {nameof(SetDownloadJobFactory)}");
            this._downloadJobFactory = downloadJobFactory;
            return this;
        }

        public IJobBuilder SetDefaultJobFinishedAsyncCallback(JobFinishedAsyncCallback jobFinishedCallback)
        {
            _finishedCallback = jobFinishedCallback;
            return this;
        }
    }
}
