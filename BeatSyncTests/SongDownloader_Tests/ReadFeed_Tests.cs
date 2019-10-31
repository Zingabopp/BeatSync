using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Configs;
using BeatSync.Downloader;
using BeatSync.Logging;
using System.Threading;
using SongFeedReaders.Readers;
using System.Reflection;
using System.Linq;

namespace BeatSyncTests.SongDownloader_Tests
{
    [TestClass]
    public class ReadFeed_Test
    {
        private const string SONGSPATH = @"Output\SongDownloaderTests";
        private static PluginConfig DefaultConfig = new PluginConfig();
        static ReadFeed_Test()
        {
            DefaultConfig.FillDefaults();
            DefaultConfig.BeastSaber.Username = "Zingabopp";
            TestSetup.Initialize();
            var userAgent = $"BeatSyncTests/{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
            SongFeedReaders.WebUtils.WebClient.SetUserAgent(userAgent);
        }

        [TestMethod]
        public void BeastSaberBookmarks()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var config = DefaultConfig;
            var sourceConfig = DefaultConfig.BeastSaber;
            sourceConfig.MaxConcurrentPageChecks = 5;
            var feedConfig = sourceConfig.Bookmarks;
            feedConfig.MaxSongs = 60;
            var songDownloader = new SongDownloader(config, null, null, SONGSPATH);
            var reader = new BeastSaberReader(sourceConfig.Username, sourceConfig.MaxConcurrentPageChecks);
            var settings = feedConfig.ToFeedSettings();

            var result = songDownloader.ReadFeed(reader, settings, 0, null, PlaylistStyle.Append, cts.Token).Result;
            if(!result.Successful)
            {
                Console.WriteLine(result.FaultedResults.First().Exception);
            }
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        [TestMethod]
        public void BeatSaverHot()
        {
            var config = DefaultConfig;
            var sourceConfig = DefaultConfig.BeatSaver;
            sourceConfig.MaxConcurrentPageChecks = 5;
            var feedConfig = sourceConfig.Hot;
            feedConfig.MaxSongs = 60;
            var songDownloader = new SongDownloader(config, null, null, SONGSPATH);
            var reader = new BeatSaverReader();
            var settings = feedConfig.ToFeedSettings();

            CancellationTokenSource cts = new CancellationTokenSource();
            var resultTask = songDownloader.ReadFeed(reader, settings, 0, null, PlaylistStyle.Append, cts.Token);


            var result = resultTask.Result;
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        [TestMethod]
        public void BeatSaverHot_Cancelled()
        {
            var config = DefaultConfig;
            var sourceConfig = DefaultConfig.BeatSaver;
            sourceConfig.MaxConcurrentPageChecks = 5;
            var feedConfig = sourceConfig.Hot;
            feedConfig.MaxSongs = 60;
            var songDownloader = new SongDownloader(config, null, null, SONGSPATH);
            var reader = new BeatSaverReader();
            var settings = feedConfig.ToFeedSettings();

            CancellationTokenSource cts = new CancellationTokenSource(150);
            var resultTask = songDownloader.ReadFeed(reader, settings, 0, null, PlaylistStyle.Append, cts.Token);


            var result = resultTask.Result;
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Successful);
            Assert.AreEqual(FeedResultErrorLevel.Cancelled, result.ErrorLevel);
        }

        [TestMethod]
        public void BeatSaverFavoriteMappers_Cancelled()
        {
            var config = DefaultConfig;
            var sourceConfig = DefaultConfig.BeatSaver;
            sourceConfig.MaxConcurrentPageChecks = 5;
            var feedConfig = sourceConfig.FavoriteMappers;
            feedConfig.MaxSongs = 60;
            var songDownloader = new SongDownloader(config, null, null, SONGSPATH);
            var reader = new BeatSaverReader();
            var settings = (BeatSaverFeedSettings)feedConfig.ToFeedSettings();
            settings.Criteria = "Ruckus";
            CancellationTokenSource cts = new CancellationTokenSource(150);
            var resultTask = songDownloader.ReadFeed(reader, settings, 0, null, PlaylistStyle.Append, cts.Token);


            var result = resultTask.Result;
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Successful);
            Assert.AreEqual(FeedResultErrorLevel.Cancelled, result.ErrorLevel);
        }
    }
}
