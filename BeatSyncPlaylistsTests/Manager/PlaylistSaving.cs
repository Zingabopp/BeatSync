using BeatSyncPlaylists;
using BeatSyncPlaylists.Legacy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace BeatSyncPlaylistsTests.Manager
{
    [TestClass]
    public class PlaylistSaving
    {
        static PlaylistSaving()
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

        [TestMethod]
        public void SaveJsonBeatSyncRecent()
        {
            PlaylistManager readManager = new PlaylistManager(TestPlaylistDirectory);
            IPlaylist playlist = readManager.GetPlaylist(BuiltInPlaylist.BeatSyncRecent);
            Assert.AreEqual("BeatSync Recent", playlist.Title);
            Assert.AreEqual("BeatSync", playlist.Author);
            Assert.AreEqual("BeatSyncRecent", playlist.Filename);
            Assert.AreEqual("Recently downloaded BeatSync songs.", playlist.Description);
            Assert.AreEqual(1, playlist.Count);

            PlaylistManager manager = new PlaylistManager(PlaylistDirectory);
            playlist.Add(new LegacyPlaylistSong("FDSA", "AddedSong", "b", "AddedMapper"));
            manager.StorePlaylist(playlist);
            string fileName = Path.Combine(PlaylistDirectory, playlist.Filename + ".json");
            Assert.IsTrue(File.Exists(fileName));

            manager = new PlaylistManager(PlaylistDirectory);
            playlist = manager.GetPlaylist(BuiltInPlaylist.BeatSyncRecent);
            Assert.AreEqual(2, playlist.Count);
            Assert.AreEqual("AddedSong", playlist[1].Name);
        }
    }
}
