using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using BeatSync.Playlists;
using BeatSync.Logging;

namespace BeatSync
{
    public static class FileIO
    {
        private const string PlaylistPath = @"Playlists";
        private static readonly string[] PlaylistExtensions = new string[] { ".bplist", ".json" };
        public static string LoadStringFromFile(string path)
        {
            string text = string.Empty;
            var bakFile = new FileInfo(path + ".bak");
            var file = new FileInfo(path);
            if (bakFile.Exists) // .bak file should only exist if there was an error on the last write to path.
            {
                bakFile.CopyTo(path, true);
                bakFile.Delete();
            }
            text = File.ReadAllText(path);
            return text;
        }

        public static void WriteStringToFile(string path, string text)
        {
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", true);
                File.Delete(path);
            }
            File.WriteAllText(path, text);
            File.Delete(path + ".bak");
        }

        public static void WritePlaylist(Playlist playlist)
        {
            var path = Path.Combine(PlaylistPath, playlist.FileName);
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", true);
                File.Delete(path);
            }
            using (var sw = File.CreateText(path))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(sw, playlist);
            }
            File.Delete(path + ".bak");
        }

        public static Playlist ReadPlaylist(Playlist playlist)
        {
            var match = Directory.EnumerateFiles(PlaylistPath, playlist.FileName + ".*");
            var path = match.FirstOrDefault();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return playlist;
            JsonConvert.PopulateObject(FileIO.LoadStringFromFile(path), playlist);
            return playlist;
        }

        public static Playlist ReadPlaylist(string fileName)
        {
            var path = GetFilePath(fileName);
            Playlist playlist = null;
            var bakFile = new FileInfo(path + ".bak");
            if (bakFile.Exists) // .bak file should only exist if there was an error on the last write to path.
            {
                bakFile.CopyTo(path, true);
                bakFile.Delete();
            }
            var serializer = new JsonSerializer();
            using (var sr = File.OpenText(path))
            {
                playlist = (Playlist)serializer.Deserialize(sr, typeof(Playlist));
            }
            playlist.FileName = fileName;
            Logger.log?.Info($"Found Playlist {playlist.Title}");

            return playlist;
        }

        public static string GetFilePath(string fileName)
        {

            //if (PlaylistExtensions.Any(e => fileName.EndsWith(e)))
            //{
            //    // fileName already has a valid extension
            //    return fileName.Contains(@"Playlists\") ? fileName : Path.Combine(PlaylistPath, fileName);
            //}

            var match = Directory.EnumerateFiles(PlaylistPath, fileName + "*");
            var path = match.FirstOrDefault();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            return path;

        }
    }
}
