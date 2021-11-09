using BeatSyncLib.History;
using BeatSyncLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongFeedReaders.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebUtilities.Mock.MockClient;
using WebUtilities;
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
            HistoryManager historyManager = new HistoryManager(HistoryTestFilePath, TestSetup.FileIO, TestSetup.LogFactory);
            Assert.AreEqual(HistoryTestFilePath, historyManager.HistoryPath);
            Assert.AreEqual(0, historyManager.Count);
        }

        [TestMethod]
        public void Constructor_NullPath()
        {
            string path = null!;
            Assert.ThrowsException<ArgumentNullException>(() => new HistoryManager(path, TestSetup.FileIO, TestSetup.LogFactory));
        }

        [TestMethod]
        public void Constructor_WithPath()
        {
            string path = Path.Combine(@"Data\HistoryManager\BeatSyncHistory-TestCol1.json");
            HistoryManager historyManager = new HistoryManager(path, TestSetup.FileIO, TestSetup.LogFactory);
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
            HistoryManager historyManager = new HistoryManager(filePath, TestSetup.FileIO, TestSetup.LogFactory);
            historyManager.Initialize();
            Assert.AreEqual(historyManager.Count, 8);
        }

        [TestMethod]
        public void Initialize_FileDoesntExist()
        {
            string fileName = "BeatSyncHistory-DoesntExist.json";
            string filePath = Path.Combine(HistoryTestPathDir, fileName);
            HistoryManager historyManager = new HistoryManager(filePath, TestSetup.FileIO, TestSetup.LogFactory);
            historyManager.Initialize();
            Assert.AreEqual(historyManager.Count, 0);
        }

        [TestMethod]
        public void Initialize_SecondCall_Parameterless()
        {
            string fileName = "BeatSyncHistory-TestCol1.json";
            string filePath = Path.Combine(HistoryTestPathDir, fileName);
            Directory.CreateDirectory(HistoryTestPathDir);
            HistoryManager historyManager = new HistoryManager(filePath, TestSetup.FileIO, TestSetup.LogFactory);
            historyManager.Initialize();
            KeyValuePair<string, HistoryEntry> songToAdd = TestCollection2.First();
            historyManager.TryAdd(songToAdd.Key, songToAdd.Value.SongInfo, songToAdd.Value.Flag);
            Assert.AreEqual(9, historyManager.Count);
            historyManager.Initialize();
            Assert.AreEqual(9, historyManager.Count);
        }
    }
}
