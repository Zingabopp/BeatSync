using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Logging;

namespace BeatSyncTests.SongDownloader_Tests
{
    [TestClass]
    public class RunReaders_Tests
    {
        static RunReaders_Tests()
        {
            TestSetup.Initialize();
        }
        [TestMethod]
        public void Normal()
        {
            Console.WriteLine("RunReaders_Normal");
        }
    }
}
