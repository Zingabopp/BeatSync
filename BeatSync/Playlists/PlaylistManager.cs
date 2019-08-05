using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSync
{
    public class PlaylistManager
    {
        private const string PlaylistPath = @"Playlists";

        public static Dictionary<string, Playlist> AvailablePlaylists = new Dictionary<string, Playlist>()
        {
            {"BeastSaberBookmarks", new Playlist("", "BeastSaber Bookmarks", "BeatSync", "1") }
        };

        public static void WritePlaylist(Playlist playlist)
        {
            FileIO.WritePlaylist(playlist);
        }

        public Playlist ReadPlaylist(string fileName)
        {
            if (!File.Exists(fileName))
                return null;
            bool oldFormat = fileName.ToLower().EndsWith(".json");
            var path = Path.Combine(PlaylistPath, fileName);
            var playlist = FileIO.ReadPlaylist(path);
            Logger.log.Info($"Found Playlist {playlist.Title}{(oldFormat ? " in old playlist format." : ".")}");
            
            return playlist;
        }
    }
}
