using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Logging;

namespace BeatSyncTests.SongDownloader_Tests
{
    [TestClass]
    public class ReadFeed_Test
    {
        static ReadFeed_Test()
        {
            TestSetup.Initialize();
        }
        [TestMethod]
        public void Normal()
        {
            Console.WriteLine("ReadFeed_Normal");
        }
    }
}
