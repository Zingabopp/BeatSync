using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace BeatSync
{
    public static class FileIO
    {
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
            var path = playlist.fileName;
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

        public static Playlist ReadPlaylist(string path)
        {
            Playlist playlist = null;
            string text = string.Empty;
            var bakFile = new FileInfo(path + ".bak");
            var file = new FileInfo(path);
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
            return playlist;
        }

    }
}
