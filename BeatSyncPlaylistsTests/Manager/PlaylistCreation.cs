using BeatSyncPlaylists;
using BeatSyncPlaylists.Legacy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace BeatSyncPlaylistsTests.Manager.PlaylistCreation
{
    [TestClass]
    public class PlaylistCreation
    {
        static PlaylistCreation()
        {
            TestSetup.Setup();
            DirectorySetup();
        }
        private static string PlaylistDirectory = Path.Combine("Output", "Manager", "PlaylistCreation");
        private static string TestPlaylistDirectory = Path.Combine("Data", "LegacyPlaylists");
        private static bool DirectoryIsClean = false;
        private static object _cleanLock = new object();
        private static void DirectorySetup()
        {
            lock (_cleanLock)
            {
                if (DirectoryIsClean)
                    return;
                Directory.CreateDirectory(PlaylistDirectory);
                foreach (var file in Directory.GetFiles(PlaylistDirectory))
                {
                    File.Delete(file);
                }
            }
        }


        [TestMethod]
        public void CreateBuiltIn()
        {
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
    }
}
