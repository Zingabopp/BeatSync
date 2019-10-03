using BeatSync.UI;
using BeatSync.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync.Downloader
{
    public sealed class JobFinishedContainer
    {
        private string ReaderName;
        private WeakReference<IDownloadJob> JobReference;
        private int PostId;
        private WeakReference<IStatusManager> StatusManagerReference;
        private bool finishedStatusUpdated = false;
        private bool startedStatusUpdated = false;
        public JobFinishedContainer(IDownloadJob job, string readerName, IStatusManager statusManager)
        {
            JobReference = new WeakReference<IDownloadJob>(job);
            ReaderName = readerName;
            StatusManagerReference = new WeakReference<IStatusManager>(statusManager);
            job.OnJobStarted += Job_OnJobStarted;
            if (job.Status != JobStatus.NotStarted)
            {
                //Logger.log?.Warn($"Job already started: ({job.SongKey}) {job.SongName} by {job.LevelAuthorName}");
                StartedUpdateStatus();
            }
            job.OnJobFinished += Job_OnJobFinished;
            if (job.Status == JobStatus.Finished)
            {
                //Logger.log?.Warn($"Job already finished: ({job.SongKey}) {job.SongName} by {job.LevelAuthorName}");
                FinishedUpdateStatus(job.Result?.Successful ?? false,
                    job.Result?.DownloadResult?.Status ?? DownloadResultStatus.Unknown,
                    job.Result?.ZipResult?.ResultStatus ?? ZipExtractResultStatus.Unknown);
            }
        }

        private void Job_OnJobStarted(object sender, JobStartedEventArgs e)
        {
            StartedUpdateStatus();
        }

        private void Job_OnJobFinished(object sender, JobFinishedEventArgs e)
        {
            FinishedUpdateStatus(e.JobSuccessful, e?.DownloadResult ?? DownloadResultStatus.Unknown, e?.ZipExtractResult ?? ZipExtractResultStatus.Unknown);
        }

        private void StartedUpdateStatus()
        {
            JobReference.TryGetTarget(out var job);
            if (job != null)
                job.OnJobStarted -= Job_OnJobStarted;
            if (startedStatusUpdated)
                return;
            startedStatusUpdated = true;
            Logger.log?.Info($"   Starting download for ({job.SongKey}) {job.SongName} by {job.LevelAuthorName}");
            if (StatusManagerReference.TryGetTarget(out var statusManager))
            {
                PostId = statusManager.Post(ReaderName, $"-Downloading {job.SongName} by {job.LevelAuthorName}...");
                //Logger.log?.Debug($"Received PostId {PostId} for {job.SongName} by {job.LevelAuthorName}");
            }
        }

        private void FinishedUpdateStatus(bool successful, DownloadResultStatus downloadStatus, ZipExtractResultStatus zipStatus)
        {
            if (JobReference.TryGetTarget(out var job))
                job.OnJobFinished -= Job_OnJobFinished;
            if (finishedStatusUpdated)
                return;
            finishedStatusUpdated = true;
            if (PostId == 0)
                Logger.log?.Warn($"PostId during FinishedUpdateStatus is 0: {job.SongKey} {job.LevelAuthorName}");
            if (StatusManagerReference.TryGetTarget(out var statusManager))
            {
                if (successful)
                {
                    statusManager.AppendPost(PostId, "Done", FontColor.Green);
                }
                else
                {
                    string reason = "Failed";
                    if (downloadStatus != DownloadResultStatus.Success)
                    {
                        reason = "Download Failed";
                        if (downloadStatus == DownloadResultStatus.NetNotFound)
                            reason = "Removed From BeatSaver";

                    }
                    else if (zipStatus != ZipExtractResultStatus.Success)
                    {
                        reason = "Extraction Failed";
                    }
                    statusManager.AppendPost(PostId, reason, FontColor.Red);
                }
            }
        }
    }
}
