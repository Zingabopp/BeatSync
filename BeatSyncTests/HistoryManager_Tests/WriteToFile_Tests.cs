using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BeatSync.Playlists;
using Newtonsoft.Json;

namespace BeatSyncTests.HistoryManager_Tests
{
    [TestClass]
    public class WriteToFile_Tests
    {
        static WriteToFile_Tests()
        {
            TestSetup.Initialize();
            //File.WriteAllText(@"Data\HistoryManager\BeatSyncHistory-TestCol1-newnew.json", JsonConvert.SerializeObject(TestCollection1, Formatting.Indented));
            //File.WriteAllText(@"Data\HistoryManager\BeatSyncHistory-TestCol2-newnew.json", JsonConvert.SerializeObject(TestCollection2, Formatting.Indented));
        }

        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Output", "HistoryManager", "WriteTests"));

        private static Dictionary<string, HistoryEntry> TestCollection1 = new Dictionary<string, HistoryEntry>()
        {
            {"LAKSDJFLK23LKJF23LKJ23R", new HistoryEntry("Test song 1 by whoever", 0) },
            {"ASDFALKSDJFLKAJSDFLKJAS", new HistoryEntry("Test song 2 by whoever", 0) },
            {"AVCIJASLDKVJAVLSKDJLKAJ", new HistoryEntry("Test song 3 by whoever", 0) },
            {"ASDLVKJASVLDKJALKSDJFLK", new HistoryEntry("Test song 4 by whoever", 0) },
            {"QWEORIUQWEORIUQOWIEURAO", new HistoryEntry("Test song 5 by whoever", 0) },
            {"ZXCVPOZIXCVPOIZXCVPOVIV", new HistoryEntry("Test song 6 by whoever", 0) },
            {"QLQFWHJLNKFLKNMWLQKCNML", new HistoryEntry("Test song 7 by whoever", 0) },
            {"TBRNEMNTMRBEBNMTEERVCVB", new HistoryEntry("Test song 8 by whoever", 0) }
        };

        private static Dictionary<string, HistoryEntry> TestCollection2 = new Dictionary<string, HistoryEntry>()
        {
            {"QWEMNRBQENMQBWERNBQWXCV", new HistoryEntry("Test song 9 by whoever", 0) },
            {"ZXCVOIUZXCOVIUZXCVUIOZZ", new HistoryEntry("Test song 10 by whoever", 0) },
            {"YXXCVBYIUXCVBIUYXCVBIUY", new HistoryEntry("Test song 11 by whoever", 0) },
            {"MNBWMENRTBMQNWEBTMNQBWE", new HistoryEntry("Test song 12 by whoever", 0) }
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
                historyManager.TryAdd(item.Key, item.Value.SongInfo, 0);
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
                historyManager.TryAdd(item.Key, item.Value.SongInfo, 0);
            }
            historyManager.WriteToFile();

            // Cleanup
            Assert.IsTrue(File.Exists(filePath));
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
        }


    }
}
