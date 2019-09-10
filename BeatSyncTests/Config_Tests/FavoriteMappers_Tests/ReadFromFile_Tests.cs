using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Configs;
using SongFeedReaders;
using BeatSync.Playlists;
using System.IO;
using System.Linq;

namespace BeatSyncTests.Config_Tests.FavoriteMappers_Tests
{
    [TestClass]
    public class ReadFromFile_Tests
    {
        private const string DataPath = @"Data\FavoriteMappers";
        private static readonly string[] DefaultMappers = new string[] { "ConnorJC", "fafurion", "greatyazer", "zeekin", "rexxz", "fefeland" };


        [TestMethod]
        public void ExpectedInput()
        {
            var fileName = "FavoriteMappers_Expected.ini";
            var filePath = Path.Combine(DataPath, fileName);
            Assert.IsTrue(File.Exists(filePath));
            var favoriteMappers = new FavoriteMappers(filePath);
            favoriteMappers.Initialize();
            Assert.AreEqual(6, favoriteMappers.Mappers.Length);
            foreach (var mapper in DefaultMappers)
            {
                Assert.IsTrue(favoriteMappers.Mappers.Contains(mapper));
            }
        }

        [TestMethod]
        public void EmptyFile()
        {
            var fileName = "FavoriteMappers_Empty.ini";
            var filePath = Path.Combine(DataPath, fileName);
            Assert.IsTrue(File.Exists(filePath));
            var favoriteMappers = new FavoriteMappers(filePath);
            favoriteMappers.Initialize();
            Assert.AreEqual(0, favoriteMappers.Mappers.Length);
        }

        [TestMethod]
        public void NewLineAtStart()
        {
            var fileName = "FavoriteMappers_NewLineAtStart.ini";
            var filePath = Path.Combine(DataPath, fileName);
            Assert.IsTrue(File.Exists(filePath));
            var favoriteMappers = new FavoriteMappers(filePath);
            favoriteMappers.Initialize();
            Assert.AreEqual(6, favoriteMappers.Mappers.Length);
            foreach (var mapper in DefaultMappers)
            {
                Assert.IsTrue(favoriteMappers.Mappers.Contains(mapper));
            }
        }

        [TestMethod]
        public void NewLineAtEnd()
        {
            var fileName = "FavoriteMappers_NewLineAtEnd.ini";
            var filePath = Path.Combine(DataPath, fileName);
            Assert.IsTrue(File.Exists(filePath));
            var favoriteMappers = new FavoriteMappers(filePath);
            favoriteMappers.Initialize();
            Assert.AreEqual(6, favoriteMappers.Mappers.Length);
            foreach (var mapper in DefaultMappers)
            {
                Assert.IsTrue(favoriteMappers.Mappers.Contains(mapper));
            }
        }

        [TestMethod]
        public void NewLineAtMiddle()
        {
            var fileName = "FavoriteMappers_NewLineAtMiddle.ini";
            var filePath = Path.Combine(DataPath, fileName);
            Assert.IsTrue(File.Exists(filePath));
            var favoriteMappers = new FavoriteMappers(filePath);
            favoriteMappers.Initialize();
            Assert.AreEqual(6, favoriteMappers.Mappers.Length);
            foreach (var mapper in DefaultMappers)
            {
                Assert.IsTrue(favoriteMappers.Mappers.Contains(mapper));
            }
        }

        [TestMethod]
        public void PrefixedSpaces()
        {
            var fileName = "FavoriteMappers_PrefixedSpaces.ini";
            var filePath = Path.Combine(DataPath, fileName);
            Assert.IsTrue(File.Exists(filePath));
            var favoriteMappers = new FavoriteMappers(filePath);
            favoriteMappers.Initialize();
            Assert.AreEqual(6, favoriteMappers.Mappers.Length);
            foreach (var mapper in DefaultMappers)
            {
                Assert.IsTrue(favoriteMappers.Mappers.Contains(mapper));
            }
        }

        [TestMethod]
        public void PostfixedSpaces()
        {
            var fileName = "FavoriteMappers_PostfixedSpaces.ini";
            var filePath = Path.Combine(DataPath, fileName);
            Assert.IsTrue(File.Exists(filePath));
            var favoriteMappers = new FavoriteMappers(filePath);
            favoriteMappers.Initialize();
            Assert.AreEqual(6, favoriteMappers.Mappers.Length);
            foreach (var mapper in DefaultMappers)
            {
                Assert.IsTrue(favoriteMappers.Mappers.Contains(mapper));
            }
        }
    }
}
