using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Configs;
using SongFeedReaders;
using BeatSync.Playlists;

namespace BeatSyncTests.Config_Tests
{
    [TestClass]
    public class SyncIntervalConfig_Tests
    {
        [TestMethod]
        public void GetHashCode_CollisionTest()
        {
            int max = 1000;
            var intervalOne = new SyncIntervalConfig(0, 0);
            var intervalTwo = new SyncIntervalConfig(0, 0);
            for (int i = 0; i < max; i++)
            {
                intervalOne.Hours = i;
                intervalTwo.Minutes = i;
                for (int j = 0; j < max; j++)
                {
                    intervalOne.Minutes = j;
                    intervalTwo.Hours = i;
                    int oneHash = intervalOne.GetHashCode();
                    int twoHash = intervalTwo.GetHashCode();
                    if (!intervalOne.Equals(intervalTwo))
                        Assert.AreNotEqual(oneHash, twoHash);
                }
            }
        }
    }
}
