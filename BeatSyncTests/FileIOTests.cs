using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Playlists;

namespace BeatSyncTests
{
    [TestClass]
    public class FileIO_Tests
    {
        [TestMethod]
        public void GetFilePath_Test()
        {
            var test = FileIO.GetFilePath("asdfasdf");
        }
    }
}
