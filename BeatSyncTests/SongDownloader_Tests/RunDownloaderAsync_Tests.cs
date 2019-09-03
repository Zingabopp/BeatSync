using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync;
using BeatSync.Logging;
using BeatSync.Configs;
using System.IO;
using BeatSync.Downloader;
using BeatSync.Playlists;
using System.Linq;
using System.Reflection;

namespace BeatSyncTests.SongDownloader_Tests
{
    [TestClass]
    public class RunDownloaderAsync_Tests
    {

        static RunDownloaderAsync_Tests()
        {
            TestSetup.Initialize();
            defaultConfig = new PluginConfig().SetDefaults();
            var userAgent = $"BeatSyncTests/{Assembly.GetExecutingAssembly().GetName().Version.ToString()}";
            SongFeedReaders.WebUtils.Initialize(new WebUtilities.WebWrapper.WebClientWrapper());
            SongFeedReaders.WebUtils.WebClient.SetUserAgent(userAgent);

        }

        public static PluginConfig defaultConfig;
        public static string DefaultHistoryPath = Path.GetFullPath(Path.Combine("Output", "RunDownloaderAsync_Tests", "BeatSyncHistory.json"));
        public static string DefaultHashCachePath = Path.GetFullPath(Path.Combine("Output", "RunDownloaderAsync_Tests", "hashData.dat"));
        public static string DefaultSongsPath = Path.GetFullPath(Path.Combine("Output", "RunDownloaderAsync_Tests", "Songs"));

        [TestMethod]
        public void SingleThreaded_FirstSongNotFound()
        {
            var songsPath = Path.Combine(DefaultSongsPath, "SingleThreaded_FirstSongNotFound");
            if (Directory.Exists(songsPath))
                Directory.Delete(songsPath, true);
            var downloader = new SongDownloader(defaultConfig, null, null, songsPath);
            var testSong1 = new PlaylistSong("A65C4B43A6BC543AC6B534A65CB43B6AC354", "NotFoundSong", "fff0", "MapperName");
            var testSong2 = new PlaylistSong("19f2879d11a91b51a5c090d63471c3e8d9b7aee3", "Believer", "b", "rustic");
            downloader.DownloadQueue.Enqueue(testSong1);
            downloader.DownloadQueue.Enqueue(testSong2);
            var results = downloader.RunDownloaderAsync(1).Result;
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(404, results[0].DownloadResult.HttpStatusCode);
            Assert.AreEqual(true, results[1].Successful);
        }
    }
}
