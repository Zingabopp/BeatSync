using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncLib;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BeatSyncLib.Playlists;
using BeatSyncLib.History;
using static BeatSyncLibTests.HistoryManager_Tests.HistoryTestData;

namespace BeatSyncLibTests.HistoryManager_Tests
{
    [TestClass]
    public class Initialize_Tests
    {
        static Initialize_Tests()
        {
            TestSetup.Initialize();
        }

        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Data", "HistoryManager"));
        private static readonly string HistoryTestFilePath = Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json");

        [TestMethod]
        public void Constructor_Default()
        {
            var historyManager = new HistoryManager(HistoryTestFilePath);
            Assert.AreEqual(HistoryTestFilePath, historyManager.HistoryPath);
            Assert.AreEqual(0, historyManager.Count);
        }

        [TestMethod]
        public void Constructor_NullPath()
        {
            string path = null;
            Assert.ThrowsException<ArgumentNullException>(() =>  new HistoryManager(path));
        }

        [TestMethod]
        public void Constructor_WithPath()
        {
            string path = Path.Combine(@"Data\HistoryManager\BeatSyncHistory-TestCol1.json");
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            Assert.AreEqual(Path.GetFullPath(path), historyManager.HistoryPath);
            Assert.AreEqual(8, historyManager.Count);
        }

        [TestMethod]
        public void Initialize_ExistingFile()
        {
            var fileName = "BeatSyncHistory-TestCol1.json";
            var filePath = Path.Combine(HistoryTestPathDir, fileName);
            Directory.CreateDirectory(HistoryTestPathDir);
            var historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            Assert.AreEqual(historyManager.Count, 8);
        }

        [TestMethod]
        public void Initialize_FileDoesntExist()
        {
            var fileName = "BeatSyncHistory-DoesntExist.json";
            var filePath = Path.Combine(HistoryTestPathDir, fileName);
            var historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            Assert.AreEqual(historyManager.Count, 0);
        }

        [TestMethod]
        public void Initialize_SecondCall_Parameterless()
        {
            var fileName = "BeatSyncHistory-TestCol1.json";
            var filePath = Path.Combine(HistoryTestPathDir, fileName);
            Directory.CreateDirectory(HistoryTestPathDir);
            var historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            var songToAdd = TestCollection2.First();
            historyManager.TryAdd(songToAdd.Key, songToAdd.Value.SongInfo, songToAdd.Value.Flag);
            Assert.AreEqual(9, historyManager.Count);
            historyManager.Initialize();
            Assert.AreEqual(9, historyManager.Count);
        }
    }
}
