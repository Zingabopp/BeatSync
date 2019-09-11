using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Configs;
using SongFeedReaders;
using BeatSync.Playlists;

namespace BeatSyncTests.Config_Tests
{
    [TestClass]
    public class BeastSaberConfig_Tests
    {
        [TestMethod]
        public void Defaults()
        {
            var pc = new PluginConfig();
            var c = pc.BeastSaber;
            Assert.IsFalse(c.ConfigChanged); // No ConfigChange since none of the properties have been set.
            pc.FillDefaults();
            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged); // Getting defaults from unassigned properties changes config.
            pc.ResetConfigChanged();
            c.ResetConfigChanged();
            pc.FillDefaults();
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            Assert.AreEqual(true, c.Enabled);
            Assert.AreEqual(string.Empty, c.Username);
            Assert.AreEqual(5, c.MaxConcurrentPageChecks);
            Assert.IsNotNull(c.Bookmarks);
            Assert.IsNotNull(c.Follows);
            Assert.IsNotNull(c.CuratorRecommended);

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

        }

        #region Changed Properties
        [TestMethod]
        public void Changed_Enabled()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = true;
            var newValue = false;
            Assert.AreEqual(defaultValue, c.Enabled);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Enabled = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.Enabled);
        }

        [TestMethod]
        public void Changed_Username()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = string.Empty;
            var newValue = "Username";
            Assert.AreEqual(defaultValue, c.Username);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Username = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.Username);
        }

        [TestMethod]
        public void Changed_MaxConcurrentPageChecks()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = 5;
            var newValue = 1;
            Assert.AreEqual(defaultValue, c.MaxConcurrentPageChecks);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.MaxConcurrentPageChecks = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.MaxConcurrentPageChecks);
        }

        [TestMethod]
        public void Changed_Bookmarks()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = c.Bookmarks;
            var newValue = new BeastSaberBookmarks();
            Assert.AreEqual(defaultValue, c.Bookmarks);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Bookmarks = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.Bookmarks);
        }

        [TestMethod]
        public void Changed_Follows()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = c.Follows;
            var newValue = new BeastSaberFollowings();
            Assert.AreEqual(defaultValue, c.Follows);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Follows = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.Follows);
        }

        [TestMethod]
        public void Changed_CuratorRecommended()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = c.CuratorRecommended;
            var newValue = new BeastSaberCuratorRecommended();
            Assert.AreEqual(defaultValue, c.CuratorRecommended);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.CuratorRecommended = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.CuratorRecommended);
        }
        #endregion

        #region Unchanged Properties
        [TestMethod]
        public void Unchanged_Enabled()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            c.FillDefaults();
            var defaultValue = true;
            Assert.AreEqual(defaultValue, c.Enabled);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Enabled = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.Enabled);
        }

        [TestMethod]
        public void Unchanged_Username()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = string.Empty;
            Assert.AreEqual(defaultValue, c.Username);
            pc.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Username = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.Username);
        }

        [TestMethod]
        public void Unchanged_MaxConcurrentPageChecks()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = 5;
            Assert.AreEqual(defaultValue, c.MaxConcurrentPageChecks);
            pc.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.MaxConcurrentPageChecks = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.MaxConcurrentPageChecks);
        }

        [TestMethod]
        public void Unchanged_Bookmarks()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = c.Bookmarks;
            Assert.AreEqual(defaultValue, c.Bookmarks);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Bookmarks = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.Bookmarks);
        }

        [TestMethod]
        public void Unchanged_Follows()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = c.Follows;
            Assert.AreEqual(defaultValue, c.Follows);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.Follows = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.Follows);
        }

        [TestMethod]
        public void Unchanged_CuratorRecommended()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeastSaber;
            var defaultValue = c.CuratorRecommended;
            Assert.AreEqual(defaultValue, c.CuratorRecommended);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.CuratorRecommended = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.CuratorRecommended);
        }
        #endregion


        [TestMethod]
        public void Clone()
        {
            var defaultConfig = new PluginConfig();
            defaultConfig.FillDefaults();
            defaultConfig.ResetConfigChanged();

            var editedConfig = new PluginConfig();
            editedConfig.FillDefaults();
            editedConfig.ResetConfigChanged();
            Assert.IsTrue(editedConfig.ConfigMatches(defaultConfig));
            editedConfig.DownloadTimeout = defaultConfig.DownloadTimeout + 3;
            editedConfig.AllBeatSyncSongsPlaylist = !defaultConfig.AllBeatSyncSongsPlaylist;
            editedConfig.BeastSaber.Enabled = !defaultConfig.BeastSaber.Enabled;
            editedConfig.BeastSaber.Username = "TestUser";
            editedConfig.BeastSaber.Bookmarks.MaxSongs = defaultConfig.BeastSaber.Bookmarks.MaxSongs + 3;
            editedConfig.BeatSaver.MaxConcurrentPageChecks = defaultConfig.BeatSaver.MaxConcurrentPageChecks + 2;
            editedConfig.BeatSaver.FavoriteMappers.SeparateMapperPlaylists = !defaultConfig.BeatSaver.FavoriteMappers.SeparateMapperPlaylists;
            editedConfig.BeatSaver.Hot.Enabled = !defaultConfig.BeatSaver.Hot.Enabled;
            editedConfig.ScoreSaber.Enabled = !defaultConfig.ScoreSaber.Enabled;
            editedConfig.ScoreSaber.Trending.RankedOnly = !defaultConfig.ScoreSaber.Trending.RankedOnly;
            editedConfig.ScoreSaber.Trending.Enabled = !defaultConfig.ScoreSaber.Trending.Enabled;
            Assert.IsFalse(editedConfig.ConfigMatches(defaultConfig));
            var clonedConfig = editedConfig.Clone();
            Assert.IsTrue(editedConfig.ConfigMatches(clonedConfig));
        }
    }
}
