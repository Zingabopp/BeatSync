using BeatSyncPlaylists;
using BeatSyncPlaylists.Legacy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace BeatSyncPlaylistsTests.Manager
{
    [TestClass]
    public class PlaylistReading
    {
        static PlaylistReading()
        {
            TestSetup.Setup();
            DirectorySetup();
        }
        private static string PlaylistDirectory = Path.Combine("Output", "Manager", "PlaylistReading");
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
        public void GetBeatSyncAllPlaylist()
        {
            PlaylistManager manager = new PlaylistManager(TestPlaylistDirectory);
            IPlaylist playlist = manager.GetPlaylist(BuiltInPlaylist.BeatSyncAll);
            Assert.AreEqual("BeatSync Playlist", playlist.Title);
            Assert.AreEqual("BeatSync", playlist.Author);
            Assert.AreEqual("BeatSyncPlaylist", playlist.Filename);
            Assert.AreEqual("Every song BeatSync has downloaded.", playlist.Description);
            Assert.AreEqual(1, playlist.Count);
        }
    }
}
