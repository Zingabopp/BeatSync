using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Configs;
using SongFeedReaders;
using BeatSync.Playlists;
using System.Linq;

namespace BeatSyncTests.Config_Tests
{
    [TestClass]
    public class BeatSaverConfig_Tests
    {
        [TestMethod]
        public void Defaults()
        {
            var pc = new PluginConfig();
            var c = pc.BeatSaver;
            Assert.IsFalse(c.ConfigChanged); // No ConfigChange since none of the properties have been set.
            pc.FillDefaults();
            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged); // Getting defaults from unassigned properties changes config.
            pc.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);
            Assert.IsFalse(c.ConfigChanged);

            Assert.AreEqual(true, c.Enabled);
            Assert.AreEqual(5, c.MaxConcurrentPageChecks);
            Assert.IsNotNull(c.FavoriteMappers);
            Assert.IsNotNull(c.Hot);
            Assert.IsNotNull(c.Downloads);

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
            var c = pc.BeatSaver;
            var defaultValue = true;
            var newValue = false;
            Assert.AreEqual(defaultValue, c.Enabled);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Enabled = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.Enabled);
        }

        [TestMethod]
        public void Changed_MaxConcurrentPageChecks()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeatSaver;
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
        public void Changed_FavoriteMappers()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeatSaver;
            var defaultValue = c.FavoriteMappers;
            var newValue = new BeatSaverFavoriteMappers();
            Assert.AreEqual(defaultValue, c.FavoriteMappers);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.FavoriteMappers = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.FavoriteMappers);
        }

        [TestMethod]
        public void Changed_Hot()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeatSaver;
            var defaultValue = c.Hot;
            var newValue = new BeatSaverHot();
            Assert.AreEqual(defaultValue, c.Hot);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Hot = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.Hot);
        }

        [TestMethod]
        public void Changed_Downloads()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeatSaver;
            var defaultValue = c.Downloads;
            var newValue = new BeatSaverDownloads();
            Assert.AreEqual(defaultValue, c.Downloads);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Downloads = newValue;

            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.Downloads);
        }
        #endregion

        #region Unchanged Properties
        [TestMethod]
        public void Unchanged_Enabled()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeatSaver;
            c.FillDefaults();
            var defaultValue = true;
            Assert.AreEqual(defaultValue, c.Enabled);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Enabled = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.Enabled);
        }

        [TestMethod]
        public void Unchanged_MaxConcurrentPageChecks()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeatSaver;
            var defaultValue = 5;
            Assert.AreEqual(defaultValue, c.MaxConcurrentPageChecks);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.MaxConcurrentPageChecks = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.MaxConcurrentPageChecks);
        }

        [TestMethod]
        public void Unchanged_FavoriteMappers()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeatSaver;
            var defaultValue = c.FavoriteMappers;
            Assert.AreEqual(defaultValue, c.FavoriteMappers);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.FavoriteMappers = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.FavoriteMappers);
        }

        [TestMethod]
        public void Unchanged_Hot()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeatSaver;
            var defaultValue = c.Hot;
            Assert.AreEqual(defaultValue, c.Hot);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Hot = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.Hot);
        }

        [TestMethod]
        public void Unchanged_Downloads()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeatSaver;
            var defaultValue = c.Downloads;
            Assert.AreEqual(defaultValue, c.Downloads);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);

            c.Downloads = defaultValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.Downloads);
        }
        #endregion

        #region Invalid Inputs
        [TestMethod]
        public void Invalid_MaxConcurrentPageChecks()
        {
            var pc = new PluginConfig();
            pc.FillDefaults();
            pc.ResetConfigChanged();
            var c = pc.BeatSaver;
            var defaultValue = 5;
            var newValue = -1;
            Assert.AreEqual(defaultValue, c.MaxConcurrentPageChecks);
            c.ResetConfigChanged();
            Assert.IsFalse(pc.ConfigChanged);
           
            c.MaxConcurrentPageChecks = newValue;

            Assert.IsTrue(c.InvalidInputFixed);
            Assert.IsTrue(pc.ConfigChanged);
            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.MaxConcurrentPageChecks);
            var changedInput = "BeatSync.Configs.BeatSaverConfig:MaxConcurrentPageChecks";
            Assert.AreEqual(changedInput, c.InvalidInputs.First());
            pc.ResetFlags();
            Assert.AreEqual(0, c.InvalidInputs.Length);
            Assert.IsFalse(c.InvalidInputFixed);
            Assert.IsFalse(pc.ConfigChanged);
        }
        #endregion

    }
}
