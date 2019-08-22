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
    public class TryGetValue_Tests
    {
        static TryGetValue_Tests()
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
        public void TryGetValue_NotInitialized()
        {
            var historyManager = new HistoryManager();
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
                historyManager.TryAdd(pair.Key, pair.Value, 0);
            }
            var key = TestCollection1.Keys.First();
            var expectedValue = TestCollection1[key];
            var doesContain = historyManager.TryGetValue(key, out HistoryEntry value);
            Assert.IsTrue(doesContain);
            Assert.AreEqual(expectedValue, value.SongInfo);

        }

        [TestMethod]
        public void TryGetValue_DoesntContainKey()
        {
            var path = Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json");
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            foreach (var pair in TestCollection1)
            {
                historyManager.TryAdd(pair.Key, pair.Value, 0);
            }
            var key = "ozxicuvoizuxcvoiu";
            var doesContain = historyManager.TryGetValue(key, out HistoryEntry value);
            Assert.IsFalse(doesContain);
            Assert.IsNull(value);

        }

        private string PlaylistToString(PlaylistSong song)
        {
            return $"({song.Key}) {song.Name} by {song.LevelAuthorName}";
        }
    }
}
