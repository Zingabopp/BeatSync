using BeatSync.Downloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSync.Utilities;
using System.Threading;

namespace BeatSyncTests.DownloadManager_Tests
{
    public class MockDownloadJob : IDownloadJob
    {
        private Random NumberGenerator = new Random();
        public string SongHash { get; private set; }
        public string SongKey { get; private set; }
        public string SongName { get; private set; }
        public string LevelAuthorName { get; private set; }
        public string SongDirectory { get; private set; }
        public Exception Exception { get; private set; }
        private WeakReference<Func<bool>> PauseFlag = new WeakReference<Func<bool>>(() => SongFeedReaders.Utilities.IsPaused);
        private bool Paused
        {
            get
            {
                if (PauseFlag == null)
                    return false;
                if (PauseFlag.TryGetTarget(out var reference))
                    return reference.Invoke();
                return false;
            }
        }
        public JobResult Result { get; private set; }

        public JobStatus Status { get; private set; }

        private JobResult _finalResult;

        public int MinDownloadTime = 50;
        public int MaxDownloadTime = 100;
        public int MinZipTime = 10;
        public int MaxZipTime = 20;

        public MockDownloadJob(string songHash, JobResult finalResult)
        {
            SongHash = songHash;
            _finalResult = finalResult;
            Result = new JobResult() { SongHash = SongHash };
            Status = JobStatus.NotStarted;
        }
        public event EventHandler<JobStartedEventArgs> OnJobStarted;
        public event EventHandler<JobFinishedEventArgs> OnJobFinished;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            Status = JobStatus.Downloading;
            int downloadTime = NumberGenerator.Next(MinDownloadTime, MaxDownloadTime);
            int extractTime = NumberGenerator.Next(MinZipTime, MaxZipTime);
            await Task.Delay(downloadTime);
            if (Paused)
                Console.WriteLine("We're 'Paused'");
            Result.DownloadResult = _finalResult.DownloadResult;
            if ((Result.DownloadResult?.Status ?? DownloadResultStatus.Unknown) == DownloadResultStatus.Success)
            { 
                Status = JobStatus.Extracting;
                await Task.Delay(extractTime);
                Result.ZipResult = _finalResult.ZipResult;
                SongDirectory = _finalResult.SongDirectory;
                Status = JobStatus.Finished;
            }
            Status = JobStatus.Finished;
            OnJobFinished?.Invoke(this, new JobFinishedEventArgs(SongHash, Result.Successful, Result.DownloadResult.Status, Result.ZipResult.ResultStatus, SongDirectory));
        }

        public  Task RunAsync()
        {
            return RunAsync(CancellationToken.None);
        }
    }
}
