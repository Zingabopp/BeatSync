using System;
using System.Collections.Generic;
using System.Text;
using BeatSyncLib.Downloader.Targets;
using System.Threading.Tasks;
using System.Threading;
using SongFeedReaders.Models;

namespace BeatSyncLib.Downloader
{
    public interface IJob
    {
        event EventHandler? JobStarted;
        event EventHandler<JobProgress>? JobProgressChanged;
        event EventHandler<JobResult>? JobFinished;
        Exception? Exception { get; }
        ISong Song { get; }
        JobResult? Result { get; }
        JobStage JobStage { get; }
        JobState JobState { get; }
        Task<JobResult> JobTask { get; }
        DownloadResult? DownloadResult { get; }
        IEnumerable<TargetResult> TargetResults { get; }
        Task RunAsync(CancellationToken cancellationToken);
        Task RunAsync();
    }
}
