using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using System.IO;
using BeatSync.Downloader;
using BeatSync.Utilities;
using System.Collections.Generic;
using BeatSync.Playlists;
using System.Linq;

namespace BeatSyncTests.BeatSync_Tests
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
            {"C1C8E2B9394050AFAD435608137941DA0B64B8F3", new HistoryEntry("Super Mario Bros Theme - redknight", 0) },
            {"D6F3F15484FE169F4593718F50EF6D049FCAA72E", new HistoryEntry("Sail - baxter395", 0) }
        };

        public static BeatSync.BeatSync GetDefaultDownloader()
        {
            var beatSync = new BeatSync.BeatSync()
            {
                SongHasher = new SongHasher(TestSongsDir, Path.Combine(TestCacheDir, "TestSongsHashData.dat")),
                HistoryManager = new HistoryManager(Path.Combine(HistoryTestPathDir, "TestSongsHistory.json"))
            };
            return beatSync;
        }

        [TestMethod]
        public void BeatSaverNotFound()
        {
            var beatSync = GetDefaultDownloader();
            var histEntry = TestCollection1.First();

            var song = new PlaylistSong(histEntry.Key, "Missing ExpectedDiff", "fff1", "Other");
            var testDownloadResult = new DownloadResult(song.Hash, DownloadResultStatus.NetFailed, 404);
            ZipExtractResult testZipResult = null;
            var testJob = new JobResult()
            {
                DownloadResult = testDownloadResult,
                BeatSaverHash = song.Hash,
                Successful = false,
                Song = song,
                SongDirectory = null
            };
            beatSync.ProcessJob(testJob);
        }
    }
}
