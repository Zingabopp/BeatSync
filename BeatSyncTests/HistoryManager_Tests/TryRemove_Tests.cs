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
        public void TryRemove_Exists()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            string key = "QWEMNRBQENMQBWERNBQWXCV";
            string value = "Song to remove";
            string otherKey = "ZXCVOIUZXCOVIUZXCVUIOZZ";
            string otherValue = "Other song";
            var success = historyManager.TryAdd(key, value);
            success = success && historyManager.TryAdd(otherKey, otherValue);
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
            var success = historyManager.TryAdd(key, value);
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
