using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Playlists;
using System.IO;

namespace BeatSyncTests
{
    [TestClass]
    public class FileIO_Tests
    {
        [TestMethod]
        public void GetFilePath_Test()
        {
            var test = FileIO.GetPlaylistFilePath("asdfasdf");
            var sep = Path.DirectorySeparatorChar;
            var alt = Path.AltDirectorySeparatorChar;
        }
    }
}
