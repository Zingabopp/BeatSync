using BeatSyncLib.History;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            HistoryManager historyManager = new HistoryManager(HistoryTestFilePath);
            Assert.AreEqual(HistoryTestFilePath, historyManager.HistoryPath);
            Assert.AreEqual(0, historyManager.Count);
        }

        [TestMethod]
        public void Constructor_NullPath()
        {
            string path = null;
            Assert.ThrowsException<ArgumentNullException>(() => new HistoryManager(path));
        }

        [TestMethod]
        public void Constructor_WithPath()
        {
            string path = Path.Combine(@"Data\HistoryManager\BeatSyncHistory-TestCol1.json");
            HistoryManager historyManager = new HistoryManager(path);
            historyManager.Initialize();
            Assert.AreEqual(Path.GetFullPath(path), historyManager.HistoryPath);
            Assert.AreEqual(8, historyManager.Count);
        }

        [TestMethod]
        public void Initialize_ExistingFile()
        {
            string fileName = "BeatSyncHistory-TestCol1.json";
            string filePath = Path.Combine(HistoryTestPathDir, fileName);
            Directory.CreateDirectory(HistoryTestPathDir);
            HistoryManager historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            Assert.AreEqual(historyManager.Count, 8);
        }

        [TestMethod]
        public void Initialize_FileDoesntExist()
        {
            string fileName = "BeatSyncHistory-DoesntExist.json";
            string filePath = Path.Combine(HistoryTestPathDir, fileName);
            HistoryManager historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            Assert.AreEqual(historyManager.Count, 0);
        }

        [TestMethod]
        public void Initialize_SecondCall_Parameterless()
        {
            string fileName = "BeatSyncHistory-TestCol1.json";
            string filePath = Path.Combine(HistoryTestPathDir, fileName);
            Directory.CreateDirectory(HistoryTestPathDir);
            HistoryManager historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            KeyValuePair<string, HistoryEntry> songToAdd = TestCollection2.First();
            historyManager.TryAdd(songToAdd.Key, songToAdd.Value.SongInfo, songToAdd.Value.Flag);
            Assert.AreEqual(9, historyManager.Count);
            historyManager.Initialize();
            Assert.AreEqual(9, historyManager.Count);
        }
    }
}
