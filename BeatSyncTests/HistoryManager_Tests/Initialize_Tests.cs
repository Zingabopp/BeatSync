using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BeatSync.Playlists;

namespace BeatSyncTests.HistoryManager_Tests
{
    [TestClass]
    public class Initialize_Tests
    {
        static Initialize_Tests()
        {
            TestSetup.Initialize();
        }

        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Output", "HistoryManager"));

        private Dictionary<string, string> TestCollection1 = new Dictionary<string, string>()
        {
            {"LAKSDJFLK23LKJF23LKJ23R", "Test song 1 by whoever" },
            {"ASDFALKSDJFLKAJSDFLKJAS", "Test song 2 by whoever" },
            {"AVCIJASLDKVJAVLSKDJLKAJ", "Test song 3 by whoever" },
            {"ASDLVKJASVLDKJALKSDJFLK", "Test song 4 by whoever" },
            {"QWEORIUQWEORIUQOWIEURAO", "Test song 5 by whoever" },
            {"ZXCVPOZIXCVPOIZXCVPOVIV", "Test song 6 by whoever" },
            {"QLQFWHJLNKFLKNMWLQKCNML", "Test song 7 by whoever" },
            {"TBRNEMNTMRBEBNMTEERVCVB", "Test song 8 by whoever" }
        };

        private Dictionary<string, string> TestCollection2 = new Dictionary<string, string>()
        {
            {"QWEMNRBQENMQBWERNBQWXCV", "Test song 9 by whoever" },
            {"ZXCVOIUZXCOVIUZXCVUIOZZ", "Test song 10 by whoever" },
            {"YXXCVBYIUXCVBIUYXCVBIUY", "Test song 11 by whoever" },
            {"MNBWMENRTBMQNWEBTMNQBWE", "Test song 12 by whoever" }
        };

        [TestMethod]
        public void Constructor_Default()
        {
            var historyManager = new HistoryManager();
            Assert.AreEqual(@"UserData\BeatSyncHistory.json", historyManager.HistoryPath);
            Assert.AreEqual(0, historyManager.Count);
        }

        [TestMethod]
        public void Constructor_NullPath()
        {
            string path = null;
            var historyManager = new HistoryManager(path);
            Assert.AreEqual(@"UserData\BeatSyncHistory.json", historyManager.HistoryPath);
            Assert.AreEqual(0, historyManager.Count);
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
            if (!File.Exists(filePath))
                File.Copy(Path.Combine(@"Data\HistoryManager\", fileName), filePath);
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
        public void Initialize_SecondCall_SecondDoesntExist()
        {
            var fileName = "BeatSyncHistory-TestCol1.json";
            var filePath = Path.Combine(HistoryTestPathDir, fileName);
            Directory.CreateDirectory(HistoryTestPathDir);
            if (!File.Exists(filePath))
                File.Copy(Path.Combine(@"Data\HistoryManager\", fileName), filePath);
            var historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            Assert.AreEqual(historyManager.Count, 8);
            historyManager.Initialize(@"Data\HistoryManager\DoesntExist.json");
            Assert.AreEqual(0, historyManager.Count);
        }

        [TestMethod]
        public void Initialize_SecondCall_SecondDoesExist()
        {
            var fileName = "BeatSyncHistory-TestCol1.json";
            var filePath = Path.Combine(HistoryTestPathDir, fileName);
            Directory.CreateDirectory(HistoryTestPathDir);
            if (!File.Exists(filePath))
                File.Copy(Path.Combine(@"Data\HistoryManager\", fileName), filePath);
            var historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            Assert.AreEqual(historyManager.Count, 8);
            historyManager.Initialize(@"Data\HistoryManager\BeatSyncHistory-TestCol2.json");
            Assert.AreEqual(4, historyManager.Count);
        }

    }
}
