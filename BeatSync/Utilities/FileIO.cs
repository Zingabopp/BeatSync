using BeatSync.Playlists;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                Logger.log?.Debug($"Found backup playlist file {bakFile}, using this instead.");
                bakFile.CopyTo(path, true);
                bakFile.Delete();
            }
            var serializer = new JsonSerializer();
            using (var sr = File.OpenText(path))
            {
                playlist = (Playlist)serializer.Deserialize(sr, typeof(Playlist));
            }
            playlist.FileName = fileName;
            Logger.log?.Debug($"ReadPlaylist(): Found Playlist {playlist.Title}");

            return playlist;
        }

        /// <summary>
        /// Gets the path to the provided playlist file name. TODO: This needs work
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetPlaylistFilePath(string fileName, bool getDisabled = false)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);
            var path = Path.Combine(PlaylistManager.PlaylistPath, fileName);
            if (File.Exists(path))
                return Path.GetFullPath(path);
            else if (!getDisabled)
                return null;

            path = Path.Combine(PlaylistManager.DisabledPlaylistsPath, fileName);
            if (string.IsNullOrEmpty(path))
                return null;
            if (File.Exists(path))
                return path;
            return null;

        }



        /// <summary>
        /// Downloads a file from the specified URI to the specified path (path includes file name).
        /// Creates the target directory if it doesn't exist. All exceptions are stored in the DownloadResult.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="path"></param>
        /// <returns></returns> 
        public static async Task<DownloadResult> DownloadFileAsync(Uri uri, string path, bool overwrite = true)
        {
            string actualPath = path;
            int statusCode = 0;
            if (uri == null)
                return new DownloadResult(null, DownloadResultStatus.InvalidRequest, 0);
            if (!overwrite && File.Exists(path))
                return new DownloadResult(null, DownloadResultStatus.IOFailed, 0);
            try
            {
                using (var response = await SongFeedReaders.WebUtils.GetBeatSaverAsync(uri, 30, 2).ConfigureAwait(false))
                {
                    statusCode = response?.StatusCode ?? 0;
                    if (!(response?.IsSuccessStatusCode ?? false))
                        return new DownloadResult(null, DownloadResultStatus.NetFailed, statusCode, response.ReasonPhrase, response.Exception);
                    try
                    {
                        Directory.GetParent(path).Create();
                        actualPath = await response.Content.ReadAsFileAsync(path, overwrite).ConfigureAwait(false);
                    }
                    catch (IOException ex)
                    {
                        // Also catches DirectoryNotFoundException
                        return new DownloadResult(null, DownloadResultStatus.IOFailed, statusCode, response.ReasonPhrase, ex);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // File already exists and overwrite is false.
                        return new DownloadResult(null, DownloadResultStatus.IOFailed, statusCode, response.ReasonPhrase, ex);
                    }
                    catch (Exception ex)
                    {
                        return new DownloadResult(null, DownloadResultStatus.Unknown, statusCode, response?.ReasonPhrase, ex);
                    }
                }
            }catch(Exception ex)
            {
                return new DownloadResult(null, DownloadResultStatus.NetFailed, 0, ex?.Message, ex);
            }
            return new DownloadResult(actualPath, DownloadResultStatus.Success, statusCode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extractDirectory"></param>
        /// <param name="longestEntryName"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        /// <exception cref="PathTooLongException">Thrown if shortening the path enough is impossible.</exception>
        public static string GetValidPath(string extractDirectory, int longestEntryName, int padding = 0)
        {
            var extLength = extractDirectory.Length;
            var dir = new DirectoryInfo(extractDirectory);
            int minLength = dir.Parent.FullName.Length + 2;
            var dirName = dir.Name;
            var diff = MaxFileSystemPathLength - extLength - longestEntryName - padding;
            if (diff < 0)
            {

                if (dirName.Length + diff > 0)
                {
                    //Logger.log?.Warn($"{extractDirectory} is too long, attempting to shorten.");
                    extractDirectory = extractDirectory.Substring(0, minLength + dirName.Length + diff);
                }
                else
                {
                    //Logger.log?.Error($"{extractDirectory} is too long, couldn't shorten enough.");
                    throw new PathTooLongException(extractDirectory);
                }
            }
            return extractDirectory;
        }

        /// <summary>
        /// Extracts a zip file to the specified directory. If an exception is thrown during extraction, it is stored in ZipExtractResult.
        /// </summary>
        /// <param name="zipPath">Path to zip file</param>
        /// <param name="extractDirectory">Directory to extract to</param>
        /// <param name="deleteZip">If true, deletes zip file after extraction</param>
        /// <param name="overwriteTarget">If true, overwrites existing files with the zip's contents</param>
        /// <returns></returns>
        public static ZipExtractResult ExtractZip(string zipPath, string extractDirectory, bool overwriteTarget = true)
        {
            if (string.IsNullOrEmpty(zipPath))
                throw new ArgumentNullException(nameof(zipPath));
            if (string.IsNullOrEmpty(extractDirectory))
                throw new ArgumentNullException(nameof(extractDirectory));
            FileInfo zipFile = new FileInfo(zipPath);
            if (!zipFile.Exists)
                throw new ArgumentException($"File at zipPath {zipFile.FullName} does not exist.", nameof(zipPath));

            ZipExtractResult result = new ZipExtractResult
            {
                SourceZip = zipPath,
                ResultStatus = ZipExtractResultStatus.NotStarted
            };

            string createdDirectory = null;
            var createdFiles = new List<string>();
            try
            {
                //Logger.log?.Info($"ExtractDirectory is {extractDirectory}");
                using (var fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
                using (var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    //Logger.log?.Info("Zip opened");
                    //extractDirectory = GetValidPath(extractDirectory, zipArchive.Entries.Select(e => e.Name).ToArray(), shortDirName, overwriteTarget);
                    var longestEntryName = zipArchive.Entries.Select(e => e.Name).Max(n => n.Length);
                    try
                    {
                        extractDirectory = Path.GetFullPath(extractDirectory); // Could theoretically throw an exception: Argument/ArgumentNull/Security/NotSupported/PathTooLong
                        extractDirectory = GetValidPath(extractDirectory, longestEntryName, 3);
                        if (!overwriteTarget && Directory.Exists(extractDirectory))
                        {
                            int pathNum = 1;
                            string finalPath;
                            do
                            {
                                var append = $" ({pathNum})";
                                finalPath = GetValidPath(extractDirectory, longestEntryName, append.Length) + append; // padding ensures we aren't continuously cutting off the append value
                                pathNum++;
                            } while (Directory.Exists(finalPath));
                            extractDirectory = finalPath;
                        }
                    }
                    catch (PathTooLongException ex)
                    {
                        result.Exception = ex;
                        result.ResultStatus = ZipExtractResultStatus.DestinationFailed;
                        return result;
                    }
                    result.OutputDirectory = extractDirectory;
                    bool extractDirectoryExists = Directory.Exists(extractDirectory);
                    var toBeCreated = extractDirectoryExists ? null : extractDirectory; // For cleanup
                    try { Directory.CreateDirectory(extractDirectory); }
                    catch (Exception ex)
                    {
                        result.Exception = ex;
                        result.ResultStatus = ZipExtractResultStatus.DestinationFailed;
                        return result;
                    }

                    result.CreatedOutputDirectory = !extractDirectoryExists;
                    createdDirectory = string.IsNullOrEmpty(toBeCreated) ? null : extractDirectory;

                    foreach (var entry in zipArchive.Entries)
                    {
                        if (!entry.FullName.Equals(entry.Name)) // If false, the entry is a directory or file nested in one
                            continue;
                        var entryPath = Path.Combine(extractDirectory, entry.Name);
                        var fileExists = File.Exists(entryPath);
                        if (overwriteTarget || !fileExists)
                        {
                            try
                            {
                                entry.ExtractToFile(entryPath, overwriteTarget);
                                createdFiles.Add(entryPath);
                            }
                            catch (InvalidDataException ex) // Entry is missing, corrupt, or compression method isn't supported
                            {
                                Logger.log?.Error($"Error extracting {extractDirectory}, archive appears to be damaged.");
                                Logger.log?.Error(ex);
                                result.Exception = ex;
                                result.ResultStatus = ZipExtractResultStatus.SourceFailed;
                                result.ExtractedFiles = createdFiles.ToArray();
                            }
                            catch (Exception ex)
                            {
                                Logger.log?.Error($"Error extracting {extractDirectory}");
                                Logger.log?.Error(ex);
                                result.Exception = ex;
                                result.ResultStatus = ZipExtractResultStatus.DestinationFailed;
                                result.ExtractedFiles = createdFiles.ToArray();

                            }
                            if (result.Exception != null)
                            {
                                foreach (var file in createdFiles)
                                {
                                    TryDeleteAsync(file).Wait();
                                }
                                return result;
                            }
                        }
                    }
                    result.ExtractedFiles = createdFiles.ToArray();
                }
                result.ResultStatus = ZipExtractResultStatus.Success;
                return result;
#pragma warning disable CA1031 // Do not catch general exception types
            }
            catch (InvalidDataException ex) // FileStream is not in the zip archive format.
            {
                result.ResultStatus = ZipExtractResultStatus.SourceFailed;
                result.Exception = ex;
                return result;
            }
            catch (Exception ex) // If exception is thrown here, it probably happened when the FileStream was opened.
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Logger.log?.Error($"Error opening FileStream for {zipPath}");
                Logger.log?.Error(ex);
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

                result.Exception = ex;
                result.ResultStatus = ZipExtractResultStatus.SourceFailed;
                return result;
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
    }

    public class DownloadResult
    {
        public DownloadResult(string path, DownloadResultStatus status, int httpStatus, string reason = null, Exception exception = null)
        {
            FilePath = path;
            Status = status;
            HttpStatusCode = httpStatus;
            Reason = reason;
            Exception = exception;
        }
        public string FilePath { get; private set; }
        public string Reason { get; private set; }
        public DownloadResultStatus Status { get; private set; }
        public int HttpStatusCode { get; private set; }
        public Exception Exception { get; private set; }
    }

    public enum DownloadResultStatus
    {
        Unknown = 0,
        Success = 1,
        NetFailed = 2,
        IOFailed = 3,
        InvalidRequest = 4
    }

    public class ZipExtractResult
    {
        public string SourceZip { get; set; }
        public string OutputDirectory { get; set; }
        public bool CreatedOutputDirectory { get; set; }
        public string[] ExtractedFiles { get; set; }
        public ZipExtractResultStatus ResultStatus { get; set; }
        public Exception Exception { get; set; }
    }

    public enum ZipExtractResultStatus
    {
        /// <summary>
        /// Extraction hasn't been attempted.
        /// </summary>
        NotStarted = 0,
        /// <summary>
        /// Extraction was successful.
        /// </summary>
        Success = 1,
        /// <summary>
        /// Problem with the zip source.
        /// </summary>
        SourceFailed = 2,
        /// <summary>
        /// Problem with the destination target.
        /// </summary>
        DestinationFailed = 3
    }
}
