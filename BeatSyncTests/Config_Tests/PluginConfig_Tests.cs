using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Configs;
using SongFeedReaders;
using BeatSync.Playlists;
using System.Linq;

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
            Assert.AreEqual(false, c.RegenerateConfig);
            Assert.AreEqual(30, c.DownloadTimeout);
            Assert.AreEqual(3, c.MaxConcurrentDownloads);
            Assert.AreEqual(7, c.RecentPlaylistDays);
            Assert.AreEqual(0, c.TimeBetweenSyncs.Hours);
            Assert.AreEqual(10, c.TimeBetweenSyncs.Minutes);
            Assert.AreEqual(DateTime.MinValue, c.LastRun);
            Assert.AreEqual(false, c.AllBeatSyncSongsPlaylist);
            Assert.IsNotNull(c.BeastSaber);
            Assert.IsNotNull(c.BeatSaver);
            Assert.IsNotNull(c.ScoreSaber);
        }

        #region Changed Properties
        [TestMethod]
        public void Changed_RegenerateConfig()
        {
            var c = new PluginConfig();
            var defaultValue = false;
            var newValue = true;
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
        public void Changed_TimeBetweenSyncs_Minutes()
        {
            var c = new PluginConfig();
            var defaultValue = new SyncIntervalConfig() { Hours = 0, Minutes = 10 };
            var newValue = new SyncIntervalConfig() { Hours = 0, Minutes = 5 };
            Assert.AreEqual(defaultValue, c.TimeBetweenSyncs);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.TimeBetweenSyncs = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.TimeBetweenSyncs);
        }

        [TestMethod]
        public void Changed_TimeBetweenSyncs_Hours()
        {
            var c = new PluginConfig();
            var defaultValue = new SyncIntervalConfig() { Hours = 0, Minutes = 10 };
            var newValue = new SyncIntervalConfig() { Hours = 2, Minutes = 0 };
            Assert.AreEqual(defaultValue, c.TimeBetweenSyncs);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.TimeBetweenSyncs = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.TimeBetweenSyncs);
        }

        [TestMethod]
        public void Changed_LastRun()
        {
            var c = new PluginConfig();
            var defaultValue = DateTime.MinValue;
            var newValue = DateTime.Now;
            Assert.AreEqual(defaultValue, c.LastRun);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.LastRun = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(newValue, c.LastRun);
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
            var defaultValue = false;
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
        public void Unchanged_TimeBetweenSyncs()
        {
            var c = new PluginConfig();
            var defaultValue = new SyncIntervalConfig();
            var newValue = new SyncIntervalConfig(0, 10);
            Assert.AreEqual(defaultValue, c.TimeBetweenSyncs);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, newValue);
            c.TimeBetweenSyncs = newValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.TimeBetweenSyncs);
        }

        [TestMethod]
        public void Unchanged_LastRun()
        {
            var c = new PluginConfig();
            var defaultValue = DateTime.MinValue;
            var newValue = DateTime.MinValue;
            Assert.AreEqual(defaultValue, c.LastRun);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, newValue);
            c.LastRun = newValue;

            Assert.IsFalse(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.LastRun);
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

        #region Invalid Inputs
        [TestMethod]
        public void Invalid_DownloadTimeout()
        {
            var c = new PluginConfig();
            var defaultValue = 30;
            var newValue = -5;
            Assert.AreEqual(defaultValue, c.DownloadTimeout);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.DownloadTimeout = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(defaultValue, c.DownloadTimeout);
            var changedInput = "BeatSync.Configs.PluginConfig:DownloadTimeout";
            Assert.AreEqual(changedInput, c.InvalidInputs.First());
            c.ResetFlags();
            Assert.AreEqual(0, c.InvalidInputs.Length);
            Assert.IsFalse(c.InvalidInputFixed);
            Assert.IsFalse(c.ConfigChanged);
        }

        [TestMethod]
        public void Invalid_MaxConcurrentDownloads()
        {
            var c = new PluginConfig();
            var defaultValue = 3;
            var newValue = 0;
            Assert.AreEqual(defaultValue, c.MaxConcurrentDownloads);
            c.ResetConfigChanged();
            Assert.IsFalse(c.ConfigChanged);

            c.MaxConcurrentDownloads = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(1, c.MaxConcurrentDownloads);

            var changedInput = "BeatSync.Configs.PluginConfig:MaxConcurrentDownloads";
            Assert.AreEqual(changedInput, c.InvalidInputs.First());
            c.ResetFlags();
            Assert.AreEqual(0, c.InvalidInputs.Length);
            Assert.IsFalse(c.InvalidInputFixed);
            Assert.IsFalse(c.ConfigChanged);
        }

        [TestMethod]
        public void Invalid_RecentPlaylistDays()
        {
            var c = new PluginConfig();
            var defaultValue = 7;
            var newValue = -1;
            Assert.AreEqual(defaultValue, c.RecentPlaylistDays);
            c.ResetFlags();
            Assert.IsFalse(c.ConfigChanged);

            c.RecentPlaylistDays = newValue;

            Assert.IsTrue(c.ConfigChanged);
            Assert.AreEqual(0, c.RecentPlaylistDays);

            var changedInput = "BeatSync.Configs.PluginConfig:RecentPlaylistDays";
            Assert.AreEqual(changedInput, c.InvalidInputs.First());
            c.ResetFlags();
            Assert.AreEqual(0, c.InvalidInputs.Length);
            Assert.IsFalse(c.InvalidInputFixed);
            Assert.IsFalse(c.ConfigChanged);
        }

        [TestMethod]
        public void Invalid_TimeBetweenSyncs_Minutes()
        {
            var pc = new PluginConfig();

            var defaultValue = new SyncIntervalConfig(0, 10);
            var newValue = new SyncIntervalConfig(0, -2);
            Assert.AreEqual(defaultValue, pc.TimeBetweenSyncs);
            pc.ResetFlags();
            Assert.IsFalse(pc.ConfigChanged);

            pc.TimeBetweenSyncs = newValue;
            var c = pc.TimeBetweenSyncs;
            Assert.IsTrue(pc.ConfigChanged);
            Assert.AreEqual(0, pc.TimeBetweenSyncs.Hours);
            Assert.AreEqual(newValue.Minutes, pc.TimeBetweenSyncs.Minutes);

            var changedInput = "BeatSync.Configs.SyncIntervalConfig:Minutes";
            Assert.IsTrue(c.InvalidInputFixed);
            Assert.AreEqual(changedInput, c.InvalidInputs.First());
            pc.ResetFlags();
            Assert.AreEqual(0, c.InvalidInputs.Length);
            Assert.IsFalse(c.InvalidInputFixed);
            Assert.IsFalse(pc.ConfigChanged);
        }

        [TestMethod]
        public void Invalid_TimeBetweenSyncs_Hours()
        {
            var pc = new PluginConfig();

            var defaultValue = new SyncIntervalConfig(0, 10);
            var newValue = new SyncIntervalConfig(-2, 0);
            Assert.AreEqual(defaultValue, pc.TimeBetweenSyncs);
            pc.ResetFlags();
            Assert.IsFalse(pc.ConfigChanged);

            pc.TimeBetweenSyncs = newValue;
            var c = pc.TimeBetweenSyncs;
            Assert.IsTrue(pc.ConfigChanged);
            Assert.AreEqual(0, pc.TimeBetweenSyncs.Hours);
            Assert.AreEqual(newValue.Minutes, pc.TimeBetweenSyncs.Minutes);

            var changedInput = "BeatSync.Configs.SyncIntervalConfig:Hours";
            Assert.IsTrue(c.InvalidInputFixed);
            Assert.AreEqual(changedInput, c.InvalidInputs.First());
            pc.ResetFlags();
            Assert.AreEqual(0, c.InvalidInputs.Length);
            Assert.IsFalse(c.InvalidInputFixed);
            Assert.IsFalse(pc.ConfigChanged);
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
