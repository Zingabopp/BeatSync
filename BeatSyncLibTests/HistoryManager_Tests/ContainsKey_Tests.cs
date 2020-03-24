using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncLib.History;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BeatSyncLib.Playlists;
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
            var historyManager = new HistoryManager(HistoryTestPathDir);
            string key = "LKSJDFLKJASDLFKJ";
            //string value = null;
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


        private string PlaylistToString(IPlaylistSong song)
        {
            return $"({song.Key}) {song.Name} by {song.LevelAuthorName}";
        }
    }
}
