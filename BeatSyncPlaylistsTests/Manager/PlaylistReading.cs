#if NCRUNCH
#define CLEANUP
#endif
#define CLEANUP
using BeatSyncPlaylists;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public void GetBuiltInPlaylist()
        {
            PlaylistManager manager = new PlaylistManager(TestPlaylistDirectory);
            IPlaylist playlist = manager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncAll);
            Assert.AreEqual("BeatSync Playlist", playlist.Title);
            Assert.AreEqual("BeatSync", playlist.Author);
            Assert.AreEqual("BeatSyncPlaylist", playlist.Filename);
            Assert.AreEqual("Every song BeatSync has downloaded.", playlist.Description);
            Assert.AreEqual(1, playlist.Count);
        }

        [TestMethod]
        public void CreateAndGetAllBuiltin()
        {
            string outputDir = Path.Combine(PlaylistDirectory, "CreateAndGetAllBuiltin");
            Directory.CreateDirectory(outputDir);
            PlaylistManager manager = new PlaylistManager(outputDir);
            Array rawValues = Enum.GetValues(typeof(BuiltInPlaylist));
            List<BuiltInPlaylist> allTypes = new List<BuiltInPlaylist>(rawValues.Length);
            int valIndex = 0;
            foreach (var value in rawValues)
            {
                BuiltInPlaylist castVal = (BuiltInPlaylist)value;
                if (castVal != BuiltInPlaylist.BeatSaverMapper)
                {
                    allTypes.Add(castVal);

                    IPlaylist playlist = manager.GetOrAddPlaylist(castVal);
                    playlist.Add(valIndex.ToString(), $"InPlaylist {valIndex}", valIndex.ToString(), "TestMapper");
                    playlist.RaisePlaylistChanged();
                    valIndex++;
                }
            }
            manager.StoreAllPlaylists(); 
            manager = new PlaylistManager(outputDir);
            for (int i = 0; i < allTypes.Count; i++)
            {
                BuiltInPlaylist current = allTypes[i];
                IPlaylist playlist = manager.GetOrAddPlaylist(current);
                Assert.AreEqual(1, playlist.Count);
                Assert.AreEqual(i.ToString(), playlist[0].Hash, $"Failed on {current}");
            }
#if CLEANUP
            Directory.Delete(outputDir, true);
#endif
        }

        [TestMethod]
        public void GetJsonBeatSyncRecent()
        {
            PlaylistManager manager = new PlaylistManager(TestPlaylistDirectory);
            IPlaylist playlist = manager.GetOrAddPlaylist(BuiltInPlaylist.BeatSyncRecent);
            Assert.AreEqual("BeatSync Recent", playlist.Title);
            Assert.AreEqual("BeatSync", playlist.Author);
            Assert.AreEqual("BeatSyncRecent", playlist.Filename);
            Assert.AreEqual("Recently downloaded BeatSync songs.", playlist.Description);
            Assert.AreEqual(1, playlist.Count);
            Assert.IsFalse(manager.PlaylistIsChanged(playlist));
        }
    }
}
