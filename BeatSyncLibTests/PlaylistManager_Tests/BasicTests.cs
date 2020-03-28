using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncLib.Playlists;
using System.IO;

namespace BeatSyncLibTests.PlaylistManager_Tests
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void Create_Test()
        {
            Console.WriteLine(Path.GetTempFileName());
        }
    }
}
