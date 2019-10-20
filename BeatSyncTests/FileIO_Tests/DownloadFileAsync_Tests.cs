using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using BeatSync.Utilities;
using BeatSync;
using BeatSync.Logging;
using System.IO.Compression;
using System.Linq;
using System.Security.AccessControl;
using static BeatSync.Utilities.FileIO;
using System.Reflection;

namespace BeatSyncTests.FileIO_Tests
{
    [TestClass]
    public class DownloadFileAsync_Tests
    {
        static DownloadFileAsync_Tests()
        {
            TestSetup.Initialize();
            var userAgent = $"BeatSyncTests/{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
            SongFeedReaders.WebUtils.WebClient.SetUserAgent(userAgent);
            SongFeedReaders.WebUtils.WebClient.Timeout = 10000;
            //SongFeedReaders.WebUtils.Initialize(new WebUtilities.HttpClientWrapper.HttpClientWrapper(userAgent));
        }
        private static readonly string DownloadPath = Path.Combine("Data", "DownloadFileAsync");
        private const string DownloadUrlPrefix = "https://beatsaver.com/api/download/hash/";

        [TestMethod]
        public void NotFound()
        {
            string hash = "d0b7e3b9700ac39ff07a43e98f9c007b81313aa6";
            string key = "d0b7";
            Uri uri = new Uri(DownloadUrlPrefix + hash);
            string downloadPath = Path.Combine(DownloadPath, "NotFound", key);
            DownloadResult result = DownloadFileAsync(uri, downloadPath, true).Result;
            Assert.AreEqual(DownloadResultStatus.NetNotFound, result.Status);
        }

    }
}
