using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using System.IO;
using System.Linq;

namespace BeatSyncTests.SongHasher_Tests
{
    [TestClass]
    public class AddMissingHashes_Tests
    {
        private static readonly string SongCoreCachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\Hyperbolic Magnetism\Beat Saber\SongHashData.dat";
        private static readonly string TestCacheDir = Path.GetFullPath(Path.Combine("Data", "SongHashData"));
        private static readonly string TestSongsDir = Path.GetFullPath(Path.Combine("Data", "Songs"));

        static AddMissingHashes_Tests()
        {
            TestSetup.Initialize();
        }

        //[TestMethod]
        public void BigTest()
        {
            var hasher = new SongHasher();
            hasher.LoadCachedSongHashes();
            var songPath = new DirectoryInfo(hasher.HashDictionary.Keys.First());
            songPath = songPath.Parent;
            hasher = new SongHasher(songPath.FullName);
            hasher.LoadCachedSongHashes();
            var newHashes = hasher.AddMissingHashes();
            Console.WriteLine($"Hashed {newHashes} new songs");
        }

        [TestMethod]
        public void HashCacheDoesntExist()
        {
            var nonExistantCacheFile = Path.Combine(TestCacheDir, "DoesntExist.dat");
            var hasher = new SongHasher(TestSongsDir, nonExistantCacheFile);
            var newHashes = hasher.AddMissingHashes();
            Assert.AreEqual(newHashes, 6);
        }

        [TestMethod]
        public void HashAllSongs()
        {
            var cacheFile = Path.Combine(TestCacheDir, "TestSongsHashData.dat");
            var hasher = new SongHasher(TestSongsDir, cacheFile);
            var newHashes = hasher.AddMissingHashes();
            Assert.AreEqual(newHashes, 6);
        }

        [TestMethod]
        public void AfterFullCacheCoverage()
        {
            var cacheFile = Path.Combine(TestCacheDir, "TestSongsHashData.dat");
            var hasher = new SongHasher(TestSongsDir, cacheFile);
            hasher.LoadCachedSongHashes();
            var newHashes = hasher.AddMissingHashes();
            Assert.AreEqual(newHashes, 0);
        }

        [TestMethod]
        public void AfterPartialCacheCoverage()
        {
            var cacheFile = Path.Combine(TestCacheDir, "TestSongsHashData_Partial.dat");
            var hasher = new SongHasher(TestSongsDir, cacheFile);
            hasher.LoadCachedSongHashes();
            var newHashes = hasher.AddMissingHashes();
            Assert.AreEqual(newHashes, 2);
        }

        [TestMethod]
        public void EmptyDirectory()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void DirectoryDoesntExist()
        {
            throw new NotImplementedException();
        }
    }
}
