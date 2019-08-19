using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using BeatSync.Utilities;
using System.IO.Compression;
using System.Linq;
using BeatSync;
using BeatSync.Logging;

namespace BeatSyncTests.FileIO_Tests
{
    [TestClass]
    public class GetValidPath_Tests
    {
        static GetValidPath_Tests()
        {
            TestSetup.Initialize();
        }
        private static readonly string SongZipsPath = Path.Combine("Data", "SongZips");

        [TestMethod]
        public void GetValidPath_PathAlreadyValid()
        {
            string zipPath = Path.Combine(SongZipsPath, "5d28-LongEntry.zip");
            string songsPath = Environment.CurrentDirectory;
            string songDir = "5d28";
            string extractPath = Path.Combine(songsPath, songDir);
            int longestEntryLength = 40;
            Assert.AreEqual(extractPath, FileIO.GetValidPath(extractPath, longestEntryLength, 0));
        }

        [TestMethod]
        public void GetValidPath_DefaultPathTooLong()
        {
            string zipPath = Path.Combine(SongZipsPath, "5d28-LongEntry.zip");
            string songsPath = Environment.CurrentDirectory;
            string songDir = "5d28";
            string extractPath = Path.Combine(songsPath, songDir);
            int longestEntryLength = FileIO.MaxFileSystemPathLength - extractPath.Length + songDir.Length;
            Assert.ThrowsException<PathTooLongException>(() => FileIO.GetValidPath(extractPath, longestEntryLength, 0));
        }

        [TestMethod]
        public void GetValidPath_DefaultPathTooLongWithBuffer()
        {
            string zipPath = Path.Combine(SongZipsPath, "5d28-LongEntry.zip");
            int longestEntryLength = 0;
            int buffer = 4;
            using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                longestEntryLength = zipArchive.Entries.Select(e => e.Name).Max(n => n.Length);
            }
            string songsPath = Path.Combine(Environment.CurrentDirectory, "Extended-");
            string songDir = "5d28asdfasdfasdf";

            while (songsPath.Length < FileIO.MaxFileSystemPathLength - longestEntryLength - songDir.Length - buffer + 2)
            {
                songsPath += "1";
            }
            string extractPath = Path.Combine(songsPath, songDir);
            var finalPath = FileIO.GetValidPath(extractPath, longestEntryLength, buffer);
        }

        [TestMethod]
        public void GetValidPath_RootPathTooLong()
        {
            string zipPath = Path.Combine(SongZipsPath, "5d28-LongEntry.zip");
            int longestEntryLength = 0;
            using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                longestEntryLength = zipArchive.Entries.Select(e => e.Name).Max(n => n.Length);
            }
            string songsPath = Path.Combine(Environment.CurrentDirectory, "Extended-");
            string songDir = "5d28";

            while (songsPath.Length < FileIO.MaxFileSystemPathLength - longestEntryLength - 1)
            {
                songsPath += "1";
            }
            string extractPath = Path.Combine(songsPath, songDir);
            Assert.ThrowsException<PathTooLongException>(() => FileIO.GetValidPath(extractPath, longestEntryLength, 0));
        }


    }
}
