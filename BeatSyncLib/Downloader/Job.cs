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
        private readonly TargetResult[] _targetResults;
        private readonly JobFinishedCallback JobFinishedCallback;
        private readonly IProgress<JobProgress> _progress;

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
        }
        private void ReportJobProgress(JobProgressType progressType, IDownloadJob downloadJob, ISongTarget songTarget = null)
        {
            JobProgress progress = new JobProgress()
            {
                JobProgressType = progressType,
                JobStage = JobStage,
                TotalProgress = new ProgressValue(_stageIndex, _totalStages),
                CurrentDownloadJob = downloadJob,
                CurrentTarget = songTarget
            };

            EventHandler<JobProgress> handler = ProgressChanged;
            handler?.Invoke(this, progress);
            _progress?.Report(progress);
        }
        private void ReportJobProgress(JobProgressType progressType) => ReportJobProgress(progressType, null, null);
        private void ReportJobProgress(JobProgressType progressType, ISongTarget songTarget) => ReportJobProgress(progressType, null, songTarget);

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            JobState = JobState.Running;
            JobStage = JobStage.Downloading;
            EventHandler handler = JobStarted;
            Exception exception = null;
            bool canceled = false;
            handler?.Invoke(this, null);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                DownloadResult downloadResult = await _downloadJob.RunAsync(cancellationToken).ConfigureAwait(false);
                if (downloadResult.Exception != null)
                    throw downloadResult.Exception;
                DownloadContainer downloadContainer = downloadResult.DownloadContainer;
                JobStage = JobStage.TransferringToTarget;
                for (int i = 0; i < _targets.Length; i++)
                {
                    _targetResults[i] = await _targets[i].TransferAsync(downloadContainer.GetResultStream(), cancellationToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                canceled = true;
                exception = ex;
                ReportJobProgress(JobProgressType.Cancellation);
                JobState = JobState.Cancelled;
            }
            catch (Exception ex)
            {
                exception = ex;
                JobState = JobState.Error;
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
                SongHash = Song.Hash,
                SongKey = Song.SongKey,
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
            EventHandler<JobResult> handler = JobFinished;
            handler?.Invoke(this, Result);
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
