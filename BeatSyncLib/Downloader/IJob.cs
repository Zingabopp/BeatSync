using System;
using System.Collections.Generic;
using System.Text;
using BeatSyncLib.Downloader.Targets;
using System.Threading.Tasks;
using System.Threading;
using SongFeedReaders.Models;

namespace BeatSyncLib.Downloader
{

    /// <summary>
    /// Represents checking if a beatmap is wanted by any targets, downloading, and delivering it to all appropriate targets.
    /// </summary>
    public interface IJob
    {
        event EventHandler? JobStarted;
        event EventHandler<JobStage>? JobStageChanged;
        event EventHandler<JobResult>? JobFinished;
        ISong Beatmap { get; }
        JobStage JobStage { get; }
        JobState JobState { get; }
        Task<JobResult>? JobTask { get; }
        Task<JobResult> RunAsync(CancellationToken cancellationToken);
    }
}
