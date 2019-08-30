using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Configs;
using SongFeedReaders;
using BeatSync.Playlists;

namespace BeatSyncTests.Config_Tests
{
    [TestClass]
    public class PluginConfig_Tests
    {
        [TestMethod]
        public void Defaults()
        {
            var c = new PluginConfig();
            Assert.IsFalse(c.ConfigChanged);
            c.FillDefaults();
            Assert.IsTrue(c.ConfigChanged); // Getting defaults from unassigned properties changes config.
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(c.RegenerateConfig, true);
            Assert.AreEqual(c.DownloadTimeout, 30);
            Assert.AreEqual(c.MaxConcurrentDownloads, 3);
            Assert.AreEqual(c.RecentPlaylistDays, 7);
            Assert.AreEqual(c.AllBeatSyncSongsPlaylist, false);
            Assert.IsNotNull(c.BeastSaber);
            Assert.IsNotNull(c.BeatSaver);
            Assert.IsNotNull(c.ScoreSaber);
        }

        #region Changed Properties
        [TestMethod]
        public void Changed_RegenerateConfig()
        {
            var c = new PluginConfig();
            var defaultValue = true;
            var newValue = false;
            Assert.AreEqual(defaultValue, c.RegenerateConfig);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.RegenerateConfig = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.RegenerateConfig);
        }

        [TestMethod]
        public void Changed_DownloadTimeout()
        {
            var c = new PluginConfig();
            var defaultValue = 30;
            var newValue = 25;
            Assert.AreEqual(defaultValue, c.DownloadTimeout);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.DownloadTimeout = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.DownloadTimeout);
        }

        [TestMethod]
        public void Changed_MaxConcurrentDownloads()
        {
            var c = new PluginConfig();
            var defaultValue = 3;
            var newValue = 1;
            Assert.AreEqual(defaultValue, c.MaxConcurrentDownloads);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.MaxConcurrentDownloads = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.MaxConcurrentDownloads);
        }

        [TestMethod]
        public void Changed_RecentPlaylistDays()
        {
            var c = new PluginConfig();
            var defaultValue = 7;
            var newValue = 3;
            Assert.AreEqual(defaultValue, c.RecentPlaylistDays);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.RecentPlaylistDays = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.RecentPlaylistDays);
        }

        [TestMethod]
        public void Changed_AllBeatSyncSongsPlaylist()
        {
            var c = new PluginConfig();
            var defaultValue = false;
            var newValue = true;
            Assert.AreEqual(defaultValue, c.AllBeatSyncSongsPlaylist);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.AllBeatSyncSongsPlaylist = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.AllBeatSyncSongsPlaylist);
        }

        [TestMethod]
        public void Changed_BeastSaber()
        {
            var c = new PluginConfig();
            var defaultValue = c.BeastSaber;
            var newValue = new BeastSaberConfig();
            Assert.AreEqual(defaultValue, c.BeastSaber);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.BeastSaber = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.BeastSaber);
        }

        [TestMethod]
        public void Changed_BeatSaver()
        {
            var c = new PluginConfig();
            var defaultValue = c.BeatSaver;
            var newValue = new BeatSaverConfig();
            Assert.AreEqual(defaultValue, c.BeatSaver);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.BeatSaver = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.BeatSaver);
        }

        [TestMethod]
        public void Changed_ScoreSaber()
        {
            var c = new PluginConfig();
            var defaultValue = c.ScoreSaber;
            var newValue = new ScoreSaberConfig();
            Assert.AreEqual(defaultValue, c.ScoreSaber);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.ScoreSaber = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.ScoreSaber);
        }
        #endregion

        #region Unchanged Properties
        [TestMethod]
        public void Unchanged_RegenerateConfig()
        {
            var c = new PluginConfig();
            var defaultValue = true;
            Assert.AreEqual(defaultValue, c.RegenerateConfig);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.RegenerateConfig = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.RegenerateConfig);
        }

        [TestMethod]
        public void Unchanged_DownloadTimeout()
        {
            var c = new PluginConfig();
            var defaultValue = 30;
            Assert.AreEqual(defaultValue, c.DownloadTimeout);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.DownloadTimeout = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.DownloadTimeout);
        }

        [TestMethod]
        public void Unchanged_MaxConcurrentDownloads()
        {
            var c = new PluginConfig();
            var defaultValue = 3;
            Assert.AreEqual(defaultValue, c.MaxConcurrentDownloads);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.MaxConcurrentDownloads = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.MaxConcurrentDownloads);
        }

        [TestMethod]
        public void Unchanged_RecentPlaylistDays()
        {
            var c = new PluginConfig();
            var defaultValue = 7;
            Assert.AreEqual(defaultValue, c.RecentPlaylistDays);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.RecentPlaylistDays = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.RecentPlaylistDays);
        }

        [TestMethod]
        public void Unchanged_AllBeatSyncSongsPlaylist()
        {
            var c = new PluginConfig();
            var defaultValue = false;
            Assert.AreEqual(defaultValue, c.AllBeatSyncSongsPlaylist);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.AllBeatSyncSongsPlaylist = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.AllBeatSyncSongsPlaylist);
        }

        [TestMethod]
        public void Unchanged_BeastSaber()
        {
            var c = new PluginConfig();
            var defaultValue = c.BeastSaber;
            Assert.AreEqual(defaultValue, c.BeastSaber);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.BeastSaber = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.BeastSaber);
        }

        [TestMethod]
        public void Unchanged_BeatSaver()
        {
            var c = new PluginConfig();
            var defaultValue = c.BeatSaver;
            Assert.AreEqual(defaultValue, c.BeatSaver);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.BeatSaver = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.BeatSaver);
        }

        [TestMethod]
        public void Unchanged_ScoreSaber()
        {
            var c = new PluginConfig();
            var defaultValue = c.ScoreSaber;
            Assert.AreEqual(defaultValue, c.ScoreSaber);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.ScoreSaber = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.ScoreSaber);
        }
        #endregion
    }
}
