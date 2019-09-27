using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Configs;
using SongFeedReaders;
using BeatSync.Playlists;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSyncTests.Config_Tests
{
    [TestClass]
    public class LastRun_Tests
    {
        private string DataDir = Path.GetFullPath(@"Data\Config");

        [TestMethod]
        public void FutureDate()
        {
            string configFile = Path.Combine(DataDir, "FutureLastRun.json");
            Assert.IsTrue(File.Exists(configFile));
            var jsonString = File.ReadAllText(configFile);
            var config = JsonConvert.DeserializeObject<PluginConfig>(jsonString);
            Assert.AreEqual(DateTime.MinValue, config.LastRun);
        }
    }
}
