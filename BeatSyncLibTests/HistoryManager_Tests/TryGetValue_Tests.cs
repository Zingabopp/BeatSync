using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncLib.History;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using static BeatSyncLibTests.HistoryManager_Tests.HistoryTestData;

namespace BeatSyncLibTests.HistoryManager_Tests
{
    [TestClass]
    public class TryGetValue_Tests
    {
        static TryGetValue_Tests()
        {
            TestSetup.Initialize();
        }

        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Output", "HistoryManager"));


        [TestMethod]
        public void TryGetValue_NotInitialized()
        {
            var historyManager = new HistoryManager(HistoryTestPathDir);
            string key = "LKSJDFLKJASDLFKJ";
            HistoryEntry value = null;
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryGetValue(key, out value));
        }

        [TestMethod]
        public void TryGetValue_DoesContainKey()
        {
            var path = Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json");
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (var pair in TestCollection1)
            {
                historyManager.TryAdd(pair.Key, pair.Value.SongInfo, pair.Value.Flag);
            }
            var key = TestCollection1.Keys.First();
            var expectedValue = TestCollection1[key];
            var doesContain = historyManager.TryGetValue(key, out HistoryEntry value);
            Assert.IsTrue(doesContain);
            Assert.AreEqual(expectedValue.SongInfo, value.SongInfo);

        }

        [TestMethod]
        public void TryGetValue_DoesntContainKey()
        {
            var path = Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json");
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (var pair in TestCollection1)
            {
                historyManager.TryAdd(pair.Key, pair.Value.SongInfo, pair.Value.Flag);
            }
            var key = "ozxicuvoizuxcvoiu";
            var doesContain = historyManager.TryGetValue(key, out HistoryEntry value);
            Assert.IsFalse(doesContain);
            Assert.IsNull(value);

        }
    }
}
