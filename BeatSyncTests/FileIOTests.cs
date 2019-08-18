using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Playlists;
using System.IO;
using BeatSync.Utilities;
using System.IO.Compression;
namespace BeatSyncTests
{
    [TestClass]
    public class FileIO_Tests
    {
        [TestMethod]
        public void GetFilePath_Test()
        {
            var test = FileIO.GetPlaylistFilePath("BeatSyncBSaberBookmarks");
            var sep = Path.DirectorySeparatorChar;
            var alt = Path.AltDirectorySeparatorChar;
        }

        [TestMethod]
        public void ExtractZip_DuplicateFileNames()
        {
            string zipPath = "TeoTest.zip";
            string extractPath = "TeoTest";
            string filePath = Path.Combine(extractPath, zipPath);
            var thing = Directory.GetParent(filePath);
            bool overwrite = false;
            
            var extractedFiles = FileIO.ExtractZipAsync(zipPath, extractPath, false, overwrite).Result;
        }
    }
}
