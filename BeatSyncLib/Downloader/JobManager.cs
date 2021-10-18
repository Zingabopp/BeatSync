using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader
{
    public class JobManager
    {
        private BlockingCollection<IJob> _queuedJobs = new BlockingCollection<IJob>();

        private readonly ConcurrentDictionary<string, IJob> _existingJobs = new ConcurrentDictionary<string, IJob>();
        private readonly ConcurrentDictionary<string, IJob> _activeJobs = new ConcurrentDictionary<string, IJob>();
        private readonly ConcurrentDictionary<string, IJob> _completedDownloads = new ConcurrentDictionary<string, IJob>();
        private readonly ConcurrentDictionary<string, IJob> _failedDownloads = new ConcurrentDictionary<string, IJob>();
        private readonly ConcurrentDictionary<string, IJob> _cancelledDownloads = new ConcurrentDictionary<string, IJob>();
        public IReadOnlyList<IJob> CompletedJobs
        {
            get { return _completedDownloads.Values.ToList(); }
        }
        private bool _acceptingJobs = false;
        private bool _running = false;
        private int _concurrentDownloads = 1;
        private CancellationToken _externalCancellation;
        private CancellationTokenSource? _cancellationSource;
        //private Task[] _tasks;
        private Thread[] _threads;
        public int ActiveJobs => _activeJobs.Count;

        /// <summary>
        /// Number of simultaneous downloads.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for values less than 1.</exception>
        public int ConcurrentDownloads
        {
            get { return _concurrentDownloads; }
            private set
            {
                if (value == _concurrentDownloads)
                    return;
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(ConcurrentDownloads), "ConcurrentDownloads cannot be less than 1.");
                _concurrentDownloads = value;
            }
        }

        public void ClearHistory()
        {
            _existingJobs.Clear();
            _failedDownloads.Clear();
            _cancelledDownloads.Clear();
            _completedDownloads.Clear();
        }

        public JobManager(int concurrentDownloads)
        {
            ConcurrentDownloads = concurrentDownloads;
            _acceptingJobs = true;
            //_tasks = new Task[ConcurrentDownloads];
            _threads = new Thread[ConcurrentDownloads];
            _threadCompletionSource = new TaskCompletionSource<bool>[ConcurrentDownloads];
        }

        public void Start(CancellationToken cancellationToken)
        {
            _externalCancellation = cancellationToken;
            if (_running)
                return;
            _running = true;
            if (_cancellationSource == null || _cancellationSource.IsCancellationRequested)
                _cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(_externalCancellation);
            for (int i = 0; i < ConcurrentDownloads; i++)
            {
                int taskId = i; // Apparently using 'i' directly for HandlerStartAsync doesn't work well...
                //TaskFactory tf = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
                //_tasks[i] = tf.StartNew(() => HandlerStartAsync(taskId, _cancellationSource.Token)).Unwrap();
                _threadCompletionSource[i] = new TaskCompletionSource<bool>();
                _threads[i] = new Thread(new ParameterizedThreadStart(ThreadStart));
                _threads[i].Start(taskId);
            }
        }
        private TaskCompletionSource<bool>[] _threadCompletionSource;
        public void Stop()
        {
            //_queuedJobs.CompleteAdding();
            _running = false;
            if (_cancellationSource != null)
            {
                _cancellationSource.Cancel();
                _cancellationSource.Dispose();
                _cancellationSource = null;
            }
        }

        public Task StopAsync()
        {
            Stop();
            return WaitForThreadStopAsync();
        }

        private Task WaitForThreadStopAsync() => Task.WhenAll(_threadCompletionSource.Select(tcs => tcs.Task));

        /// <summary>
        /// 
        /// </summary>
        public void Complete()
        {
            // TODO: _running isn't set to false with this.
            _acceptingJobs = false;
            _queuedJobs.CompleteAdding();
        }

        /// <summary>
        /// Completes the DownloadManager and waits for all remaining jobs to finish.
        /// </summary>
        /// <returns></returns>
        public async Task CompleteAsync()
        {
            Complete();
            try
            {
                if (_running)
                    await WaitForThreadStopAsync();
            }
            catch { }
            _running = false;
        }

        /// <summary>
        /// Tries to post a new job to the queue. If the song was already downloaded, returns the existing DownloadJob. 
        /// Returns null if the job failed to post and the song wasn't previously downloaded.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public bool TryPostJob(IJob job, out IJob? postedOrExistingJob)
        {
            if (_acceptingJobs && _existingJobs.TryAdd(job.Song.Hash, job) && _queuedJobs.TryAdd(job))
            {
                job.JobFinished += Job_OnJobFinished;
                postedOrExistingJob = job;
                return true;
            }
            else if (_existingJobs.TryGetValue(job.Song.Hash, out var existingJob))
            {
                postedOrExistingJob = existingJob;
                return false;
            }
            else
            {
                postedOrExistingJob = null;
                return false;
            }
        }

        private void Job_OnJobFinished(object sender, JobResult e)
        {
            IJob finishedJob = (IJob)sender;
            if(e?.Song == null)
            {
                Logger.log?.Warn($"Song in JobResult is null for finished job, unable to add to finished job dictionary.");
            }
            else if (e.Song.Hash == null || e.Song.Hash.Length == 0)
            {
                Logger.log?.Warn($"Hash is null for finished job, unable to add to finished job dictionary.");
            }
            else
            {
                if (!_activeJobs.TryRemove(e.Song.Hash, out _))
                {
                    Logger.log?.Warn($"Couldn't remove {finishedJob} from _activeJobs, this shouldn't happen.");
                }
                switch (e.JobState)
                {
                    case JobState.Finished:
                        _completedDownloads.TryAdd(e.Song.Hash, (IJob)sender);
                        break;
                    case JobState.Cancelled:
                        _cancelledDownloads.TryAdd(e.Song.Hash, (IJob)sender);
                        break;
                    default:
                        _failedDownloads.TryAdd(e.Song.Hash, (IJob)sender);
                        break;
                }
            }
            if (_activeJobs.Count == 0)
                _running = false;
        }

        private async void ThreadStart(object threadId)
        {
            CancellationToken cancellationToken = _cancellationSource?.Token ?? CancellationToken.None;
            int taskId = (int)threadId;
            try
            {
                foreach (var job in _queuedJobs.GetConsumingEnumerable(cancellationToken))
                {
                    if (!_activeJobs.TryAdd(job.Song.Hash, job))
                    {
                        Logger.log?.Warn($"Couldn't add {job} to _activeJobs, this shouldn't happen.");
                    }
                    await job.RunAsync(cancellationToken).ConfigureAwait(false);
                }
                _threadCompletionSource[taskId].TrySetResult(true);
            }
            catch (OperationCanceledException)
            {
                Logger.log?.Warn($"DownloadManager task {taskId} canceled.");
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Exception in DownloadManager task {taskId}:{ex.Message}");
                Logger.log?.Debug($"Exception in DownloadManager task {taskId}:{ex.StackTrace}");
            }
            _threadCompletionSource[taskId].TrySetResult(false);
        }

        public bool TryGetJob(string songHash, out IJob job)
        {
            return _existingJobs.TryGetValue(songHash, out job);
        }
    }
}
