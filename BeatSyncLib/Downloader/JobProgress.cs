using System;
using System.Collections.Generic;
using System.Text;
using BeatSyncLib.Downloader.Targets;

namespace BeatSyncLib.Downloader
{
    public class JobProgress
    {
        public static JobProgress CreateDownloadCompletion(ProgressValue totalProgress, DownloadedContainer result)
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
        public readonly IBeatmapTarget? SongTarget;
        public readonly DownloadedContainer? DownloadResult;
        public readonly TargetResult? TargetResult;

        public JobProgress(JobProgressType jobProgressType, JobStage jobStage, ProgressValue totalProgress, ProgressValue? stageProgress = null,
            IDownloadJob? downloadJob = null, IBeatmapTarget? songTarget = null, DownloadedContainer? downloadResult = null, TargetResult? targetResult = null)
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
}
