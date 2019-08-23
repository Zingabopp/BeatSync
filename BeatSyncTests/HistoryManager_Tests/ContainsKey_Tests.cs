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
    public class ContainsKey_Tests
    {
        static ContainsKey_Tests()
        {
            TestSetup.Initialize();
        }

        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Output", "HistoryManager"));

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
        public void ContainsKey_NotInitialized()
        {
            var historyManager = new HistoryManager();
            string key = "LKSJDFLKJASDLFKJ";
            string value = null;
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.ContainsKey(key));
        }

        [TestMethod]
        public void ContainsKey_DoesContainKey()
        {
            var path = Path.Combine(HistoryTestPathDir, "DoesntExist", "BeatSyncHistory.json");
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (var pair in TestCollection1)
            {
                
                historyManager.TryAdd(pair.Key, pair.Value.SongInfo, pair.Value.Flag);
            }
            var doesContain = historyManager.ContainsKey(TestCollection1.Keys.First());
            Assert.IsTrue(doesContain);
        }

        [TestMethod]
        public void ContainsKey_DoesntContainKey()
        {
            var path = Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json");
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (var pair in TestCollection1)
            {
                historyManager.TryAdd(pair.Key, pair.Value.SongInfo, pair.Value.Flag);
            }
            var notAddedKey = "zoxcasdlfkjasdlfkj";
            var doesContain = historyManager.ContainsKey(notAddedKey);
            Assert.IsFalse(doesContain);
        }

        [TestMethod]
        public void ContainsKey_EmptyKey()
        {
            var path = Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json");
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (var pair in TestCollection1)
            {
                historyManager.TryAdd(pair.Key, pair.Value.SongInfo, pair.Value.Flag);
            }
            var emptyKey = "";
            var doesContain = historyManager.ContainsKey(emptyKey);
            Assert.IsFalse(doesContain);
        }

        [TestMethod]
        public void ContainsKey_NullKey()
        {
            var path = Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json");
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (var pair in TestCollection1)
            {
                historyManager.TryAdd(pair.Key, pair.Value.SongInfo, pair.Value.Flag);
            }
            string nullKey = null;
            var doesContain = historyManager.ContainsKey(nullKey);
            Assert.IsFalse(doesContain);
        }


        private string PlaylistToString(PlaylistSong song)
        {
            return $"({song.Key}) {song.Name} by {song.LevelAuthorName}";
        }
    }
}
