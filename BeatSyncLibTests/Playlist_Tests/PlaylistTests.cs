using BeatSyncLib.Playlists;
using BeatSyncLib.Playlists.Legacy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading;

namespace BeatSyncLibTests.Playlist_Tests
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
            PlaylistManager playlistManager = new PlaylistManager("Playlists");
            LegacyPlaylistSong song1 = new LegacyPlaylistSong("63F2998EDBCE2D1AD31917E4F4D4F8D66348105D", "Sun Pluck", "3a9b", "ruckus");
            IPlaylist bookmarks = playlistManager.GetPlaylist(BuiltInPlaylist.BeastSaberBookmarks);
            bookmarks.TryAdd(song1);
            string imageStr = BeatSyncLib.Utilities.Util.ByteArrayToString(((MemoryStream)bookmarks.GetCoverStream()).ToArray());
        }

        [TestMethod]
        public void SetCover_String()
        {
            string coverStr = "base64,ABCD";
            LegacyPlaylist playlist = new LegacyPlaylist("SetCover_String.bplist", "SetCover_String_Test", "PlaylistTests", coverStr);
            Assert.IsTrue(playlist.TryStore());
            string path = Path.GetFullPath(playlist.FileName);
            string imageStr = BeatSyncLib.Utilities.Util.ByteArrayToBase64(((MemoryStream)playlist.GetCoverStream()).ToArray());
            Assert.AreEqual(coverStr, imageStr);
        }

        [TestMethod]
        public void CTSDispose()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            cts.Cancel();
            //cts.Dispose();
            Assert.IsTrue(token.IsCancellationRequested);
            CancellationTokenSource newCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            Assert.IsTrue(newCts.IsCancellationRequested);


        }
    }
}
