using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using System.IO;
using BeatSync.Downloader;
using BeatSync.Utilities;
using System.Collections.Generic;
using BeatSync.Playlists;
using System.Linq;
using BeatSyncTests.DownloadManager_Tests;

namespace BeatSyncTests.SongDownloader_Tests
{
    [TestClass]
    public class ProcessJob_Tests
    {
        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Data", "HistoryManager"));
        private static readonly string TestSongsDir = Path.GetFullPath(Path.Combine("Data", "Songs"));
        private static readonly string TestCacheDir = Path.GetFullPath(Path.Combine("Data", "SongHashData"));

        private static Dictionary<string, HistoryEntry> TestCollection1 = new Dictionary<string, HistoryEntry>()
        {
            {"EF1A4AC10D2E271D6B95D7FEB773D1F387F28525", new HistoryEntry("Missing ExpectedDiff", HistoryFlag.Missing) },
            {"C1C8E2B9394050AFAD435608137941DA0B64B8F3", new HistoryEntry("Super Mario Bros Theme - redknight", 0) },
            {"310694F2FF8D129D4E64192251653CAFFDC65B62", new HistoryEntry("New Game - Nitro Fun - Fafurion", 0) },
            {"05FF1B7ECDD5089E5EAFC1D8474680E448A017DD", new HistoryEntry("The Diary of Jane - Breaking Benjamin - Heisenberg", 0) },
            {"D6F3F15484FE169F4593718F50EF6D049FCAA72E", new HistoryEntry("Sail - baxter395", 0) }
        };

        public static SongDownloader GetDefaultDownloader()
        {
            var hasher = new SongHasher(TestSongsDir, Path.Combine(TestCacheDir, "TestSongsHashData.dat"));

            var history = new HistoryManager(Path.Combine(HistoryTestPathDir, "TestSongsHistory.json"));
            history.Initialize();
            var downloader = new SongDownloader(BeatSync.Configs.PluginConfig.DefaultConfig, history, hasher, TestSongsDir);
            return downloader;
        }

        /// <summary>
        /// Not found on Beat Saver, song kept in history with the 'NotFound' flag and removed from all playlists.
        /// </summary>
        [TestMethod]
        public void BeatSaverNotFound()
        {
            //Assert.Fail("Need to fix after refactor");
            var downloader = GetDefaultDownloader();
            var testHash = "ASDFLKJACVOIAOSICJVLKXCVJ";

            var song = new PlaylistSong(testHash, "Missing ExpectedDiff", "fff1", "Other");
            downloader.HistoryManager.TryAdd(song, 0);
            var testDownloadResult = new DownloadResult(song.Hash, DownloadResultStatus.NetNotFound, 404);
            ZipExtractResult testZipResult = null;
            var jobResult = new JobResult()
            {
                DownloadResult = testDownloadResult,
                ZipResult = testZipResult,
                SongHash = song.Hash,
                //Song = song,
                SongDirectory = null
            };
            var testJob = new MockDownloadJob(testHash, jobResult, false);
            testJob.RunAsync().Wait();
            var recentPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncRecent);
            recentPlaylist.TryAdd(song);
            var bSaberPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeastSaberCurator);
            bSaberPlaylist.TryAdd(song);
            // Verify setup
            Assert.IsTrue(recentPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(bSaberPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(downloader.HistoryManager.TryGetValue(song.Hash, out var entry));
            Assert.AreEqual(HistoryFlag.None, entry.Flag);

            // Run test
            downloader.ProcessJob(testJob);
            Assert.IsTrue(downloader.HistoryManager.TryGetValue(song.Hash, out entry));
            Assert.AreEqual(HistoryFlag.BeatSaverNotFound, entry.Flag);
            Assert.IsFalse(recentPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsFalse(bSaberPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
        }

        /// <summary>
        /// Not found on Beat Saver, song kept in history with the 'NotFound' flag and removed from all playlists.
        /// </summary>
        [TestMethod]
        public void DownloadFailed_IO()
        {
            var downloader = GetDefaultDownloader();
            var testHash = "ASDFLKJACVOIAOSICJVLKXCVJ";

            var song = new PlaylistSong(testHash, "Missing ExpectedDiff", "fff1", "Other");
            downloader.HistoryManager.TryAdd(song, 0);
            var testDownloadResult = new DownloadResult(song.Hash, DownloadResultStatus.IOFailed, 200);
            ZipExtractResult testZipResult = null;
            var jobResult = new JobResult()
            {
                DownloadResult = testDownloadResult,
                ZipResult = testZipResult,
                SongHash = song.Hash,
                //Song = song,
                SongDirectory = null
            };
            var testJob = new MockDownloadJob(testHash, jobResult, false);
            testJob.RunAsync().Wait();
            var recentPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncRecent);
            recentPlaylist.TryAdd(song);
            var bSaberPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeastSaberCurator);
            bSaberPlaylist.TryAdd(song);
            // Verify setup
            Assert.IsTrue(recentPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(bSaberPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(downloader.HistoryManager.TryGetValue(song.Hash, out var entry));
            Assert.AreEqual(HistoryFlag.None, entry.Flag);

            // Run test
            downloader.ProcessJob(testJob);
            Assert.IsTrue(downloader.HistoryManager.TryGetValue(song.Hash, out entry));
            Assert.AreEqual(HistoryFlag.Error, entry.Flag);
            Assert.IsTrue(recentPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(bSaberPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
        }

        /// <summary>
        /// Job successful, song in history with 'Downloaded' flag.
        /// </summary>
        [TestMethod]
        public void Successful()
        {
            var downloader = GetDefaultDownloader();
            var testHash = "A7SDF6A86SDF9ASDF";
            var song = new PlaylistSong(testHash, "Successful Song", "fff2", "Other");
            var songPath = Path.Combine(TestSongsDir, "fff2 (Successful Song)");
            downloader.HistoryManager.TryAdd(song, 0);

            var testDownloadResult = new DownloadResult(songPath, DownloadResultStatus.Success, 200);
            ZipExtractResult testZipResult = new ZipExtractResult()
            {
                CreatedOutputDirectory = true,
                Exception = null,
                OutputDirectory = songPath,
                ExtractedFiles = new string[] { "info.dat", "expert.dat", "song.egg" },
                ResultStatus = ZipExtractResultStatus.Success,
                SourceZip = "fff2.zip"
            };
            var jobResult = new JobResult()
            {
                DownloadResult = testDownloadResult,
                ZipResult = testZipResult,
                SongHash = song.Hash,
                //Song = song,
                SongDirectory = null
            };
            var testJob = new MockDownloadJob(testHash, jobResult, false);
            testJob.RunAsync().Wait();
            var recentPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncRecent);
            recentPlaylist.TryAdd(song);
            var bSaberPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeastSaberCurator);
            bSaberPlaylist.TryAdd(song);
            // Verify setup
            Assert.IsTrue(recentPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(bSaberPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(downloader.HistoryManager.TryGetValue(song.Hash, out var entry));
            Assert.AreEqual(HistoryFlag.None, entry.Flag);

            // Run test
            downloader.ProcessJob(testJob);
            Assert.IsTrue(downloader.HistoryManager.TryGetValue(song.Hash, out entry));
            Assert.AreEqual(HistoryFlag.Downloaded, entry.Flag);
            Assert.IsTrue(recentPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(bSaberPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
        }

        /// <summary>
        /// Extraction failed due to problem with destination, song removed from history.
        /// </summary>
        [TestMethod]
        public void ZipDestinationFailed()
        {
            var downloader = GetDefaultDownloader();
            var testHash = "A7SDF6A86SDF9ASDF";

            var song = new PlaylistSong(testHash, "Successful Song", "fff2", "Other");
            var songPath = Path.Combine(TestSongsDir, "fff2 (Successful Song)");
            downloader.HistoryManager.TryAdd(song, 0);

            var testDownloadResult = new DownloadResult(songPath, DownloadResultStatus.Success, 200);
            ZipExtractResult testZipResult = new ZipExtractResult()
            {
                CreatedOutputDirectory = true,
                Exception = null,
                OutputDirectory = songPath,
                ExtractedFiles = new string[] { "info.dat", "expert.dat", "song.egg" },
                ResultStatus = ZipExtractResultStatus.DestinationFailed,
                SourceZip = "fff2.zip"
            };
            var jobResult = new JobResult()
            {
                DownloadResult = testDownloadResult,
                ZipResult = testZipResult,
                SongHash = song.Hash,
                //Song = song,
                SongDirectory = null
            };
            var testJob = new MockDownloadJob(testHash, jobResult, false);
            testJob.RunAsync().Wait();
            var recentPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeatSyncRecent);
            recentPlaylist.TryAdd(song);
            var bSaberPlaylist = PlaylistManager.GetPlaylist(BuiltInPlaylist.BeastSaberCurator);
            bSaberPlaylist.TryAdd(song);
            // Verify setup
            Assert.IsTrue(recentPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(bSaberPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(downloader.HistoryManager.TryGetValue(song.Hash, out var entry));
            Assert.AreEqual(HistoryFlag.None, entry.Flag);

            // Run tests
            downloader.ProcessJob(testJob);
            Assert.IsTrue(downloader.HistoryManager.TryGetValue(song.Hash, out entry));
            Assert.AreEqual(HistoryFlag.Error, entry.Flag);
            Assert.IsTrue(recentPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
            Assert.IsTrue(bSaberPlaylist.Beatmaps.Any(s => song.Hash.Equals(s.Hash)));
        }

    }
}
