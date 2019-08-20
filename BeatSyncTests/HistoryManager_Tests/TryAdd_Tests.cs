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
    public class TryAdd_Tests
    {
        static TryAdd_Tests()
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
        public void TryAdd_FileDoesntExist()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "DoesntExist", "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            var pairToInsert = TestCollection1.First();
            Assert.AreEqual(historyManager.Count, 0);
            var success = historyManager.TryAdd(pairToInsert.Key, pairToInsert.Value);
            Assert.IsTrue(success);
            Assert.AreEqual(historyManager.Count, 1);
        }

        [TestMethod]
        public void TryAdd_Duplicate()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            var pairToInsert = TestCollection1.First();
            Assert.AreEqual(historyManager.Count, 0);
            var success = historyManager.TryAdd(pairToInsert.Key, pairToInsert.Value);
            Assert.IsTrue(success);
            success = historyManager.TryAdd(pairToInsert.Key, pairToInsert.Value);
            Assert.IsFalse(success);
            Assert.AreEqual(historyManager.Count, 1);
        }

        [TestMethod]
        public void TryAdd_EmptyKey()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            string key = "";
            string value = "Song that should not be inserted";
            var success = historyManager.TryAdd(key, value);
            Assert.IsFalse(success);
            Assert.AreEqual(historyManager.Count, 0);
        }

        [TestMethod]
        public void TryAdd_NullKey()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            string key = null;
            string value = "Song that should not be inserted";
            var success = historyManager.TryAdd(key, value);
            Assert.IsFalse(success);
            Assert.AreEqual(historyManager.Count, 0);
        }

        [TestMethod]
        public void TryAdd_EmptyValue()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            string key = "LKSJDFLKJASDLFKJ";
            string value = "";
            var success = historyManager.TryAdd(key, value);
            Assert.IsTrue(success);
            Assert.AreEqual(historyManager.Count, 1);
        }

        [TestMethod]
        public void TryAdd_NullValue()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            string key = "LKSJDFLKJASDLFKJ";
            string value = null;
            var success = historyManager.TryAdd(key, value);
            Assert.IsTrue(success);
            Assert.AreEqual(historyManager.Count, 1);
        }

        [TestMethod]
        public void TryAdd_NotInitialized()
        {
            var historyManager = new HistoryManager();
            string key = "LKSJDFLKJASDLFKJ";
            string value = null;
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryAdd(key, value));
        }

        [TestMethod]
        public void TryAdd_PlaylistSong_NotInitialized()
        {
            var historyManager = new HistoryManager();
            string hash = "LKSJDFLKJASDLFKJ";
            string songName = "TestName";
            string songKey = "aaaa";
            string mapper = "SomeMapper";
            PlaylistSong song = new PlaylistSong(hash, songName, songKey, mapper);
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryAdd(song));
        }

        [TestMethod]
        public void TryAdd_PlaylistSong()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            string hash = "LKSJDFLKJASDLFKJ";
            string songName = "TestName";
            string songKey = "aaaa";
            string mapper = "SomeMapper";
            PlaylistSong song = new PlaylistSong(hash, songName, songKey, mapper);
            var success = historyManager.TryAdd(song);
            Assert.IsTrue(success);
            success = historyManager.TryGetValue(hash, out var retrieved);
            Assert.IsTrue(success);
            Assert.AreEqual(retrieved, PlaylistToString(song));
        }

        [TestMethod]
        public void TryAdd_PlaylistSong_NullHash()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            string hash = null;
            string songName = "TestName";
            string songKey = "aaaa";
            string mapper = "SomeMapper";
            try
            {
                PlaylistSong song = new PlaylistSong(hash, songName, songKey, mapper);
                Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryAdd(song));
            }
            catch (ArgumentNullException)
            {
                //Assert.Inconclusive("PlaylistSong.Hash can never be null or empty.");
            }

        }

        [TestMethod]
        public void TryAdd_PlaylistSong_EmptyHash()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            string hash = "";
            string songName = "TestName";
            string songKey = "aaaa";
            string mapper = "SomeMapper";
            try
            {
                PlaylistSong song = new PlaylistSong(hash, songName, songKey, mapper);
                Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryAdd(song));
            }
            catch (ArgumentNullException)
            {
                //Assert.Inconclusive("PlaylistSong.Hash can never be null or empty.");
            }

        }

        private string PlaylistToString(PlaylistSong song)
        {
            return $"({song.Key}) {song.Name} by {song.LevelAuthorName}";
        }
    }
}
