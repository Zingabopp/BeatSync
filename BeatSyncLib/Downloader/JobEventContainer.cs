using BeatSyncLib;
using BeatSyncLib.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public sealed class JobEventContainer
    {
        public static ConcurrentDictionary<string, DownloadStats> DownloadTracker = new ConcurrentDictionary<string, DownloadStats>();
        private string ReaderName;
        private WeakReference<IDownloadJob> JobReference;
        private int PostId;
        private WeakReference<IStatusManager> StatusManagerReference;
        private bool finishedStatusUpdated = false;
        private bool startedStatusUpdated = false;
        private Func<bool> readerFinishedPosting;
        public JobEventContainer(IDownloadJob job, string readerName, IStatusManager statusManager, Func<bool> readerFinished)
        {
            readerFinishedPosting = readerFinished;
            var stats = DownloadTracker.GetOrAdd(readerName, new DownloadStats());
            stats.IncrementTotalDownloads();
            JobReference = new WeakReference<IDownloadJob>(job);
            ReaderName = readerName;
            StatusManagerReference = new WeakReference<IStatusManager>(statusManager);
            job.OnJobStarted += Job_OnJobStarted;
            if (job.Status != DownloadJobStatus.NotStarted)
            {
                //Logger.log?.Warn($"Job already started: ({job.SongKey}) {job.SongName} by {job.LevelAuthorName}");
                StartedUpdateStatus();
            }
            job.OnJobFinished += Job_OnJobFinished;
            if (job.Status == DownloadJobStatus.Finished)
            {
                //Logger.log?.Warn($"Job already finished: ({job.SongKey}) {job.SongName} by {job.LevelAuthorName}");
                FinishedUpdateStatus(job.Result?.Successful ?? false,
                    job.Result?.DownloadResult?.Status ?? DownloadResultStatus.Unknown,
                    job.Result?.ZipResult?.ResultStatus ?? ZipExtractResultStatus.Unknown);
            }
        }

        private void Job_OnJobStarted(object sender, DownloadJobStartedEventArgs e)
        {
            StartedUpdateStatus();
        }

        private void Job_OnJobFinished(object sender, DownloadJobFinishedEventArgs e)
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
#if DEBUG
                Logger.log?.Debug($"Received PostId {PostId} for {job.SongName} by {job.LevelAuthorName}");
#endif
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
            var stats = DownloadTracker.GetOrAdd(ReaderName, new DownloadStats());
            var haveStatusManager = StatusManagerReference.TryGetTarget(out var statusManager);

            if (haveStatusManager)
            {
                if (successful)
                {
                    bool postSuccessful = statusManager.AppendPost(PostId, "Done", FontColor.Green);
#if DEBUG
                    string name = string.Empty;
                    if (JobReference.TryGetTarget(out var downloadJob))
                        name = downloadJob.SongName;
                    Logger.log?.Info($"   Attempt to update status for {name}, on {ReaderName}.{PostId}: {postSuccessful}");
#endif
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
                    stats.IncrementErroredDownloads();
                    bool postSuccessful = statusManager.AppendPost(PostId, reason, FontColor.Red);
#if DEBUG
                    string name = string.Empty;
                    if (JobReference.TryGetTarget(out var downloadJob))
                        name = downloadJob.SongName;
                    Logger.log?.Info($"   Attempt to update status for {name}, on {ReaderName}.{PostId}: {postSuccessful}");
#endif
                }

            }

            stats.IncrementFinishedDownloads();
            if (haveStatusManager)
            {
                string errorText = string.Empty;
                if (stats.ErroredDownloads > 0)
                {
#if DEBUG
                    Logger.log?.Warn($"Errored Downloads in {ReaderName}: {stats.ErroredDownloads}");
#endif
                    errorText = $" -- {stats.ErroredDownloads}{(stats.ErroredDownloads == 1 ? " download failed" : " downloads failed")}";
                }
                statusManager.SetSubHeader(ReaderName, $"{stats.FinishedDownloads}/{stats.TotalDownloads}{errorText}");
                if (readerFinishedPosting.Invoke() && stats.FinishedDownloads == stats.TotalDownloads)
                {
                    var fontColor = FontColor.Green;
                    if (stats.ErroredDownloads > 0)
                        fontColor = FontColor.Yellow;
                    statusManager.SetHeaderColor(ReaderName, fontColor);
                }
            }
        }
    }
}
