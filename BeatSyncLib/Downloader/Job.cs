using BeatSyncLib.Downloader.Downloading;
using BeatSyncLib.Downloader.Targets;
using SongFeedReaders;
using SongFeedReaders.Data;
using SongFeedReaders.Readers.BeatSaver;
using SongFeedReaders.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using WebUtilities.DownloadContainers;

namespace BeatSyncLib.Downloader
{
    public class Job : IJob
    {
        public bool CanPause { get; private set; }
        public void Pause() { }
        public void Unpause() { }
        public Exception? Exception { get; private set; }
        public ISong Song { get; private set; }
        public event EventHandler? JobStarted;
        public event EventHandler<JobResult>? JobFinished;
        public event EventHandler<JobProgress>? JobProgressChanged;

        protected TaskCompletionSource<JobResult> TaskCompletionSource = new TaskCompletionSource<JobResult>();
        public Task<JobResult> JobTask => TaskCompletionSource.Task;

        public JobResult? Result { get; private set; }

        public JobStage JobStage { get; private set; }
        public JobState JobState { get; private set; }

        private readonly IDownloadJob _downloadJob;
        private readonly SongTarget[] _targets;
        private JobFinishedAsyncCallback? JobFinishedAsyncCallback;
        private JobFinishedCallback? JobFinishedCallback;
        private readonly IProgress<JobProgress> _progress;
        public DownloadResult? DownloadResult { get; private set; }
        public IEnumerable<TargetResult> TargetResults { get; private set; } = Array.Empty<TargetResult>();
        private CancellationToken CancellationToken = CancellationToken.None;
        public void RegisterCancellationToken(CancellationToken cancellationToken)
        {
            if (!(JobState == JobState.Ready || JobState == JobState.NotReady))
                return;
            CancellationToken = cancellationToken;
        }
        private int _totalStages;
        private int _stageIndex;

        public void SetJobFinishedCallback(JobFinishedAsyncCallback jobFinishedAsyncCallback)
        {
            JobFinishedAsyncCallback = jobFinishedAsyncCallback;
            JobFinishedCallback = null;
        }
        public void SetJobFinishedCallback(JobFinishedCallback jobFinishedCallback)
        {
            JobFinishedCallback = jobFinishedCallback;
            JobFinishedAsyncCallback = null;
        }

        private Job(ISong song, IDownloadJob downloadJob, IEnumerable<SongTarget> targets, IProgress<JobProgress> progress)
        {
            Song = song;
            _downloadJob = downloadJob;
            _targets = targets.ToArray();
            _progress = progress;
            JobStage = JobStage.NotStarted;
            JobState = JobState.Ready;
            _totalStages = 1 + _targets.Length + 1;
            _stageIndex = 0;

        }
        public Job(ISong song, IDownloadJob downloadJob, IEnumerable<SongTarget> targets, JobFinishedAsyncCallback jobFinishedAsyncCallback, IProgress<JobProgress> progress)
            : this(song, downloadJob, targets, progress)
        {
            JobFinishedAsyncCallback = jobFinishedAsyncCallback;
        }

        public Job(ISong song, IDownloadJob downloadJob, IEnumerable<SongTarget> targets, JobFinishedCallback jobFinishedCallback, IProgress<JobProgress> progress)
            : this(song, downloadJob, targets, progress)
        {
            JobFinishedCallback = jobFinishedCallback;
        }

        private ProgressValue CurrentProgress => new ProgressValue(_stageIndex, _totalStages);
        private void ReportProgress(JobProgress progress)
        {
            EventHandler<JobProgress>? handler = JobProgressChanged;
            handler?.Invoke(this, progress);
            _progress?.Report(progress);
        }

        /// <summary>
        /// Transfer the downloaded song to the given <see cref="SongTarget"/>s.
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="downloadContainer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        protected async Task<TargetResult[]> TransferToTargets(IEnumerable<SongTarget> targets, DownloadContainer downloadContainer, CancellationToken cancellationToken)
        {
            List<TargetResult> completedTargets = new List<TargetResult>(_targets.Length);
            foreach (SongTarget? target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TargetResult result = await target.TransferAsync(Song, downloadContainer.GetResultStream(), cancellationToken).ConfigureAwait(false);
                completedTargets.Add(result);
                _stageIndex++;
                ReportProgress(JobProgress.CreateTargetCompletion(CurrentProgress, result));
            }
            return completedTargets.ToArray();

        }
        /// <summary>
        /// Starts the job.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            JobState = JobState.Running;
            if (CancellationToken.CanBeCanceled)
            {
                CancellationTokenSource? cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationToken);
                cancellationToken = cts.Token;
                cts.Dispose();
                cts = null;
            }
            JobStage = JobStage.Downloading;
            Exception? exception = null;
            bool canceled = false;
            EventHandler? handler = JobStarted;
            handler?.Invoke(this, null);
            DownloadContainer? downloadContainer = null;
            List<TargetResult> completedTargets = new List<TargetResult>(_targets.Length);
            List<SongTarget> pendingTargets = new List<SongTarget>(_targets.Length);
            foreach (SongTarget? target in _targets)
            {
                SongState songState = await target.CheckSongExistsAsync(Song).ConfigureAwait(false);
                if (songState != SongState.Wanted)
                    completedTargets.Add(new TargetResult(target, songState, true, null));
                else
                    pendingTargets.Add(target);
            }
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrEmpty(Song.Key) && Song.Hash != null && Song.Hash.Length > 0)
                {
                    SongInfoResponse? result = await WebUtils.SongInfoManager.GetSongByHashAsync(Song.Hash, cancellationToken).ConfigureAwait(false);
                    if (result.Success)
                    {
                        Song.Key = result.Song?.Key;
                    }
                }
                if (pendingTargets.Count > 0)
                {
                    _downloadJob.JobProgressChanged += _downloadJob_JobProgressChanged;
                    DownloadResult = await _downloadJob.RunAsync(cancellationToken).ConfigureAwait(false);
                    downloadContainer = DownloadResult.DownloadContainer;
                    if (DownloadResult.Exception != null)
                        throw DownloadResult.Exception;
                    if(downloadContainer != null)
                    {
                        _stageIndex = 1;
                        ReportProgress(JobProgress.CreateDownloadCompletion(CurrentProgress, DownloadResult));
                        JobStage = JobStage.TransferringToTargets;
                        // Report progress on the targets that do not want the song.
                        foreach (TargetResult? targetResult in completedTargets)
                        {
                            _stageIndex++;
                            ReportProgress(JobProgress.CreateTargetCompletion(CurrentProgress, targetResult));
                        }
                        // Transfer to targets that do want the song.
                        completedTargets.AddRange(await TransferToTargets(pendingTargets, downloadContainer, cancellationToken).ConfigureAwait(false));
                    }
                    else
                    {
                        Logger.log?.Warn($"DownloadResult has no exception, but returned a null DownloadContainer.");
                    }
                }
                else
                {
                    DownloadResult = new DownloadResult(null, DownloadResultStatus.Skipped, 0);
                    _stageIndex = 1;
                    _downloadJob_JobProgressChanged(this, new DownloadJobProgressChangedEventArgs(DownloadJobStatus.Finished));
                    JobStage = JobStage.TransferringToTargets;
                    foreach (TargetResult? targetResult in completedTargets)
                    {
                        _stageIndex++;
                        ReportProgress(JobProgress.CreateTargetCompletion(CurrentProgress, targetResult));
                    }
                }
                TargetResult[] failedTargets = completedTargets.Where(t => !t.Success).ToArray();
                if (failedTargets.Length > 0)
                {
                    Exception? firstException = failedTargets.FirstOrDefault(t => t.Exception != null)?.Exception;
                    if (firstException != null)
                        throw firstException;
                }
                TargetResults = completedTargets.ToArray();
            }
            catch (OperationCanceledException ex)
            {
                if (TargetResults == null && completedTargets.Count > 0)
                    TargetResults = completedTargets.ToArray();
                else
                    TargetResults = Array.Empty<TargetResult>();

                canceled = true;
                exception = ex;
                ReportProgress(JobProgress.CreateFromFault(JobProgressType.Cancellation, JobStage, CurrentProgress));
                JobState = JobState.Cancelled;
            }
            catch (Exception ex)
            {
                if (TargetResults == null && completedTargets.Count > 0)
                    TargetResults = completedTargets.ToArray();
                else
                    TargetResults = Array.Empty<TargetResult>();
                exception = ex;
                JobState = JobState.Error;
                ReportProgress(JobProgress.CreateFromFault(JobProgressType.Error, JobStage, CurrentProgress));
            }
            finally
            {
                JobStage = JobStage.Finishing;
                try
                {
                    downloadContainer?.Dispose();
                }
                catch { }
                if (DownloadResult == null)
                {
                    // TODO: What was this for?
                }
                FinishJob(canceled, exception);
                JobFinishedAsyncCallback? asyncCallback = JobFinishedAsyncCallback;
                if (asyncCallback != null)
                    await asyncCallback(Result).ConfigureAwait(false);
            }
            
        }

        private void _downloadJob_JobProgressChanged(object sender, DownloadJobProgressChangedEventArgs e)
        {

            ReportProgress(JobProgress.CreateDownloadingProgress(CurrentProgress, e.DownloadProgress ?? default(ProgressValue)));
        }

        private void DownloadContainer_ProgressChanged(object sender, DownloadProgress e)
        {
            //throw new NotImplementedException();
        }

        protected virtual void FinishJob(bool canceled = false, Exception? exception = null)
        {
            Exception = exception;
            if (canceled || exception is OperationCanceledException)
                JobState = JobState.Cancelled;
            else if (exception != null)
                JobState = JobState.Error;
            else
                JobState = JobState.Finished;
            _stageIndex++;
            Result = new JobResult()
            {
                Song = Song,
                DownloadResult = DownloadResult,
                JobState = JobState,
                TargetResults = TargetResults.ToArray(),
                Exception = exception
            };
            JobStage = JobStage.Finished;
            EventHandler<JobResult>? handler = JobFinished;
            handler?.Invoke(this, Result);
            ReportProgress(JobProgress.CreateJobFinished(CurrentProgress));
            JobFinishedCallback? callback = JobFinishedCallback;
            callback?.Invoke(Result);
            TaskCompletionSource.SetResult(Result);
        }

        public Task RunAsync()
        {
            return RunAsync(CancellationToken.None);
        }


        public override string ToString()
        {
            string retStr = string.Empty;
            if (!string.IsNullOrEmpty(Song.Key))
                retStr = $"({Song.Key}) ";
            retStr = retStr + $"{Song.Name} by {Song.LevelAuthorName}";
#if DEBUG
            retStr = string.Join(" | ", retStr, $"{JobState} ({JobStage})");
#endif
            return retStr;
        }
    }
}
