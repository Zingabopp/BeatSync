using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Logging;
using BeatSync.Playlists;
using BeatSync.Configs;
using BeatSync.Downloader;
using System.IO;

namespace BeatSyncTests.SongDownloader_Tests
{
    [TestClass]
    public class DownloadJob_Tests
    {
        
        static DownloadJob_Tests()
        {
            TestSetup.Initialize();
            defaultConfig = new PluginConfig().SetDefaults();
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
            SongFeedReaders.WebUtils.WebClient.Timeout = 500;
            
        }

        public static PluginConfig defaultConfig;
        public static string DefaultHistoryPath = Path.GetFullPath(Path.Combine("Output", "DownloadJob_Tests", "BeatSyncHistory.json"));
        public static string DefaultHashCachePath = Path.GetFullPath(Path.Combine("Output", "DownloadJob_Tests", "hashData.dat"));
        public static string DefaultSongsPath = Path.GetFullPath(Path.Combine("Output", "DownloadJob_Tests", "Songs"));
        
        [TestMethod]
        public void SongDoesntExist()
        {
            //Assert.AreEqual(1, SongFeedReaders.WebUtils.WebClient.Timeout);
            //var response = SongFeedReaders.WebUtils.WebClient.GetAsync(@"http://releases.ubuntu.com/18.04.3/ubuntu-18.04.3-live-server-amd64.iso").Result;
            //var dResult = response.Content.ReadAsFileAsync("ubuntu.iso", true).Result;
            var historyManager = new HistoryManager(DefaultHistoryPath);
            var songHasher = new SongHasher(DefaultSongsPath, DefaultHashCachePath);
            historyManager.Initialize();
            var downloader = new SongDownloader(defaultConfig, historyManager, songHasher, DefaultSongsPath);
            var doesntExist = new PlaylistSong("196be1af64958d8b5375b328b0eafae2151d46f8", "Doesn't Exist", "ffff", "Who knows");
            historyManager.TryAdd(doesntExist, 0); // Song is added before it gets to DownloadJob
            var result = downloader.DownloadJob(doesntExist).Result;
            Assert.IsTrue(historyManager.ContainsKey(doesntExist.Hash)); // Keep song in history so it doesn't try to download a non-existant song again.
        }

        [TestMethod]
        public void SongDoesExist()
        {
            var historyManager = new HistoryManager(DefaultHistoryPath);
            var songHasher = new SongHasher(DefaultSongsPath, DefaultHashCachePath);
            historyManager.Initialize();
            var downloader = new SongDownloader(defaultConfig, historyManager, songHasher, DefaultSongsPath);
            var exists = new PlaylistSong("d375405d047d6a2a4dd0f4d40d8da77554f1f677", "Does Exist", "5e20", "ejiejidayo");
            historyManager.TryAdd(exists, 0); // Song is added before it gets to DownloadJob
            var result = downloader.DownloadJob(exists).Result;
            Assert.AreEqual(exists.Hash, result.HashAfterDownload);
            Assert.IsTrue(historyManager.ContainsKey(exists.Hash)); // Successful download is kept in history
        }
    }
}
