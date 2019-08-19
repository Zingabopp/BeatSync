using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Logging;

namespace BeatSyncTests.SongDownloader_Tests
{
    [TestClass]
    public class RunDownloaderAsync_Tests
    {
        static RunDownloaderAsync_Tests()
        {
            TestSetup.Initialize();
        }
        [TestMethod]
        public void Normal()
        {
            Console.WriteLine("RunDownloaderAsync_Normal");
        }
    }
}
