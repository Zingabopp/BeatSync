using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Logging;

namespace BeatSyncTests.SongDownloader_Tests
{
    [TestClass]
    public class DownloadJob_Tests
    {
        static DownloadJob_Tests()
        {
            TestSetup.Initialize();
        }
        [TestMethod]
        public void Normal()
        {
            Console.WriteLine("DownloadJob_Normal");
        }
    }
}
