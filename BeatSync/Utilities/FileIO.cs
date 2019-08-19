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
        public const int MaxFileSystemPathLength = 259;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public static string LoadStringFromFile(string path)
        {
            string text;
            var bakFile = new FileInfo(path + ".bak");
            if (bakFile.Exists) // .bak file should only exist if there was an error on the last write to path.
            {
                bakFile.CopyTo(path, true);
                bakFile.Delete();
            }
            text = File.ReadAllText(path);
            return text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="text"></param>
        /// <exception cref="IOException">Thrown when there's a problem writing to the file.</exception>
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

        /// <summary>
        /// Writes a playlist to a file.
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        /// <exception cref="IOException">Thrown when there's a problem writing to the file.</exception>
        public static string WritePlaylist(Playlist playlist)
        {
            var path = Path.Combine(PlaylistManager.PlaylistPath,
                playlist.FileName + (playlist.FileName.ToLower().EndsWith(".bplist")
                || playlist.FileName.ToLower().EndsWith(".json") ? "" : ".bplist"));

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
            return path;
        }

        /// <summary>
        /// Updates an existing playlist from a file.
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided playlist is null.</exception>
        public static Playlist ReadPlaylist(Playlist playlist)
        {
            if (playlist == null)
                throw new ArgumentNullException(nameof(playlist), "playlist cannot be null for FileIO.ReadPlaylist().");
            var path = GetPlaylistFilePath(playlist.FileName);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return playlist;
            JsonConvert.PopulateObject(LoadStringFromFile(path), playlist);
            return playlist;
        }

        /// <summary>
        /// Creates a new Playlist from a file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="IOException">Thrown if there's a problem copying/deleting an associated .bak file </exception>
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
        /// Downloads a file from the specified URI to the specified path (path includes file name).
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<string> DownloadFileAsync(Uri uri, string path, bool overwrite = true)
        {
            string actualPath = path;
            if (!overwrite && File.Exists(path))
                return null;
            using (var response = await SongFeedReaders.WebUtils.GetBeatSaverAsync(uri).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    return null;
                try
                {
                    Directory.GetParent(path).Create();

                    actualPath = await response.Content.ReadAsFileAsync(path, overwrite).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    return null;
                }
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
        /// <exception cref="ArgumentNullException">Thrown when zipPath or extractDirectory are null or empty.</exception>
        /// <exception cref="ArgumentException">Thrown when the file at zipPath doesn't exist.</exception>
        public static async Task<string> ExtractZipAsync(string zipPath, string extractDirectory, bool deleteZip = true, bool overwriteTarget = true)
        {
            if (string.IsNullOrEmpty(zipPath))
                throw new ArgumentNullException(nameof(zipPath));
            if (string.IsNullOrEmpty(extractDirectory))
                throw new ArgumentNullException(nameof(extractDirectory));
            FileInfo zipFile = new FileInfo(zipPath);
            if (!zipFile.Exists)
                throw new ArgumentException($"File at zipPath {zipFile.FullName} does not exist.", nameof(zipPath));
            //Logger.log.Info($"Starting ExtractZipAsync for {zipPath}");
            //var extractedFiles = await ExtractAsync(zipFile.FullName, extDir.FullName, overwriteTarget).ConfigureAwait(true);
            //List<string> extractedFiles = new List<string>();
            bool success = false;

            extractDirectory = await Task.Run(() => ExtractTask(zipPath, extractDirectory, overwriteTarget)).ConfigureAwait(false);
            success = !string.IsNullOrEmpty(extractDirectory);

            if (deleteZip)
            {
                try
                {
                    var deleteSuccessful = await TryDeleteAsync(zipFile.FullName).ConfigureAwait(false);
                }
                catch (IOException ex)
                {
                    //Logger.log.Warn($"Unable to delete file {zipFile.FullName}.\n{ex.Message}\n{ex.StackTrace}");
                }
            }
            //Logger.log.Info($"Finished extraction, {success}");
            return extractDirectory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extractDirectory"></param>
        /// <param name="longestEntryName"></param>
        /// <returns></returns>
        /// <exception cref="PathTooLongException">Thrown if shortening the path enough is impossible.</exception>
        public static string GetValidPath(string extractDirectory, int longestEntryName, int buffer = 0)
        {
            var extLength = extractDirectory.Length;
            var dir = new DirectoryInfo(extractDirectory);
            int minLength = dir.Parent.FullName.Length + 2;
            var dirName = dir.Name;
            var diff = MaxFileSystemPathLength - extLength - longestEntryName - buffer;
            if (diff < 0)
            {

                if (dirName.Length + diff > 0)
                {
                    //Logger.log.Warn($"{extractDirectory} is too long, attempting to shorten.");
                    extractDirectory = extractDirectory.Substring(0, minLength + dirName.Length + diff);
                }
                else
                {
                    //Logger.log.Error($"{extractDirectory} is too long, couldn't shorten enough.");
                    throw new PathTooLongException(extractDirectory);
                }
            }
            return extractDirectory;
        }

        private static string ExtractTask(string zipPath, string extractDirectory, bool overwriteTarget = true)
        {
            extractDirectory = Path.GetFullPath(extractDirectory);
            string createdDirectory = null;
            var createdFiles = new List<string>();
            try
            {
                //Logger.log.Info($"ExtractDirectory is {extractDirectory}");
                using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
                using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    //Logger.log.Info("Zip opened");
                    //extractDirectory = GetValidPath(extractDirectory, zipArchive.Entries.Select(e => e.Name).ToArray(), shortDirName, overwriteTarget);
                    var longestEntryName = zipArchive.Entries.Select(e => e.Name).Max(n => n.Length);
                    extractDirectory = GetValidPath(extractDirectory, longestEntryName, 3);
                    if (!overwriteTarget && Directory.Exists(extractDirectory))
                    {
                        int pathNum = 1;
                        string finalPath;
                        do
                        {
                            var append = $" ({pathNum})";
                            finalPath = GetValidPath(extractDirectory, longestEntryName, append.Length) + append; // buffer ensures we aren't continuously cutting off the append value
                            pathNum++;
                        } while (Directory.Exists(finalPath));
                        extractDirectory = finalPath;
                    }
                    var toBeCreated = Directory.Exists(extractDirectory) ? null : extractDirectory; // For cleanup
                    Directory.CreateDirectory(extractDirectory);
                    createdDirectory = string.IsNullOrEmpty(toBeCreated) ? null : extractDirectory;
                    foreach (var entry in zipArchive.Entries)
                    {
                        var entryPath = Path.Combine(extractDirectory, entry.Name);
                        var fileExists = File.Exists(entryPath);
                        if (overwriteTarget || !fileExists)
                        {
                            try
                            {
                                entry.ExtractToFile(entryPath, overwriteTarget);
                                createdFiles.Add(entryPath);
                            }
                            catch (Exception ex)
                            {
                                //Logger.log.Error($"Error extracting {extractDirectory}");
                                //Logger.log.Error(ex);
                                throw ex;
                            }
                        }
                    }
                }
                return extractDirectory;
#pragma warning disable CA1031 // Do not catch general exception types
            }
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                //Logger.log.Error($"Error extracting {extractDirectory}");
                //Logger.log.Error(ex);
                try
                {
                    if (!string.IsNullOrEmpty(createdDirectory))
                    {
                        Directory.Delete(createdDirectory, true);
                    }
                    else
                    {
                        foreach (var file in createdFiles)
                        {
                            File.Delete(file);
                        }
                    }
                }
                catch (Exception cleanUpException)
                {
                    // Failed at cleanup
                }
                return null;
            }
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

        public static Task<bool> TryDeleteAsync(string filePath)
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
