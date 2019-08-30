using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Configs;
using SongFeedReaders;
using BeatSync.Playlists;

namespace BeatSyncTests.Config_Tests
{
    [TestClass]
    public class ScoreSaberConfig_Tests
    {
        [TestMethod]
        public void Defaults()
        {
            var pc = new PluginConfig();
            var c = pc.ScoreSaber;
            Assert.IsFalse(c.ConfigChanged); // No ConfigChange since none of the properties have been set.
            c.FillDefaults();
            Assert.IsTrue(c.ConfigChanged); // Getting defaults from unassigned properties changes config.
            pc.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            Assert.AreEqual(false, c.Enabled);
            Assert.IsNotNull(c.TopRanked);
            Assert.IsNotNull(c.LatestRanked);
            Assert.IsNotNull(c.Trending);
            Assert.IsNotNull(c.TopPlayed);

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
            var c = pc.ScoreSaber;
            var defaultValue = false;
            var newValue = true;
            Assert.AreEqual(defaultValue, c.Enabled);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.Enabled = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.Enabled);
        }

        [TestMethod]
        public void Changed_TopRanked()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.ScoreSaber;
            var defaultValue = c.TopRanked;
            var newValue = new ScoreSaberTopRanked();
            Assert.AreEqual(defaultValue, c.TopRanked);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.TopRanked = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.TopRanked);
        }

        [TestMethod]
        public void Changed_LatestRanked()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.ScoreSaber;
            var defaultValue = c.LatestRanked;
            var newValue = new ScoreSaberLatestRanked();
            Assert.AreEqual(defaultValue, c.LatestRanked);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.LatestRanked = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.LatestRanked);
        }

        [TestMethod]
        public void Changed_Trending()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.ScoreSaber;
            var defaultValue = c.Trending;
            var newValue = new ScoreSaberTrending();
            Assert.AreEqual(defaultValue, c.Trending);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.Trending = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.Trending);
        }
        #endregion

        #region Unchanged Properties
        [TestMethod]
        public void Unchanged_Enabled()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.ScoreSaber;
            c.FillDefaults();
            var defaultValue = false;
            Assert.AreEqual(defaultValue, c.Enabled);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.Enabled = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.Enabled);
        }

        [TestMethod]
        public void Unchanged_TopRanked()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.ScoreSaber;
            var defaultValue = c.TopRanked;
            Assert.AreEqual(defaultValue, c.TopRanked);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.TopRanked = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.TopRanked);
        }

        [TestMethod]
        public void Unchanged_LatestRanked()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.ScoreSaber;
            var defaultValue = c.LatestRanked;
            Assert.AreEqual(defaultValue, c.LatestRanked);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.LatestRanked = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.LatestRanked);
        }

        [TestMethod]
        public void Unchanged_Trending()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.ScoreSaber;
            var defaultValue = c.Trending;
            Assert.AreEqual(defaultValue, c.Trending);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.Trending = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.Trending);
        }

        [TestMethod]
        public void Unchanged_TopPlayed()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.ScoreSaber;
            var defaultValue = c.TopPlayed;
            Assert.AreEqual(defaultValue, c.TopPlayed);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.TopPlayed = defaultValue;

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.TopPlayed);
        }
        #endregion

    }
}
