using BeatSyncLib.Downloader.Downloading;
using BeatSyncLib.Downloader.Targets;
using SongFeedReaders.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public interface IJobBuilder
    {
        IEnumerable<SongTarget> SongTargets { get; }
        IJobBuilder AddTarget(SongTarget songTarget);
        IJobBuilder SetDefaultJobFinishedAsyncCallback(JobFinishedAsyncCallback jobFinishedCallback);

        Job CreateJob(ISong song, IProgress<JobProgress>? progress = null, JobFinishedAsyncCallback? finishedCallback = null);
    }
	
    public delegate Task JobFinishedAsyncCallback(JobResult jobResult);
    public delegate void JobFinishedCallback(JobResult jobResult);
}
