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
        void SetJobFinishedCallback(JobFinishedAsyncCallback jobFinishedAsyncCallback);
        void SetJobFinishedCallback(JobFinishedCallback jobFinishedCallback);
        Task RunAsync(CancellationToken cancellationToken);
        Task RunAsync();
    }

    public class JobProgress
    {
        public static JobProgress CreateDownloadCompletion(ProgressValue totalProgress, DownloadResult result)
        {
            return new JobProgress(JobProgressType.StageCompletion, JobStage.Downloading, totalProgress, downloadResult: result);
        }
        public static JobProgress CreateDownloadingProgress(ProgressValue totalProgress, ProgressValue downloadProgress)
        {
            return new JobProgress(JobProgressType.StageProgress, JobStage.Downloading, totalProgress, downloadProgress);
        }
        public static JobProgress CreateTargetCompletion(ProgressValue totalProgress, TargetResult result)
        {
            return new JobProgress(JobProgressType.StageCompletion, JobStage.TransferringToTargets, totalProgress, targetResult: result);
        }
        public static JobProgress CreateTargetProgress(ProgressValue totalProgress, ProgressValue targetProgress)
        {
            return new JobProgress(JobProgressType.StageProgress, JobStage.TransferringToTargets, totalProgress, targetProgress);
        }
        public static JobProgress CreateJobFinishing(ProgressValue totalProgress, ProgressValue targetProgress)
        {
            return new JobProgress(JobProgressType.StageProgress, JobStage.Finishing, totalProgress, targetProgress);
        }
        public static JobProgress CreateJobFinished(ProgressValue totalProgress)
        {
            return new JobProgress(JobProgressType.Finished, JobStage.Finished, totalProgress);
        }
        public static JobProgress CreateFromFault(JobProgressType progressType, JobStage jobStage, ProgressValue totalProgress)
        {
            return new JobProgress(progressType, jobStage, totalProgress);
        }


        public readonly JobProgressType JobProgressType;
        public readonly JobStage JobStage;
        public readonly ProgressValue TotalProgress;
        public readonly ProgressValue? StageProgress;
        public readonly IDownloadJob? DownloadJob;
        public readonly SongTarget? SongTarget;
        public readonly DownloadResult? DownloadResult;
        public readonly TargetResult? TargetResult;
        public JobProgress(JobProgressType jobProgressType, JobStage jobStage, ProgressValue totalProgress, ProgressValue? stageProgress = null, 
            IDownloadJob? downloadJob = null, SongTarget? songTarget = null, DownloadResult? downloadResult = null, TargetResult? targetResult = null)
        {
            JobProgressType = jobProgressType;
            JobStage = jobStage;
            StageProgress = stageProgress;
            TotalProgress = totalProgress;
            DownloadJob = downloadJob;
            SongTarget = songTarget;
            DownloadResult = downloadResult;
            TargetResult = targetResult;
        }
        public override string ToString()
        {
            return $"{JobProgressType}: {TotalProgress.TotalProgress}/{TotalProgress.ExpectedMax} | {JobStage}: {StageProgress?.ToString() ?? "<N/A>"}";
        }
    }

    public enum JobStage
    {
        NotStarted = 0,
        Downloading = 1,
        TransferringToTargets = 2,
        Finishing = 3,
        Finished = 4
    }

    public enum JobState
    {
        NotReady = 0,
        Ready = 1,
        Running = 2,
        Finished = 3,
        Cancelled = 4,
        Error = 5
    }

    public enum JobProgressType
    {
        None = 0,
        StageProgress = 1,
        StageCompletion = 2,
        Finished = 3,
        Error = 4,
        Cancellation = 5,
        Paused = 6
    }
    public delegate Task JobFinishedAsyncCallback(JobResult jobResult);
    public delegate void JobFinishedCallback(JobResult jobResult);
}
