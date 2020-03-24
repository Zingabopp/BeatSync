using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncLib.History;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BeatSyncLib.Playlists;
using static BeatSyncLibTests.HistoryManager_Tests.HistoryTestData;
using BeatSyncLib.Playlists.Legacy;

namespace BeatSyncLibTests.HistoryManager_Tests
{
    [TestClass]
    public class TryAdd_Tests
    {
        static TryAdd_Tests()
        {
            TestSetup.Initialize();
        }

        private static readonly string HistoryTestPathDir = Path.GetFullPath(Path.Combine("Output", "HistoryManager"));


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
            var historyManager = new HistoryManager(HistoryTestPathDir);
            string key = "LKSJDFLKJASDLFKJ";
            string songName = null;
            string mapperName = null;
            Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryAdd(key, songName, mapperName, 0));
        }

        [TestMethod]
        public void TryAdd_NullPlaylistSong()
        {
            var path = Path.Combine(Path.Combine(HistoryTestPathDir, "BeatSyncHistory.json"));
            var historyManager = new HistoryManager(path);
            historyManager.Initialize();
            IPlaylistSong nullSong = null;
            var success = historyManager.TryAdd(nullSong, 0);
            Assert.IsFalse(success);
            Assert.AreEqual(historyManager.Count, 0);
        }


        [TestMethod]
        public void TryAdd_PlaylistSong_NotInitialized()
        {
            var historyManager = new HistoryManager(HistoryTestPathDir);
            string hash = "LKSJDFLKJASDLFKJ";
            string songName = "TestName";
            string songKey = "aaaa";
            string mapper = "SomeMapper";
            IPlaylistSong song = new LegacyPlaylistSong(hash, songName, songKey, mapper);
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
            IPlaylistSong song = new LegacyPlaylistSong(hash, songName, songKey, mapper);
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
                IPlaylistSong song = new LegacyPlaylistSong(hash, songName, songKey, mapper);
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
                IPlaylistSong song = new LegacyPlaylistSong(hash, songName, songKey, mapper);
                Assert.ThrowsException<InvalidOperationException>(() => historyManager.TryAdd(song, 0));
            }
            catch (ArgumentNullException)
            {
                //Assert.Inconclusive("PlaylistSong.Hash can never be null or empty.");
            }

        }

        private string PlaylistToString(IPlaylistSong song)
        {
            return $"({song.Key}) {song.Name} by {song.LevelAuthorName}";
        }
    }
}
