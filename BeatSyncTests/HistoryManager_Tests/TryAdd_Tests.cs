﻿using System;
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
        public void TryAdd_FileDoesntExist()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "DoesntExist", "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            var pairToInsert = TestCollection1.First();
            Assert.AreEqual(historyManager.Count, 0);
            var success = historyManager.TryAdd(pairToInsert.Key, pairToInsert.Value.SongInfo, pairToInsert.Value.Flag);
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
            var success = historyManager.TryAdd(pairToInsert.Key, pairToInsert.Value.SongInfo, pairToInsert.Value.Flag);
            Assert.IsTrue(success);
            success = historyManager.TryAdd(pairToInsert.Key, pairToInsert.Value.SongInfo, pairToInsert.Value.Flag);
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
            var success = historyManager.TryAdd(key, value, 0);
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
            var success = historyManager.TryAdd(key, value, 0);
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
            var success = historyManager.TryAdd(key, value, 0);
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
            var success = historyManager.TryAdd(key, value, 0);
            Assert.IsTrue(success);
            Assert.AreEqual(historyManager.Count, 1);
        }

        [TestMethod]
        public void TryAdd_NotInitialized()
        {
            var historyManager = new HistoryManager();
            string key = "LKSJDFLKJASDLFKJ";
            string value = null;
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryAdd(key, value, 0));
        }

        [TestMethod]
        public void TryAdd_NullPlaylistSong()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            PlaylistSong nullSong = null;
            var success = historyManager.TryAdd(nullSong, 0);
            Assert.IsFalse(success);
            Assert.AreEqual(historyManager.Count, 0);
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
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryAdd(song, 0));
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
            var success = historyManager.TryAdd(song, 0);
            Assert.IsTrue(success);
            success = historyManager.TryGetValue(hash, out var retrieved);
            Assert.IsTrue(success);
            Assert.AreEqual(song.ToString(), retrieved.SongInfo);
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
                Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryAdd(song, 0));
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
                Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryAdd(song, 0));
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
