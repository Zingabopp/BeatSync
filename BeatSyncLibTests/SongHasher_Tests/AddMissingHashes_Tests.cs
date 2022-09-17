﻿using BeatSaber.SongHashing;
using BeatSyncLib.Hashing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLibTests.SongHasher_Tests
{
    [TestClass]
    public class AddMissingHashes_Tests
    {
        private static readonly string SongCoreCachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\Hyperbolic Magnetism\Beat Saber\SongHashData.dat";
        private static readonly string TestCacheDir = Path.GetFullPath(Path.Combine("Data", "SongHashData"));
        private static readonly string TestSongsDir = Path.GetFullPath(Path.Combine("Data", "Songs"));

        private static readonly IBeatmapHasher BeatmapHasher = new Hasher();

        static AddMissingHashes_Tests()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public async Task HashCacheDoesntExist()
        {
            string nonExistantCacheFile = Path.Combine(TestCacheDir, "DoesntExist.dat");
            SongHasher<SongHashData> hasher = new SongHasher<SongHashData>(TestSongsDir, BeatmapHasher, TestSetup.LogFactory);
            int newHashes = await hasher.HashDirectoryAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(newHashes, 7);
        }

        [TestMethod]
        public async Task HashAllSongs()
        {
            SongHasher<SongHashData> hasher = new SongHasher<SongHashData>(TestSongsDir, BeatmapHasher, TestSetup.LogFactory);
            int newHashes = await hasher.HashDirectoryAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(newHashes, 7);
        }

        //[TestMethod]
        //public async Task AfterFullCacheCoverage()
        //{
        //    string cacheFile = Path.Combine(TestCacheDir, "TestSongsHashData.dat");
        //    SongHasher<SongHashData> hasher = new SongHasher<SongHashData>(TestSongsDir);
        //    int newHashes = await hasher.HashDirectoryAsync().ConfigureAwait(false);
        //    Assert.AreEqual(newHashes, 0);
        //}

        //[TestMethod]
        //public async Task AfterPartialCacheCoverage()
        //{
        //    string cacheFile = Path.Combine(TestCacheDir, "TestSongsHashData_Partial.dat");
        //    SongHasher<SongHashData> hasher = new SongHasher<SongHashData>(TestSongsDir);
        //    int newHashes = await hasher.HashDirectoryAsync().ConfigureAwait(false);
        //    Assert.AreEqual(newHashes, 2);
        //}

        [TestMethod]
        public async Task EmptyDirectory()
        {
            string emptyDir = Path.Combine(TestSongsDir, "EmptyDirectory");
            DirectoryInfo dInfo = new DirectoryInfo(emptyDir);
            dInfo.Create();
            Assert.AreEqual(0, dInfo.GetFiles().Count());
            SongHasher<SongHashData> hasher = new SongHasher<SongHashData>(emptyDir, BeatmapHasher, TestSetup.LogFactory);
            int newHashes = await hasher.HashDirectoryAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(0, newHashes);

            //Clean up
            if (dInfo.Exists)
                dInfo.Delete(true);
        }

        //[TestMethod]
        //public void TestDirectoryHash()
        //{
        //    string directory = "H:\\SteamApps\\steamapps\\common\\Beat Saber\\Beat Saber_Data\\CustomLevels\\6f11 (Leave The Lights On (KROT Remix) - Rigid)";
        //    long dirHash = 987761419790609;
        //    long calcHash = SongHasher.GetDirectoryHash(directory);
        //    Assert.AreEqual(dirHash, calcHash);
        //}

        [TestMethod]
        public async Task DirectoryDoesntExist()
        {
            string nonExistantDir = Path.Combine(TestSongsDir, "DoesntExist");
            if (Directory.Exists(nonExistantDir))
                Directory.Delete(nonExistantDir, true);
            Assert.IsFalse(Directory.Exists(nonExistantDir));
            SongHasher<SongHashData> hasher = new SongHasher<SongHashData>(nonExistantDir, BeatmapHasher, TestSetup.LogFactory);
            try
            {
                await hasher.HashDirectoryAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Fail("Should have thrown exception.");
            }
            catch (DirectoryNotFoundException)
            {

            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected {nameof(DirectoryNotFoundException)} but threw {ex.GetType().Name}");
            }
            if (Directory.Exists(nonExistantDir))
                Directory.Delete(nonExistantDir, true);
        }
    }
}
