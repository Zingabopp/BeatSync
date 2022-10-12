using BeatSyncLib.Downloader.Targets;
using BeatSyncLib.Utilities;
using SongFeedReaders.Logging;
using SongFeedReaders.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities.DownloadContainers;

namespace BeatSyncLib.Downloader
{
    public sealed class Job : IJob
    {
        private readonly IBeatmapTarget[] _targets;
        private readonly ISongDownloader _songDownloader;
        private readonly IPauseManager? _pauseManager;
        private readonly ILogger? _logger;
        private readonly MultiCancellationTokenSource _cancellationTokenSource = new MultiCancellationTokenSource();

        public event EventHandler? JobStarted;
        public event EventHandler<JobStage>? JobStageChanged;
        public event EventHandler<JobResult>? JobFinished;

        public Job(ISong beatmap, ISongDownloader songDownloader, IEnumerable<IBeatmapTarget> targets,
            IPauseManager? pauseManager = null, ILogFactory? logFactory = null)
        {
            Beatmap = beatmap ?? throw new ArgumentNullException(nameof(beatmap));
            _songDownloader = songDownloader ?? throw new ArgumentNullException(nameof(songDownloader));
            _targets = targets?.ToArray() ?? throw new ArgumentNullException(nameof(targets));
            if (_targets.Length == 0)
                throw new ArgumentException("No beatmap targets were provided", nameof(targets));
            _pauseManager = pauseManager;
            _logger = logFactory?.GetLogger();
        }

        public ISong Beatmap { get; private set; }

        public JobStage JobStage { get; private set; } = JobStage.NotStarted;

        public JobState JobState { get; private set; } = JobState.Ready;

        public Task<JobResult>? JobTask { get; private set; }

        public Task<JobResult> RunAsync(CancellationToken cancellationToken)
        {
            if (!_cancellationTokenSource.Disposed)
            {
                _cancellationTokenSource.AddToken(cancellationToken);
            }
            return JobTask ??= RunAsyncInternal(_cancellationTokenSource.Token);
        }

        private async Task<JobResult> RunAsyncInternal(CancellationToken cancellationToken)
        {
            JobResult result = new JobResult()
            {
                JobState = JobState,
                Song = Beatmap
            };
            List<TargetResult> targetResults = new List<TargetResult>(_targets.Length);
            try
            {
                SetState(JobState.Running, result);
                JobStarted?.Invoke(this, EventArgs.Empty);
                SetStage(JobStage.Downloading);
                await WaitForPause(cancellationToken);

                // Fine to dispose because if the beatmap is downloaded for one feed, it shares the same target as any other feed
                // that might also want the beatmap
                using DownloadedContainer downloadedContainer = await _songDownloader.DownloadSongAsync(Beatmap, cancellationToken).ConfigureAwait(false);
                DownloadResult downloadResult = downloadedContainer.DownloadResult;
                result.DownloadResult = downloadedContainer.DownloadResult;

                if (downloadResult.Status != DownloadResultStatus.Success)
                {
                    Exception ex = downloadResult.Exception ?? new Exception($"Download failed: {downloadResult.Status}");
                    throw ex;
                }
                DownloadContainer container = downloadedContainer.DownloadContainer
                    ?? throw new Exception("Download result did not have a container");
                await WaitForPause(cancellationToken);

                SetStage(JobStage.TransferringToTargets);
                for (int i = 0; i < _targets.Length; i++)
                {
                    IBeatmapTarget target = _targets[i];
                    if (container.TryGetResultStream(out Stream? stream, out Exception? exception)
                        && stream != null)
                    {
                        TargetResult targetResult = await target.TransferAsync(Beatmap, stream, cancellationToken);
                        result.HashAfterDownload = targetResult.BeatmapHash;
                    }
                    else if (exception != null)
                        throw exception;
                    else
                        throw new Exception("Unable to get download container result stream");
                }

            }
            catch (OperationCanceledException ex)
            {
                SetState(JobState.Cancelled, result);
                result.Exception = ex;
            }
            catch (Exception ex)
            {
                SetState(JobState.Error, result);
                result.Exception = ex;
            }
            finally
            {
                // TODO: JobStage.Finishing?

                SetStage(JobStage.Finished);
                if (targetResults.Count > 0)
                    result.TargetResults = targetResults.ToArray();
                _cancellationTokenSource.Dispose();
            }

            return result;
        }

        private void SetStage(JobStage stage)
        {
            if (JobStage == stage)
                return;
            JobStage = stage;
            JobStageChanged?.Invoke(this, stage);
        }

        private void SetState(JobState state, JobResult result)
        {
            JobState = state;
            result.JobState = state;
        }

#if NET5_0_OR_GREATER
        private async ValueTask WaitForPause(CancellationToken cancellationToken)
#else
        private async Task WaitForPause(CancellationToken cancellationToken)
#endif

        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_pauseManager != null)
            {
                if (_pauseManager.IsPaused)
                {
                    JobState prevState = JobState;
                    JobStage prevStage = JobStage;
                    SetStage(JobStage.Paused);
                    JobState = JobState.Paused;
                    await _pauseManager.WaitForPause(cancellationToken);
                    JobState = prevState;
                    SetStage(prevStage);
                }
            }
        }

    }
}
