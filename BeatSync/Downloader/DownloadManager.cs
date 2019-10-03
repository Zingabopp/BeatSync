using BeatSync.Playlists;
using BeatSync.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSync.Downloader
{
    public class DownloadManager
    {

        private BlockingCollection<IDownloadJob> _queuedJobs = new BlockingCollection<IDownloadJob>();
        private ConcurrentDictionary<string, IDownloadJob> _downloadResults = new ConcurrentDictionary<string, IDownloadJob>();
        public IReadOnlyList<IDownloadJob> CompletedJobs
        {
            get { return _downloadResults.Values.Where(j => j.Status == JobStatus.Finished).ToList(); }
        }
        private bool _acceptingJobs = false;
        private bool _running = false;
        private int _concurrentDownloads = 1;
        private CancellationTokenSource _cancellationSource;
        private Task[] _tasks;

        public int ConcurrentDownloads
        {
            get { return _concurrentDownloads; }
            private set
            {
                if (value < 1)
                    value = 1;
                if (value == _concurrentDownloads)
                    return;
                _concurrentDownloads = value;
            }
        }

        public DownloadManager(int concurrentDownloads)
        {
            ConcurrentDownloads = concurrentDownloads;
            _acceptingJobs = true;
            _tasks = new Task[ConcurrentDownloads];
        }

        public void Start()
        {
            if (_running)
                return;
            _running = true;
            if (_cancellationSource == null || _cancellationSource.IsCancellationRequested)
                _cancellationSource = new CancellationTokenSource();
            for (int i = 0; i < ConcurrentDownloads; i++)
            {
                int taskId = i; // Apparently using 'i' directly for HandlerStartAsync doesn't work well...
                _tasks[i] = Task.Run(() => HandlerStartAsync(_cancellationSource.Token, taskId));
            }
        }

        public void Stop()
        {
            //_queuedJobs.CompleteAdding();
            _running = false;
            _cancellationSource.Cancel();
        }

        public async Task StopAsync()
        {
            Stop();
            await Task.WhenAll(_tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Complete()
        {
            // TODO: _running isn't set to false with this.
            _acceptingJobs = false;
            _queuedJobs.CompleteAdding();
        }

        public async Task CompleteAsync()
        {
            Complete();
            try
            {
                await Task.WhenAll(_tasks).ConfigureAwait(false);
            }
            catch (Exception) { }
            _running = false;
        }

        /// <summary>
        /// Tries to post a new job to the queue. If the song was already downloaded, returns the existing DownloadJob. 
        /// Returns null if the job failed to post and the song wasn't previously downloaded.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        public bool TryPostJob(IDownloadJob job, out IDownloadJob postedJob)
        {
            if (_acceptingJobs)
            {
                if (_downloadResults.TryAdd(job.SongHash, job) && _queuedJobs.TryAdd(job))
                {
                    postedJob = job;
                    return true;
                }
            }
            if (_downloadResults.TryGetValue(job.SongHash, out var existingJob))
            {
                postedJob = existingJob;
                return false;
            }
            else
            {
                postedJob = null;
                return false;
            }
        }

        private async Task HandlerStartAsync(object cancellationToken, int taskId)
        {
            if (!(cancellationToken is CancellationToken token))
                throw new ArgumentException("OnHandlerStart parameter must be a CancellationToken", nameof(cancellationToken));
            try
            {
                foreach (var job in _queuedJobs.GetConsumingEnumerable(token))
                {
                    await job.RunAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Exception in DownloadManager task {taskId}:{ex.Message}");
                Logger.log?.Debug($"Exception in DownloadManager task {taskId}:{ex.StackTrace}");
            }
        }

        public bool TryGetJob(string songHash, out IDownloadJob job)
        {
            return _downloadResults.TryGetValue(songHash, out job);
        }



    }
}
