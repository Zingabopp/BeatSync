using BeatSync.Utilities;
using BeatSync.Downloader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using WebUtilities;
using BeatSync.Playlists;
using System.Reflection;
using BeatSync.Configs;

namespace BeatSyncTests.DownloadManager_Tests
{
    [TestClass]
    public class PostJob_Tests
    {
        static PluginConfig defaultConfig;
        static PostJob_Tests()
        {
            TestSetup.Initialize();
            defaultConfig = new PluginConfig().SetDefaults();
            var userAgent = $"BeatSyncTests/{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
            SongFeedReaders.WebUtils.WebClient.SetUserAgent(userAgent);

        }

        [TestMethod]
        public void PostJob_Normal()
        {
            int concurrentDownloads = 3;
            int numJobs = 10;
            var manager = new DownloadManager(concurrentDownloads);
            var songsDir = Path.GetFullPath(@"Output\DownloadManager_Tests\PostJob_Tests");
            var jobs = GetDefaultJobs(songsDir, numJobs);
            manager.Start(CancellationToken.None);
            foreach (var job in jobs)
            {
                job.OnJobFinished += OnJobFinished_Default;
                manager.TryPostJob(job, out var postedJob);
            }
            manager.CompleteAsync().Wait();
            foreach (var job in jobs)
            {
                Assert.IsTrue(job.Result.Successful);
            }
            foreach (var job in jobs)
            {
                if (manager.TryGetJob(job.SongHash, out var foundJob))
                    Assert.IsTrue(foundJob.Result.Successful);
                else
                    Assert.Fail("Couldn't find a job");
            }
            if (manager.TryGetJob("asdfasdf", out var fakeJob))
                Assert.Fail();
            else
                Assert.IsNull(fakeJob);
        }

        [TestMethod]
        public void PostJob_Duplicate()
        {
            int concurrentDownloads = 3;
            int numJobs = 10;
            var manager = new DownloadManager(concurrentDownloads);
            var songsDir = Path.GetFullPath(@"Output\DownloadManager_Tests\PostJob_Tests");
            var jobs = GetDefaultJobs(songsDir, numJobs);
            var duplicateJobs = GetDefaultJobs(songsDir, numJobs);
            var originalJob = jobs.First();
            jobs.Insert(3, duplicateJobs.First());
            manager.Start(CancellationToken.None);
            foreach (var job in jobs)
            {
                job.OnJobFinished += OnJobFinished_Default;
                if (manager.TryPostJob(job, out var postedJob))
                    Console.WriteLine($"{job.SongHash.Substring(0, 1)} failed to post: {postedJob?.SongHash.Substring(0, 4) ?? "null"}");
                else if (postedJob != job)
                {
                    Assert.AreSame(originalJob, postedJob);
                    Console.WriteLine($"{job.SongHash.Substring(0, 1)} already exists: {postedJob?.SongHash.Substring(0, 4) ?? "null"}");
                }
                Task.Delay(50).Wait();
            }
            manager.CompleteAsync().Wait();
            bool foundDuplicateJob = false;
            foreach (var job in jobs)
            {
                if (job != duplicateJobs.First())
                    Assert.IsTrue(job.Result.Successful);
                else
                {
                    foundDuplicateJob = true;
                    Assert.AreEqual(JobStatus.NotStarted, job.Status);
                }
            }
            Assert.IsTrue(foundDuplicateJob);
        }

        [TestMethod]
        public void PostJob_Actual()
        {
            int concurrentDownloads = 3;
            var manager = new DownloadManager(concurrentDownloads);
            var songsDir = Path.GetFullPath(@"Output\DownloadManager_Tests\PostJob_Actual_Test");
            if (Directory.Exists(songsDir))
                Directory.Delete(songsDir, true);
            var song = new PlaylistSong("19f2879d11a91b51a5c090d63471c3e8d9b7aee3", "Believer", string.Empty, "rustic");
            var job = new DownloadJob(song, songsDir);
            manager.Start(CancellationToken.None);
            manager.TryPostJob(job, out var postedJob);
            manager.CompleteAsync().Wait();
            Assert.IsTrue(postedJob.Result.Successful);
            Assert.AreEqual(song.Hash, postedJob.Result.HashAfterDownload);
            foreach(var file in job.Result.ZipResult.ExtractedFiles)
            {
                Assert.IsTrue(File.Exists(file));
            }
        }

        private void OnJobFinished_Default(object sender, JobFinishedEventArgs e)
        {
            Console.WriteLine($"Job Finished at {DateTime.Now}, {e.SongHash.Substring(0, 4)}: {e.JobSuccessful}");
        }

        #region Setups
        private List<IDownloadJob> GetDefaultJobs(string songsDir, int numJobs, int startIndex = 0)
        {
            var jobs = new List<IDownloadJob>();
            for (int i = startIndex; i < startIndex + numJobs; i++)
            {
                var songHash = $"{i}".PadRight(40, '-');
                var path = Path.Combine(songsDir, i.ToString());
                var job = GetJob_Successful(songHash, path);
                jobs.Add(job);
            }
            return jobs;
        }

        private MockDownloadJob GetJob_Successful(string songHash, string songDir)
        {
            var downloadResult = new DownloadResult(@"C:\Test", DownloadResultStatus.Success, 200);
            var zipResult = new ZipExtractResult() { ResultStatus = ZipExtractResultStatus.Success };
            var finalResult = new JobResult() { SongHash = songHash, DownloadResult = downloadResult, ZipResult = zipResult, SongDirectory = songDir };
            var job = new MockDownloadJob(songHash, finalResult);
            return job;
        }

        private MockDownloadJob GetJob_FailedDownload_NetFailed(string songHash, string songDir)
        {
            Exception downloadException = new WebClientException("404: Not Found");
            DownloadResult downloadResult = new DownloadResult(@"C:\Test", DownloadResultStatus.NetFailed, 404, "404: Not Found", downloadException);
            ZipExtractResult zipResult = null;
            var finalResult = new JobResult() { SongHash = songHash, DownloadResult = downloadResult, ZipResult = zipResult, SongDirectory = songDir };
            var job = new MockDownloadJob(songHash, finalResult);
            return job;
        }

        private MockDownloadJob GetJob_FailedDownload_IOFailed(string songHash, string songDir)
        {
            Exception downloadException = new IOException();
            DownloadResult downloadResult = new DownloadResult(@"C:\Test", DownloadResultStatus.IOFailed, 200, "IOFailed", downloadException);
            ZipExtractResult zipResult = null;
            var finalResult = new JobResult() { SongHash = songHash, DownloadResult = downloadResult, ZipResult = zipResult, SongDirectory = songDir };
            var job = new MockDownloadJob(songHash, finalResult);
            return job;
        }

        private MockDownloadJob GetJob_FailedExtraction_Destination(string songHash, string songDir)
        {
            var downloadResult = new DownloadResult(@"C:\Test", DownloadResultStatus.Success, 200);
            Exception zipException = new IOException();
            var zipResult = new ZipExtractResult() { ResultStatus = ZipExtractResultStatus.DestinationFailed, Exception = zipException };
            var finalResult = new JobResult() { SongHash = songHash, DownloadResult = downloadResult, ZipResult = zipResult, SongDirectory = null };
            var job = new MockDownloadJob(songHash, finalResult);
            return job;
        }
        private MockDownloadJob GetJob_FailedExtraction_Source(string songHash, string songDir)
        {
            var downloadResult = new DownloadResult(@"C:\Test", DownloadResultStatus.Success, 200);
            Exception zipException = new IOException();
            var zipResult = new ZipExtractResult() { ResultStatus = ZipExtractResultStatus.SourceFailed, Exception = zipException };
            var finalResult = new JobResult() { SongHash = songHash, DownloadResult = downloadResult, ZipResult = zipResult, SongDirectory = null };
            var job = new MockDownloadJob(songHash, finalResult);
            return job;
        }
        #endregion
    }
}
