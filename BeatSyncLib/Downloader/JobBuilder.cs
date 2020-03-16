using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Playlists;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;

namespace BeatSyncLib.Downloader
{
    public class JobBuilder : IJobBuilder
    {
        private List<ISongTargetFactory> _songTargetFactories = new List<ISongTargetFactory>();
        private IDownloadJobFactory _downloadJobFactory;
        private JobFinishedAsyncCallback _finishedCallback;

        public void EnsureValidState()
        {
            if (_songTargetFactories.Count == 0)
                throw new InvalidOperationException($"Invalid JobBuilder state: no ISongTargetFactories have been added.");
            if (_downloadJobFactory == null)
                throw new InvalidOperationException($"Invalid JobBuilder state: an IDownloadJobFactory has not been set.");
        }


        public Job CreateJob(ScrapedSong song, IProgress<JobProgress> progress = null, JobFinishedAsyncCallback finishedCallback = null)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), $"{nameof(song)} cannot be null for {nameof(CreateJob)}");
            if (finishedCallback == null)
                finishedCallback = _finishedCallback;
            EnsureValidState();
            Job job = null;
            IDownloadJob downloadJob = _downloadJobFactory.CreateDownloadJob(song);
            List<ISongTarget> songTargets = new List<ISongTarget>();
            foreach (ISongTargetFactory songTargetFactory in _songTargetFactories)
            {
                songTargets.Add(songTargetFactory.CreateTarget(song));
            }
            job = new Job(song, downloadJob, songTargets, finishedCallback, progress);
            return job;
        }

        public IJobBuilder AddTargetFactory(ISongTargetFactory songTargetFactory)
        {
            if (songTargetFactory == null)
                throw new ArgumentNullException(nameof(songTargetFactory), $"{nameof(songTargetFactory)} cannot be null for {nameof(AddTargetFactory)}");
            _songTargetFactories.Add(songTargetFactory);
            return this;
        }

        public IJobBuilder SetDownloadJobFactory(IDownloadJobFactory downloadJobFactory)
        {
            if (downloadJobFactory == null)
                throw new ArgumentNullException(nameof(downloadJobFactory), $"{nameof(downloadJobFactory)} cannot be null for {nameof(SetDownloadJobFactory)}");
            this._downloadJobFactory = downloadJobFactory;
            return this;
        }

        public IJobBuilder SetDefaultJobFinishedCallback(JobFinishedAsyncCallback jobFinishedCallback)
        {
            _finishedCallback = jobFinishedCallback;
            return this;
        }
    }
}
