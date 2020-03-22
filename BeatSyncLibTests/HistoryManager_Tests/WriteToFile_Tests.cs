using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncLib.History;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BeatSyncLib.Playlists;
using Newtonsoft.Json;
using static BeatSyncLibTests.HistoryManager_Tests.HistoryTestData;

namespace BeatSyncLibTests.HistoryManager_Tests
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
