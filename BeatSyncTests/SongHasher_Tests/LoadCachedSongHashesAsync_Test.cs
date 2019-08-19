using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using System.IO;
using BeatSync.Logging;

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

        [TestMethod]
        public void Normal()
        {
            Assert.IsTrue(File.Exists(SongCoreCachePath));
            var hasher = new SongHasher();
            hasher.LoadCachedSongHashesAsync(SongCoreCachePath);
            Assert.IsTrue(hasher.HashDictionary.Count > 0);
            Console.WriteLine("LoadCachedSongHashesAsync_FileDoesntExist");
        }
    }
}
