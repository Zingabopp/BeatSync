using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Playlists;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using BeatSync.Logging;
using BeatSync;

namespace BeatSyncTests.Playlist_Tests
{
    [TestClass]
    public class PlaylistTests
    {
        static PlaylistTests()
        {
            TestSetup.Initialize();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var playlists = PlaylistManager.DefaultPlaylists;
            var song1 = new PlaylistSong("63F2998EDBCE2D1AD31917E4F4D4F8D66348105D", "Sun Pluck", "3a9b", "ruckus");
            var thing = playlists.TryGetValue(BuiltInPlaylist.BeastSaberBookmarks, out var okay);
            var callingAssembly = Assembly.GetCallingAssembly();
            var thingything = BeatSync.Utilities.Util.GetResource(callingAssembly, "BeatSync.Icons.BeatSyncLogoSmall.png");
            var thingyLength = thingything.Length;
            
            var imageStr = okay.Image;
            //StackTest();
            foreach (var playlist in playlists.Values)
            {
                
            }
        }
    }
}
