using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncLib.Hashing;
using System.IO;
using System.Threading.Tasks;

namespace BeatSyncLibTests.SongHasher_Tests
{

    [TestClass]
    public class GetSongHashData_Tests
    {
        private static readonly string SongCoreCachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\Hyperbolic Magnetism\Beat Saber\SongHashData.dat";
        private static readonly string TestCacheDir = Path.GetFullPath(Path.Combine("Data", "SongHashData"));
        private static readonly string TestSongsDir = Path.GetFullPath(Path.Combine("Data", "Songs"));
        private static readonly string TestSongZipsDir = Path.GetFullPath(Path.Combine("Data", "SongZips"));

        static GetSongHashData_Tests()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public void ValidDir()
        {
            var hasher = new SongHasher(@"Data\Songs");
            var songDir = @"Data\Songs\5d02 (Sail - baxter395)";
            var expectedHash = "A955A84C6974761F5E1600998C7EC202DB7810B1".ToUpper();
            var hashData = SongHasher.GetSongHashDataAsync(songDir).Result;
            Assert.AreEqual(expectedHash, hashData.songHash);
        }

        [TestMethod]
        public void MissingInfoDat()
        {
            var hasher = new SongHasher(TestSongsDir);
            var songDir = @"Data\Songs\0 (Missing Info.dat)";
            var hashData = SongHasher.GetSongHashDataAsync(songDir).Result;
            Assert.IsNull(hashData.songHash);
        }

        [TestMethod]
        public void MissingExpectedDifficultyFile()
        {
            var hasher = new SongHasher(TestSongsDir);
            var songDir = @"Data\Songs\0 (Missing ExpectedDiff)";
            var hashData = SongHasher.GetSongHashDataAsync(songDir).Result;
            Assert.IsNotNull(hashData.songHash);
        }

        [TestMethod]
        public void ZipHash_NoFile()
        {
            string expectedHash = null;
            string zipPath = Path.Combine(TestSongZipsDir, "DoesntExist.zip");
            string actualHash = SongHasher.GetZippedSongHash(zipPath);
            Assert.AreEqual(expectedHash, actualHash);
        }

        [TestMethod]
        public void ZipMissingInfo()
        {
            string expectedHash = null;
            string zipPath = Path.Combine(TestSongZipsDir, "MissingInfo.zip");
            string actualHash = SongHasher.GetZippedSongHash(zipPath);
            Assert.AreEqual(expectedHash, actualHash);
        }

        [TestMethod]
        public void ZipMissingDiff()
        {
            string expectedHash = "BD8CB1F979B29760D4B623E65A59ACD217F093F3";
            string zipPath = Path.Combine(TestSongZipsDir, "MissingDiff.zip");
            string actualHash = SongHasher.GetZippedSongHash(zipPath);
            Assert.AreEqual(expectedHash, actualHash);
        }

        [TestMethod]
        public void HashSongZip()
        {
            string expectedHash = "ea2d289fb640ce8a0d7302ae36bfa3a5710d9ee8".ToUpper();
            string zipPath = Path.Combine(TestSongZipsDir, "2cd (Yee - katiedead).zip");
            long quickHash = SongHasher.GetQuickZipHash(zipPath);
            string actualHash = SongHasher.GetZippedSongHash(zipPath);
            Assert.AreEqual(expectedHash, actualHash);
        }

        [TestMethod]
        public async Task DirectoryDoesntExist()
        {
            var songDir = Path.GetFullPath(@"Data\DoesntExistSongs");
            var hasher = new SongHasher(TestSongsDir);
            try
            {
                await SongHasher.GetSongHashDataAsync(songDir).ConfigureAwait(false);
                Assert.Fail("Should have thrown exception.");
            }
            catch (DirectoryNotFoundException)
            {

            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected {nameof(DirectoryNotFoundException)} but threw {ex.GetType().Name}");
            }
        }
    }
}
