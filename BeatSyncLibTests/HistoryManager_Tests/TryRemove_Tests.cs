using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncLib.History;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BeatSyncPlaylists;
using static BeatSyncLibTests.HistoryManager_Tests.HistoryTestData;

namespace BeatSyncLibTests.HistoryManager_Tests
{
    [TestClass]
    public class TryRemove_Tests
    {
        static TryRemove_Tests()
        {
            TestSetup.Initialize();
        }

        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Output", "historyManager"));

        [TestMethod]
        public void TryRemove_Exists()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            string key = "QWEMNRBQENMQBWERNBQWXCV";
            string value = "Song to remove";
            string otherKey = "ZXCVOIUZXCOVIUZXCVUIOZZ";
            string otherValue = "Other song";
            var success = historyManager.TryAdd(key, value, 0);
            success = success && historyManager.TryAdd(otherKey, otherValue, 0);
            Assert.IsTrue(success);
            Assert.AreEqual(historyManager.Count, 2);
            // End setup

            success = historyManager.TryRemove(key, out var _);
            Assert.IsTrue(success);
            Assert.AreEqual(historyManager.Count, 1);
            Assert.IsTrue(historyManager.ContainsKey(otherKey));
        }

        [TestMethod]
        public void TryRemove_DoesntExist()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            string key = "QWEMNRBQENMQBWERNBQWXCV";
            string value = "Song to remove";
            string doesntExist = "ZXCVOIUZXCOVIUZXCVUIOZZ";
            var success = historyManager.TryAdd(key, value, 0);
            Assert.IsTrue(success);
            Assert.AreEqual(historyManager.Count, 1);
            // End setup

            success = historyManager.TryRemove(doesntExist, out var _);
            Assert.IsFalse(success);
            Assert.AreEqual(historyManager.Count, 1);
            Assert.IsTrue(historyManager.ContainsKey(key));
        }

        [TestMethod]
        public void TryRemove_NotInitialized()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            string key = "QWEMNRBQENMQBWERNBQWXCV";
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryRemove(key, out var _));
        }
    }
}
