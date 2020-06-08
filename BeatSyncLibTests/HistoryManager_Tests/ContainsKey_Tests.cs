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
    public class ContainsKey_Tests
    {
        static ContainsKey_Tests()
        {
            TestSetup.Initialize();
        }

        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Output", "HistoryManager"));

        [TestMethod]
        public void ContainsKey_NotInitialized()
        {
            HistoryManager historyManager = new HistoryManager(HistoryTestPathDir);
            string key = "LKSJDFLKJASDLFKJ";
            //string value = null;
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.ContainsKey(key));
        }

        [TestMethod]
        public void ContainsKey_DoesContainKey()
        {
            string path = Path.Combine(HistoryTestPathDir, "DoesntExist", "BeatSyncHistory.json");
            HistoryManager historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (KeyValuePair<string, HistoryEntry> pair in TestCollection1)
            {

                historyManager.TryAdd(pair.Key, pair.Value.SongInfo, pair.Value.Flag);
            }
            bool doesContain = historyManager.ContainsKey(TestCollection1.Keys.First());
            Assert.IsTrue(doesContain);
        }

        [TestMethod]
        public void ContainsKey_DoesntContainKey()
        {
            string path = Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json");
            HistoryManager historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (KeyValuePair<string, HistoryEntry> pair in TestCollection1)
            {
                historyManager.TryAdd(pair.Key, pair.Value.SongInfo, pair.Value.Flag);
            }
            string notAddedKey = "zoxcasdlfkjasdlfkj";
            bool doesContain = historyManager.ContainsKey(notAddedKey);
            Assert.IsFalse(doesContain);
        }

        [TestMethod]
        public void ContainsKey_EmptyKey()
        {
            string path = Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json");
            HistoryManager historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (KeyValuePair<string, HistoryEntry> pair in TestCollection1)
            {
                historyManager.TryAdd(pair.Key, pair.Value.SongInfo, pair.Value.Flag);
            }
            string emptyKey = "";
            bool doesContain = historyManager.ContainsKey(emptyKey);
            Assert.IsFalse(doesContain);
        }

        [TestMethod]
        public void ContainsKey_NullKey()
        {
            string path = Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json");
            HistoryManager historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (KeyValuePair<string, HistoryEntry> pair in TestCollection1)
            {
                historyManager.TryAdd(pair.Key, pair.Value.SongInfo, pair.Value.Flag);
            }
            string nullKey = null;
            bool doesContain = historyManager.ContainsKey(nullKey);
            Assert.IsFalse(doesContain);
        }
    }
}
