using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Logging;
using BeatSync.Playlists;
using BeatSync.Configs;
using BeatSync.Downloader;
using System.IO;
using System.Reflection;
using BeatSync.Utilities;
using System.Threading;

namespace BeatSyncTests.SongDownloader_Tests
{
    [TestClass]
    public class DownloadJob_Tests
    {
        
        static DownloadJob_Tests()
        {
            TestSetup.Initialize();
            defaultConfig = new PluginConfig().SetDefaults();
            var userAgent = $"BeatSyncTests/{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
            SongFeedReaders.WebUtils.WebClient.SetUserAgent(userAgent);
            SongFeedReaders.WebUtils.WebClient.Timeout = 10000;
            //SongFeedReaders.WebUtils.Initialize(new WebUtilities.HttpClientWrapper.HttpClientWrapper(userAgent));

        }

        public static PluginConfig defaultConfig;
        public static string DefaultHistoryPath = Path.GetFullPath(Path.Combine("Output", "DownloadJob_Tests", "BeatSyncHistory.json"));
        public static string DefaultHashCachePath = Path.GetFullPath(Path.Combine("Output", "DownloadJob_Tests", "hashData.dat"));
        public static string DefaultSongsPath = Path.GetFullPath(Path.Combine("Output", "DownloadJob_Tests", "Songs"));
        
        [TestMethod]
        public void SongDoesntExist()
        {
            var downloadManager = new DownloadManager(1);
            downloadManager.Start(CancellationToken.None);
            var doesntExist = new PlaylistSong("196be1af64958d8b5375b328b0eafae2151d46f8", "Doesn't Exist", "ffff", "Who knows");
            var job = new DownloadJob(doesntExist, DefaultSongsPath);
            Assert.IsTrue(downloadManager.TryPostJob(job, out var postedJob));
            downloadManager.CompleteAsync().Wait();
            Assert.AreEqual(DownloadResultStatus.NetNotFound, postedJob.Result.DownloadResult.Status);
            Assert.AreEqual(404, postedJob.Result.DownloadResult.HttpStatusCode);
            Assert.AreEqual(JobStatus.Finished, postedJob.Status);
            Assert.IsNull(postedJob.Result.ZipResult);
            Assert.IsFalse(postedJob.Result.Successful);
            Assert.AreEqual(null, postedJob.Result.HashAfterDownload);
        }

        [TestMethod]
        public void SongDoesExist()
        {
            var downloadManager = new DownloadManager(1);
            downloadManager.Start(CancellationToken.None);
            var existingSong = new PlaylistSong("d375405d047d6a2a4dd0f4d40d8da77554f1f677", "Does Exist", "5e20", "ejiejidayo");
            var job = new DownloadJob(existingSong, DefaultSongsPath);
            Assert.IsTrue(downloadManager.TryPostJob(job, out var postedJob));
            downloadManager.CompleteAsync().Wait();
            Assert.AreEqual(JobStatus.Finished, postedJob.Status);
            Assert.AreEqual(DownloadResultStatus.Success, postedJob.Result.DownloadResult.Status);
            Assert.AreEqual(ZipExtractResultStatus.Success, postedJob.Result.ZipResult.ResultStatus);
            Assert.IsTrue(postedJob.Result.Successful);
            Assert.AreEqual(existingSong.Hash, postedJob.Result.HashAfterDownload);
        }
    }
}
