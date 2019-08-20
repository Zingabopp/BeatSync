using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;

namespace BeatSyncTests.HistoryManager_Tests
{
    [TestClass]
    public class Collection_Tests
    {
        static Collection_Tests()
        {
            TestSetup.Initialize();
        }
        [TestMethod]
        public void FileDoesntExist()
        {
            Console.WriteLine("LoadCachedSongHashesAsync_FileDoesntExist");
        }
    }
}
