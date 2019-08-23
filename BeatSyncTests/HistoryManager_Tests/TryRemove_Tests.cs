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
    public class TryRemove_Tests
    {
        static TryRemove_Tests()
        {
            TestSetup.Initialize();
        }

        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Output", "historyManager"));

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

            success = historyManager.TryRemove(key);
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

            success = historyManager.TryRemove(doesntExist);
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
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryRemove(key));
        }
    }
}
