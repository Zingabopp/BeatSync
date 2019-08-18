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
            Directory.CreateDirectory("Test");
            Console.Write($"Directory created at {Path.GetFullPath("Test")}");
            var sep = Path.DirectorySeparatorChar;
            var alt = Path.AltDirectorySeparatorChar;
        }

        [TestMethod]
        public void ExtractZip_DuplicateFileNames()
        {
            string zipPath = "5d28.zip";
            string songsPath = @"H:\SteamApps\steamapps\common\Beat Saber\Beat Saber_Data\CustomLevels";
            string songDir = "5d28 ([Anniversary] Millionaire (ft. Nelly  Alan Walker Remix) - Cash Cash & Digital Farm Animals (StyngMe & Skyler Wallace) - StyngMe & Skyler Wallace)";
            string extractPath = Path.Combine(songsPath, songDir);
            var extractedFiles = FileIO.ExtractZipAsync(zipPath, extractPath, "5d28", false, false).Result;
        }

        [TestMethod]
        public void GetValidPath_PathAlreadyValid()
        {
            string zipPath = "5d28.zip";
            string songsPath = @"H:\SteamApps";
            string songDir = "5d28";
            string extractPath = Path.Combine(songsPath, songDir);
            int longestEntryLength = 40;
            Assert.AreEqual(extractPath, FileIO.GetValidPath(extractPath, longestEntryLength, 0));
        }

        [TestMethod]
        public void GetValidPath_PathTooLong()
        {
            string zipPath = "5d28.zip";
            string songsPath = @"H:\SteamApps";
            string songDir = "5d28";
            string extractPath = Path.Combine(songsPath, songDir);
            int longestEntryLength = FileIO.MaxFileSystemPathLength - extractPath.Length + songDir.Length;
            Assert.ThrowsException<PathTooLongException>(() => FileIO.GetValidPath(extractPath, longestEntryLength, 0));
        }

        [TestMethod]
        public void GetValidPath_PathTooLongWithBuffer()
        {
            string zipPath = "5d28.zip";
            string songsPath = @"H:\SteamApps";
            string songDir = "5d28";
            string extractPath = Path.Combine(songsPath, songDir);
            int bufferLength = 2;
            int longestEntryLength = FileIO.MaxFileSystemPathLength - extractPath.Length + songDir.Length - bufferLength;
            Assert.ThrowsException<PathTooLongException>(() => FileIO.GetValidPath(extractPath, longestEntryLength, bufferLength));
        }
    }
}
