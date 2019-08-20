using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using System.IO;
using BeatSync.Logging;
using System.Linq;

namespace BeatSyncTests.SongHasher_Tests
{
    
    [TestClass]
    public class LoadCachedSongHashesAsync_Test
    {
        static LoadCachedSongHashesAsync_Test()
        {
            TestSetup.Initialize();
        }
        private static readonly string SongCoreCachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\Hyperbolic Magnetism\Beat Saber\SongHashData.dat";
        private static readonly string TestCacheDir = Path.GetFullPath(Path.Combine("Data", "SongHashData"));
        private static readonly string TestSongsDir = Path.GetFullPath(Path.Combine("Data", "Songs"));

        [TestMethod]
        public void Normal()
        {
            var cachePath = Path.Combine(TestCacheDir, "SongHashData.dat");
            Assert.IsTrue(File.Exists(cachePath));
            var hasher = new SongHasher(cachePath, TestSongsDir);
            hasher.LoadCachedSongHashes();
            Assert.AreEqual(hasher.HashDictionary.Count, 8);
        }

        [TestMethod]
        public void DuplicateSong_DifferentFolders()
        {
            var cachePath = Path.Combine(TestCacheDir, "SongHashData_DuplicateSong.dat");
            Assert.IsTrue(File.Exists(cachePath));
            var hasher = new SongHasher(cachePath, TestSongsDir);
            hasher.LoadCachedSongHashes();
            Assert.AreEqual(hasher.HashDictionary.Count, 9);
            int uniqueHashes = hasher.HashDictionary.Values.Select(h => h.songHash).Distinct().Count();
            Assert.AreEqual(uniqueHashes, 8);
        }

        [TestMethod]
        public void DuplicateEntries()
        {
            // Completely ignores the duplicate entry.
            var cachePath = Path.Combine(TestCacheDir, "SongHashData_DuplicateEntries.dat");
            Assert.IsTrue(File.Exists(cachePath));
            var hasher = new SongHasher(cachePath, TestSongsDir);
            hasher.LoadCachedSongHashes();
            Assert.AreEqual(hasher.HashDictionary.Count, 8);
            int uniqueHashes = hasher.HashDictionary.Values.Select(h => h.songHash).Distinct().Count();
            Assert.AreEqual(uniqueHashes, 8);
        }

        [TestMethod]
        public void HashCacheDoesntExist()
        {
            var nonExistantCacheFile = Path.Combine(TestCacheDir, "DoesntExist.dat");
            Assert.IsFalse(File.Exists(nonExistantCacheFile));
            var hasher = new SongHasher(nonExistantCacheFile, TestSongsDir);
            hasher.LoadCachedSongHashes();
            Assert.AreEqual(hasher.HashDictionary.Count, 0);
        }

        [TestMethod]
        public void AddMissingCalledFirst()
        {
            // Completely ignores the duplicate entry.
            var cachePath = Path.Combine(TestCacheDir, "TestSongsHashData.dat");
            Assert.IsTrue(File.Exists(cachePath));
            var hasher = new SongHasher(cachePath, TestSongsDir);
            hasher.LoadCachedSongHashes();
            Assert.AreEqual(hasher.HashDictionary.Count, 6);
            int uniqueHashes = hasher.HashDictionary.Values.Select(h => h.songHash).Distinct().Count();
            Assert.AreEqual(uniqueHashes, 5);
        }
    }
}
