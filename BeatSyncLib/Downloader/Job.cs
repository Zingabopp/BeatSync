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
    public class Job
    {
        public bool CanPause { get; private set; }
        public void Pause() { }
        public void Unpause() { }
        public Exception Exception { get; private set; }
        public ScrapedSong Song { get; private set; }
        public event EventHandler JobStarted;
        public event EventHandler<JobResult> JobFinished;
        public event EventHandler<JobProgress> ProgressChanged;

        public JobResult Result { get; private set; }

        public JobStage JobStage { get; private set; }
        public JobState JobState { get; private set; }

        private readonly IDownloadJob _downloadJob;
        private readonly ISongTarget[] _targets;
        private TargetResult[] _targetResults;
        private readonly JobFinishedCallback JobFinishedCallback;
        private readonly IProgress<JobProgress> _progress;
        public DownloadResult DownloadResult { get; private set; }
        public TargetResult[] TargetResults { get; private set; }

        private int _totalStages;
        private int _stageIndex;
        public Job(ScrapedSong song, IDownloadJob downloadJob, IEnumerable<ISongTarget> targets, JobFinishedCallback jobFinishedCallback, IProgress<JobProgress> progress)
        {
            Song = song;
            _downloadJob = downloadJob;
            _targets = targets.ToArray();
            JobFinishedCallback = jobFinishedCallback;
            _progress = progress;
            JobStage = JobStage.NotStarted;
            JobState = JobState.Ready;
            _totalStages = 1 + _targets.Length + 1;
            _stageIndex = 0;
        }
        private void ReportJobProgress(JobProgressType progressType, DownloadResult downloadJob, TargetResult songTargetResult = null)
        {
            JobProgress progress = new JobProgress()
            {
                JobProgressType = progressType,
                JobStage = JobStage,
                TotalProgress = new ProgressValue(_stageIndex, _totalStages),
                DownloadResult = downloadJob,
                TargetResult = songTargetResult
            };

            EventHandler<JobProgress> handler = ProgressChanged;
            handler?.Invoke(this, progress);
            _progress?.Report(progress);
        }
        private void ReportJobProgress(JobProgressType progressType) => ReportJobProgress(progressType, null, null);
        private void ReportJobProgress(JobProgressType progressType, TargetResult songTargetResult) => ReportJobProgress(progressType, null, songTargetResult);

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            JobState = JobState.Running;
            JobStage = JobStage.Downloading;
            EventHandler handler = JobStarted;
            Exception exception = null;
            bool canceled = false;
            handler?.Invoke(this, null);
            List<TargetResult> completedTargets = new List<TargetResult>(_targets.Length);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                DownloadResult downloadResult = await _downloadJob.RunAsync(cancellationToken).ConfigureAwait(false);
                if (downloadResult.Exception != null)
                    throw downloadResult.Exception;
                _stageIndex = 1;
                ReportJobProgress(JobProgressType.StageCompletion, downloadResult);
                DownloadContainer downloadContainer = downloadResult.DownloadContainer;
                JobStage = JobStage.TransferringToTarget;
                for (int i = 0; i < _targets.Length; i++)
                {
                    TargetResult result = await _targets[i].TransferAsync(downloadContainer.GetResultStream(), cancellationToken).ConfigureAwait(false);
                    completedTargets.Add(result);
                    _stageIndex++;
                    ReportJobProgress(JobProgressType.StageCompletion, result);
                }
                _targetResults = completedTargets.ToArray();
            }
            catch (OperationCanceledException ex)
            {
                if (_targetResults == null && completedTargets.Count > 0)
                    _targetResults = completedTargets.ToArray();
                else
                    _targetResults = Array.Empty<TargetResult>();
                    
                canceled = true;
                exception = ex;
                ReportJobProgress(JobProgressType.Cancellation);
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
                ReportJobProgress(JobProgressType.Error);
            }
            finally
            {

                JobStage = JobStage.Finishing;
            }

            FinishJob(canceled, exception);
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
            ReportJobProgress(JobProgressType.Finished);
            JobFinishedCallback?.Invoke(Result);
        }

        public Task RunAsync()
        {
            return RunAsync(CancellationToken.None);
        }


        public override string ToString()
        {
            string retStr = string.Empty;
            if (!string.IsNullOrEmpty(Song.SongKey))
                retStr = $"({Song.SongKey}) ";
            retStr = retStr + $"{Song.SongName} by {Song.MapperName}";
#if DEBUG
            retStr = string.Join(" | ", retStr, $"{JobState} ({JobStage})");
#endif
            return retStr;
        }
    }
}
