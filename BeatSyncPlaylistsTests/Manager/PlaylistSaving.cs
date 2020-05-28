#if NCRUNCH
#define CLEANUP
#endif
#define CLEANUP
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
        private static string PlaylistDirectory = Path.Combine("Output", "Manager", "PlaylistSaving");
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
        public void AddExistingAndStoreUpdated()
        {
            PlaylistManager manager = new PlaylistManager(TestPlaylistDirectory);
            IPlaylist playlist = manager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncAll);
            int previousCount = 1;
            Assert.AreEqual("BeatSync Playlist", playlist.Title);
            Assert.AreEqual("BeatSync", playlist.Author);
            Assert.AreEqual("BeatSyncPlaylist", playlist.Filename);
            Assert.AreEqual("Every song BeatSync has downloaded.", playlist.Description);
            Assert.AreEqual(previousCount, playlist.Count);
            string playlistDir = Path.Combine(PlaylistDirectory, "AddExistingAndStoreUpdated");
            Directory.CreateDirectory(playlistDir);
            PlaylistManager outputManager = new PlaylistManager(playlistDir);
            outputManager.AddToCustomPlaylists(playlist);
            outputManager.StoreAllPlaylists();
            playlist.Add("OIU", "AddedSong", "5", "AddedMapper");
            playlist.RaisePlaylistChanged();
            Assert.IsTrue(outputManager.PlaylistIsChanged(playlist));
            outputManager.StoreAllPlaylists();
            string filename = playlist.Filename;
            outputManager = new PlaylistManager(playlistDir);
            IPlaylist updatedPlaylist = outputManager.GetPlaylist(filename);
            Assert.AreEqual(previousCount + 1, updatedPlaylist.Count);

#if CLEANUP
            Directory.Delete(playlistDir, true);
#endif
        }

        [TestMethod]
        public void SaveJsonBeatSyncRecent()
        {
            PlaylistManager readManager = new PlaylistManager(TestPlaylistDirectory);
            IPlaylist playlist = readManager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncRecent);
            Assert.AreEqual("BeatSync Recent", playlist.Title);
            Assert.AreEqual("BeatSync", playlist.Author);
            Assert.AreEqual("BeatSyncRecent", playlist.Filename);
            Assert.AreEqual("Recently downloaded BeatSync songs.", playlist.Description);
            Assert.AreEqual(1, playlist.Count);

            string playlistDir = Path.Combine(PlaylistDirectory, "SaveJsonBeatSyncRecent");
            Directory.CreateDirectory(playlistDir);
            PlaylistManager manager = new PlaylistManager(playlistDir);
            playlist.Add(new LegacyPlaylistSong("FDSA", "AddedSong", "b", "AddedMapper"));
            manager.StorePlaylist(playlist);
            string fileName = Path.Combine(playlistDir, playlist.Filename + ".json");
            Assert.IsTrue(File.Exists(fileName));

            manager = new PlaylistManager(playlistDir);
            playlist = manager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncRecent);
            Assert.AreEqual(2, playlist.Count);
            Assert.AreEqual("AddedSong", playlist[1].Name);
#if CLEANUP
            Directory.Delete(playlistDir, true);
#endif
        }
    }
}
