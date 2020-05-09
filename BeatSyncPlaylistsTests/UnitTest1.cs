using BeatSyncPlaylists;
using BeatSyncPlaylists.Legacy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace BeatSyncPlaylistsTests
{
    [TestClass]
    public class UnitTest1
    {
        static UnitTest1() => TestSetup.Setup();
        private static string PlaylistDirectory = Path.Combine("Output", "Tests");
        private static string TestPlaylistDirectory = Path.Combine("Data", "LegacyPlaylists");

        [TestMethod]
        public void TestMethod1()
        {
            Directory.CreateDirectory(PlaylistDirectory);
            foreach (var file in Directory.GetFiles(PlaylistDirectory))
            {
                File.Delete(file);
            }
            PlaylistManager manager = new PlaylistManager(PlaylistDirectory);
            IPlaylist playlist = manager.GetPlaylist(BuiltInPlaylist.BeatSyncAll);
            Assert.IsNotNull(playlist);
            Assert.AreEqual(0, playlist.Count);
            LegacyPlaylistSong testSong = new LegacyPlaylistSong("ASDF", "TestSong", "a", "TestMapper");
            playlist.Add(testSong);
            Assert.IsTrue(playlist.IsDirty);
            manager.StorePlaylist(playlist);
            Assert.IsFalse(playlist.IsDirty);
        }

        [TestMethod]
        public void GetBeatSyncAllPlaylist()
        {
            PlaylistManager manager = new PlaylistManager(TestPlaylistDirectory);
            IPlaylist playlist = manager.GetPlaylist(BuiltInPlaylist.BeatSyncAll);
            Assert.AreEqual(1, playlist.Count);
        }
    }
}
