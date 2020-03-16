using BeatSyncLib.Downloader.Targets;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;

namespace BeatSyncLib.Downloader
{
    public class Job : IJob
    {
        public bool CanPause { get; private set; }
        public void Pause() { }
        public void Unpause() { }
        public Exception Exception { get; private set; }
        public ISong Song { get; private set; }
        public event EventHandler JobStarted;
        public event EventHandler<JobResult> JobFinished;
        public event EventHandler<JobProgress> JobProgressChanged;

        public JobResult Result { get; private set; }

        public JobStage JobStage { get; private set; }
        public JobState JobState { get; private set; }

        private readonly IDownloadJob _downloadJob;
        private readonly ISongTarget[] _targets;
        private TargetResult[] _targetResults;
        private JobFinishedAsyncCallback JobFinishedAsyncCallback;
        private JobFinishedCallback JobFinishedCallback;
        private readonly IProgress<JobProgress> _progress;
        public DownloadResult DownloadResult { get; private set; }
        public TargetResult[] TargetResults { get; private set; }
        private CancellationToken CancellationToken= CancellationToken.None;
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

        private Job(ISong song, IDownloadJob downloadJob, IEnumerable<ISongTarget> targets, IProgress<JobProgress> progress)
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
        public Job(ISong song, IDownloadJob downloadJob, IEnumerable<ISongTarget> targets, JobFinishedAsyncCallback jobFinishedAsyncCallback, IProgress<JobProgress> progress)
            : this(song, downloadJob, targets, progress)
        {
            JobFinishedAsyncCallback = jobFinishedAsyncCallback;
        }

        public Job(ISong song, IDownloadJob downloadJob, IEnumerable<ISongTarget> targets, JobFinishedCallback jobFinishedCallback, IProgress<JobProgress> progress)
            : this(song, downloadJob, targets, progress)
        {
            JobFinishedCallback = jobFinishedCallback;
        }

        private ProgressValue CurrentProgress => new ProgressValue(_stageIndex, _totalStages);
        private void ReportProgress(JobProgress progress)
        {
            EventHandler<JobProgress> handler = JobProgressChanged;
            handler?.Invoke(this, progress);
            _progress?.Report(progress);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            JobState = JobState.Running;
            if (CancellationToken.CanBeCanceled)
            {
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationToken);
                cancellationToken = cts.Token;
                cts.Dispose();
                cts = null;
            }
            JobStage = JobStage.Downloading;
            EventHandler handler = JobStarted;
            Exception exception = null;
            bool canceled = false;
            handler?.Invoke(this, null);
            List<TargetResult> completedTargets = new List<TargetResult>(_targets.Length);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _downloadJob.JobProgressChanged += _downloadJob_JobProgressChanged;
                DownloadResult downloadResult = await _downloadJob.RunAsync(cancellationToken).ConfigureAwait(false);
                if (downloadResult.Exception != null)
                    throw downloadResult.Exception;
                _stageIndex = 1;
                ReportProgress(JobProgress.CreateDownloadCompletion(CurrentProgress, downloadResult));
                //ReportJobProgress(JobProgressType.StageCompletion, downloadResult);
                DownloadContainer downloadContainer = downloadResult.DownloadContainer;
                downloadContainer.ProgressChanged += DownloadContainer_ProgressChanged;
                JobStage = JobStage.TransferringToTarget;
                for (int i = 0; i < _targets.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    TargetResult result = await _targets[i].TransferAsync(downloadContainer.GetResultStream(), cancellationToken).ConfigureAwait(false);
                    completedTargets.Add(result);
                    _stageIndex++;
                    ReportProgress(JobProgress.CreateTargetCompletion(CurrentProgress, result));
                    //ReportJobProgress(JobProgressType.StageCompletion, result);
                }
                _targetResults = completedTargets.ToArray();
                if (completedTargets.All(t => !t.Success))
                    throw completedTargets.First().Exception;
            }
            catch (OperationCanceledException ex)
            {
                if (_targetResults == null && completedTargets.Count > 0)
                    _targetResults = completedTargets.ToArray();
                else
                    _targetResults = Array.Empty<TargetResult>();

                canceled = true;
                exception = ex;
                ReportProgress(JobProgress.CreateFromFault(JobProgressType.Cancellation, JobStage, CurrentProgress));
                JobState = JobState.Cancelled;
            }
            catch (Exception ex)
            {
                if (_targetResults == null && completedTargets.Count > 0)
                    _targetResults = completedTargets.ToArray();
                else
                    _targetResults = Array.Empty<TargetResult>();
                exception = ex;
                JobState = JobState.Error;
                ReportProgress(JobProgress.CreateFromFault(JobProgressType.Error, JobStage, CurrentProgress));
            }
            finally
            {
                JobStage = JobStage.Finishing;
            }

            FinishJob(canceled, exception);
            JobFinishedAsyncCallback asyncCallback = JobFinishedAsyncCallback;
            if (asyncCallback != null)
                await asyncCallback(Result).ConfigureAwait(false);
        }

        private void _downloadJob_JobProgressChanged(object sender, DownloadJobProgressChangedEventArgs e)
        {

            ReportProgress(JobProgress.CreateDownloadingProgress(CurrentProgress, e.DownloadProgress ?? default(ProgressValue)));
        }

        private void DownloadContainer_ProgressChanged(object sender, DownloadProgress e)
        {
            //throw new NotImplementedException();
        }

        protected virtual void FinishJob(bool canceled = false, Exception exception = null)
        {
            Exception = exception;
            Result = new JobResult()
            {
                Song = Song,
                DownloadResult = _downloadJob?.DownloadResult,
                TargetResults = _targets.Select(t => t.TargetResult).ToArray(),
                Exception = exception
            };
            if (canceled || exception is OperationCanceledException)
                JobState = JobState.Cancelled;
            else if (exception != null)
                JobState = JobState.Error;
            else
                JobState = JobState.Finished;
            _stageIndex++;
            JobStage = JobStage.Finished;
            EventHandler<JobResult> handler = JobFinished;
            handler?.Invoke(this, Result);
            ReportProgress(JobProgress.CreateJobFinished(CurrentProgress));
            JobFinishedCallback callback = JobFinishedCallback;
            callback?.Invoke(Result);
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
