using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using BeatSync.Playlists;
using BeatSync.Logging;
using System.Threading;
using System.IO.Compression;

namespace BeatSync.Utilities
{
    public static class FileIO
    {
        //private const string PlaylistPath = @"Playlists";

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
            var path = Path.Combine(PlaylistManager.PlaylistPath,
                playlist.FileName + (playlist.FileName.ToLower().EndsWith(".bplist") ? "" : ".bplist"));
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", true);
                File.Delete(path);
            }
            using (var sw = File.CreateText(path))
            {
                var serializer = new JsonSerializer() { Formatting = Formatting.Indented };
                serializer.Serialize(sw, playlist);
            }
            File.Delete(path + ".bak");
        }

        public static Playlist ReadPlaylist(Playlist playlist)
        {
            var path = GetPlaylistFilePath(playlist.FileName);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return playlist;
            JsonConvert.PopulateObject(FileIO.LoadStringFromFile(path), playlist);
            return playlist;
        }

        public static Playlist ReadPlaylist(string fileName)
        {
            var path = GetPlaylistFilePath(fileName);
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
            //Logger.log?.Info($"Found Playlist {playlist.Title}");

            return playlist;
        }

        /// <summary>
        /// Gets the path to the provided playlist file name.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetPlaylistFilePath(string fileName, bool getDisabled = false)
        {

            //if (PlaylistExtensions.Any(e => fileName.EndsWith(e)))
            //{
            //    // fileName already has a valid extension
            //    return fileName.Contains(@"Playlists\") ? fileName : Path.Combine(PlaylistPath, fileName);
            //}
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);
            else if (!getDisabled)
                return null;

            var path = Path.Combine(PlaylistManager.DisabledPlaylistsPath, fileName);
            if (string.IsNullOrEmpty(path))
                return null;
            return path;

        }

        /// <summary>
        /// Downloads a file from the specified URI to the specified path (path include file name).
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<string> DownloadFileAsync(Uri uri, string path, bool overwrite = true)
        {
            string actualPath = path;
            using (var response = await SongFeedReaders.WebUtils.GetBeatSaverAsync(uri).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    return null;
                actualPath = await response.Content.ReadAsFileAsync(path, overwrite).ConfigureAwait(false);
            }
            return actualPath;
        }

        /// <summary>
        /// Extracts a zip file to the specified directory.
        /// </summary>
        /// <param name="zipPath">Path to zip file</param>
        /// <param name="extractDirectory">Directory to extract to</param>
        /// <param name="deleteZip">If true, deletes zip file after extraction</param>
        /// <param name="overwriteTarget">If true, overwrites existing files with the zip's contents</param>
        /// <returns></returns>
        public static async Task<string[]> ExtractZipAsync(string zipPath, string extractDirectory, bool deleteZip = true, bool overwriteTarget = true)
        {
            if (string.IsNullOrEmpty(zipPath))
                throw new ArgumentNullException(nameof(zipPath));
            if (string.IsNullOrEmpty(extractDirectory))
                throw new ArgumentNullException(nameof(extractDirectory));

            FileInfo zipFile = new FileInfo(zipPath);
            DirectoryInfo extDir = new DirectoryInfo(extractDirectory);
            if (!zipFile.Exists)
                throw new ArgumentException($"File at zipPath {zipFile.FullName} does not exist.", nameof(zipPath));
            extDir.Create();
            //var extractedFiles = await ExtractAsync(zipFile.FullName, extDir.FullName, overwriteTarget).ConfigureAwait(true);
            string[] extractedFiles = null;
            if (await SongFeedReaders.Utilities.WaitUntil(() =>
            {
                try
                {
                    using (ZipArchive zipArchive = ZipFile.OpenRead(zipPath))
                        zipArchive.ExtractToDirectory(extDir.FullName);
                    return true;
#pragma warning disable CA1031 // Do not catch general exception types
                }
                catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    return false;
                }

            }, 25, 3000).ConfigureAwait(false))
            {

            }
            if (deleteZip)
            {
                try
                {
                    var deleteSuccessful = await TryDeleteAsync(zipFile.FullName).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    Logger.log.Warn($"Unable to delete file {zipFile.FullName}.\n{ex.Message}\n{ex.StackTrace}");
                }
            }
            return extractedFiles;
        }

        public static string GetSafeDirectoryPath(string directory)
        {
            StringBuilder retStr = new StringBuilder(directory);
            foreach (var character in Path.GetInvalidPathChars())
            {
                retStr.Replace(character.ToString(), string.Empty);
            }
            return retStr.ToString();
        }

        public static string GetSafeFileName(string fileName)
        {
            StringBuilder retStr = new StringBuilder(fileName);
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                retStr.Replace(character.ToString(), string.Empty);
            }
            return retStr.ToString();
        }

        private static Task<bool> TryDeleteAsync(string filePath)
        {
            var timeoutSource = new CancellationTokenSource(3000);
            var timeoutToken = timeoutSource.Token;
            return SongFeedReaders.Utilities.WaitUntil(() =>
            {
                try
                {
                    File.Delete(filePath);
                    timeoutSource.Dispose();
                    return true;
                }
                catch (Exception)
                {
                    timeoutSource.Dispose();
                    throw;
                }
            }, timeoutToken);
        }

        private enum ZipExtractResult
        {

        }
    }
}
