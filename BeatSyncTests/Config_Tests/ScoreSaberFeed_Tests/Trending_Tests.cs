using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Configs;
using SongFeedReaders;
using BeatSync.Playlists;

namespace BeatSyncTests.Config_Tests.ScoreSaberFeed_Tests
{
    [TestClass]
    public class Trending_Tests
    {
        [TestMethod]
        public void Defaults()
        {
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            Assert.IsFalse(c.ConfigChanged); // No ConfigChange since none of the properties have been set.
            c.FillDefaults();
            Assert.IsTrue(c.ConfigChanged); // Getting defaults from unassigned properties changes config.
            pc.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            Assert.AreEqual(false, c.Enabled);
            Assert.AreEqual(20, c.MaxSongs);
            Assert.AreEqual(PlaylistStyle.Append, c.PlaylistStyle);
            Assert.AreEqual(false, c.RankedOnly);
            Assert.AreEqual(BuiltInPlaylist.ScoreSaberTrending, c.FeedPlaylist);
            Assert.AreEqual(true, c.CreatePlaylist);

            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
        }

        #region Changed Properties
        [TestMethod]
        public void Changed_Enabled()
        {
            var defaultVal = false;
            var newVal = true;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.Enabled);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.Enabled = newVal;

            Assert.AreEqual(newVal, c.Enabled);
            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
        }

        [TestMethod]
        public void Changed_MaxSongs()
        {
            var defaultVal = 20;
            var newVal = 10;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.MaxSongs);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.MaxSongs = newVal;

            Assert.AreEqual(newVal, c.MaxSongs);
            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
        }

        [TestMethod]
        public void Changed_PlaylistStyle()
        {
            var defaultVal = PlaylistStyle.Append;
            var newVal = PlaylistStyle.Replace;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.PlaylistStyle);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.PlaylistStyle = newVal;

            Assert.AreEqual(newVal, c.PlaylistStyle);
            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
        }

        [TestMethod]
        public void Changed_RankedOnly()
        {
            var defaultVal = false;
            var newVal = true;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.RankedOnly);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.RankedOnly = newVal;

            Assert.AreEqual(newVal, c.RankedOnly);
            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
        }

        /// <summary>
        /// Changing FeedPlaylist should not affect ConfigChanged.
        /// </summary>
        [TestMethod]
        public void Changed_FeedPlaylist()
        {
            var defaultVal = BuiltInPlaylist.ScoreSaberTrending;
            var newVal = BuiltInPlaylist.ScoreSaberTopRanked;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.FeedPlaylist);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.FeedPlaylist = newVal;

            Assert.AreEqual(newVal, c.FeedPlaylist);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
        }

        [TestMethod]
        public void Changed_CreatePlaylist()
        {
            var defaultVal = true;
            var newVal = false;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.CreatePlaylist);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.CreatePlaylist = newVal;

            Assert.AreEqual(newVal, c.CreatePlaylist);
            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
        }
        #endregion

        #region Changed Properties
        [TestMethod]
        public void Unchanged_Enabled()
        {
            var defaultVal = false;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.Enabled);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.Enabled = defaultVal;

            Assert.AreEqual(defaultVal, c.Enabled);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
        }

        [TestMethod]
        public void Unchanged_MaxSongs()
        {
            var defaultVal = 20;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.MaxSongs);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.MaxSongs = defaultVal;

            Assert.AreEqual(defaultVal, c.MaxSongs);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
        }

        [TestMethod]
        public void Unchanged_PlaylistStyle()
        {
            var defaultVal = PlaylistStyle.Append;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.PlaylistStyle);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.PlaylistStyle = defaultVal;

            Assert.AreEqual(defaultVal, c.PlaylistStyle);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
        }

        [TestMethod]
        public void Unchanged_RankedOnly()
        {
            var defaultVal = false;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.RankedOnly);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.RankedOnly = defaultVal;

            Assert.AreEqual(defaultVal, c.RankedOnly);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
        }

        [TestMethod]
        public void Unchanged_FeedPlaylist()
        {
            var defaultVal = BuiltInPlaylist.ScoreSaberTrending;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.FeedPlaylist);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.FeedPlaylist = defaultVal;

            Assert.AreEqual(defaultVal, c.FeedPlaylist);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
        }

        [TestMethod]
        public void Unchanged_CreatePlaylist()
        {
            var defaultVal = true;
            var pc = new PluginConfig();
            var c = pc.ScoreSaber.Trending;
            pc.FillDefaults();
            pc.ResetConfigChanged();
            Assert.AreEqual(defaultVal, c.CreatePlaylist);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            c.CreatePlaylist = defaultVal;

            Assert.AreEqual(defaultVal, c.CreatePlaylist);
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);
        }
        #endregion

    }
}
