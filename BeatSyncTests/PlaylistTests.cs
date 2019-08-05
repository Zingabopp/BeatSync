using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Playlists;

namespace BeatSyncTests
{
    [TestClass]
    public class PlaylistTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var playlists = PlaylistManager.DefaultPlaylists;
            var song1 = new PlaylistSong("63F2998EDBCE2D1AD31917E4F4D4F8D66348105D", "Sun Pluck", "3a9b");
            foreach (var playlist in playlists.Values)
            {
                
            }
        }

        [TestMethod]
        public void ConvertLegacy_Test()
        {
            PlaylistManager.ConvertLegacyPlaylists();
        }
    }
}
