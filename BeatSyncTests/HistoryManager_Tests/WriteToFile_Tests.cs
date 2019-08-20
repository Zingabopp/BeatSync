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
    public class WriteToFile_Tests
    {
        static WriteToFile_Tests()
        {
            TestSetup.Initialize();
        }

        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Output", "HistoryManager", "WriteTests"));

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
        public void ExistingFile()
        {
            var fileName = "BeatSyncHistory-TestCol1.json";
            var dirPath = Path.Combine(HistoryTestPathDir, "Existing");
            var filePath = Path.Combine(dirPath, fileName);
            Directory.CreateDirectory(dirPath);
            if (!File.Exists(filePath))
                File.Copy(Path.Combine(@"Data\HistoryManager\", fileName), filePath);
            var historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            Assert.AreEqual(historyManager.Count, 8);
            historyManager.WriteToFile();
            Assert.IsFalse(File.Exists(filePath + ".bak"));
            // Cleanup
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
        }

        [TestMethod]
        public void LockedFile()
        {
            var fileName = "BeatSyncHistory-TestCol1.json";
            var dirPath = Path.Combine(HistoryTestPathDir, "LockedFile");
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
            var filePath = Path.Combine(dirPath, fileName);
            Directory.CreateDirectory(dirPath);
            if (!File.Exists(filePath))
                File.Copy(Path.Combine(@"Data\HistoryManager\", fileName), filePath);
            using (var fileLock = File.OpenRead(filePath))
            {
                var historyManager = new HistoryManager(filePath);
                historyManager.Initialize();
                Assert.AreEqual(historyManager.Count, 8);
                Assert.ThrowsException<IOException>(() => historyManager.WriteToFile());
                Assert.IsTrue(File.Exists(filePath + ".bak"));
            }
            // Cleanup
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
        }

        [TestMethod]
        public void FileDoesntExist()
        {
            var fileName = "BeatSyncHistory-DoesntExist.json";
            var dirPath = Path.Combine(HistoryTestPathDir, "FileDoesntExist");
            var filePath = Path.Combine(dirPath, fileName);
            Assert.IsFalse(File.Exists(filePath));
            var historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            foreach (var item in TestCollection1)
            {
                historyManager.TryAdd(item.Key, item.Value);
            }
            historyManager.WriteToFile();
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
        }

        [TestMethod]
        public void NotInitialized()
        {
            var historyManager = new HistoryManager();
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.WriteToFile());
        }

        [TestMethod]
        public void DirectoryDoesntExist()
        {
            var fileName = "BeatSyncHistory-DoesntExist.json";
            var dirPath = Path.Combine(HistoryTestPathDir, "DoesntExist");
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
            var filePath = Path.Combine(dirPath, fileName);
            var historyManager = new HistoryManager(filePath);
            historyManager.Initialize();
            Assert.AreEqual(historyManager.Count, 0);
            foreach (var item in TestCollection1)
            {
                historyManager.TryAdd(item.Key, item.Value);
            }
            historyManager.WriteToFile();

            // Cleanup
            Assert.IsTrue(File.Exists(filePath));
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
        }


    }
}
