using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSyncLib.Playlists;
using BeatSyncLib.Playlists.Legacy;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using BeatSyncLib.Logging;
using BeatSyncLib;
using System.Threading;
using System.IO;

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
            var song1 = new LegacyPlaylistSong("63F2998EDBCE2D1AD31917E4F4D4F8D66348105D", "Sun Pluck", "3a9b", "ruckus");
            var thing = playlists.TryGetValue(BuiltInPlaylist.BeastSaberBookmarks, out var okay);
            var callingAssembly = Assembly.GetCallingAssembly();
            var thingything = BeatSyncLib.Utilities.Util.GetResource(callingAssembly, "BeatSyncLib.Icons.BeatSyncLogoSmall.png");
            var thingyLength = thingything.Length;
            
            var imageStr = BeatSyncLib.Utilities.Util.ByteArrayToString(((MemoryStream)okay.GetCoverStream()).ToArray());
            //StackTest();
            foreach (var playlist in playlists.Values)
            {
                
            }
        }

        [TestMethod]
        public void CTSDispose()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var token = cts.Token;
            cts.Cancel();
            //cts.Dispose();
            Assert.IsTrue(token.IsCancellationRequested);
            var newCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            Assert.IsTrue(newCts.IsCancellationRequested);


        }
    }
}
