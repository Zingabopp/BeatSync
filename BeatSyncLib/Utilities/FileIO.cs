using BeatSyncLib.Downloader;
using SongFeedReaders.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using WebUtilities.DownloadContainers;

namespace BeatSyncLib.Utilities
{
    /// <summary>
    /// Encapsulates file I/O operations.
    /// </summary>
    public sealed class FileIO
    {
        /// <summary>
        /// Maximum path length.
        /// </summary>
        private const int MaxFileSystemPathLength = 259;
        private static readonly char[] invalidTrailingPathChars = new char[] { ' ', '.', '-' };
        private readonly ILogger? logger;
        private readonly IWebClient webClient;
        public static char[] InvalidTrailingPathChars() => invalidTrailingPathChars.ToArray();
        /// <summary>
        /// Creates a new <see cref="FileIO"/>.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="logFactory"></param>
        public FileIO(IWebClient client, ILogFactory? logFactory = null)
        {
            logger = logFactory?.GetLogger(GetType().Name);
            webClient = client;
        }

        /// <summary>
        /// Reads text from the given <paramref name="path"/>. If a .bak file exists, attempts to recover it.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="IOException"></exception>
        public string LoadStringFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));
            string text;
            FileInfo? bakFile = new FileInfo(path + ".bak");
            if (bakFile.Exists) // .bak file should only exist if there was an error on the last write to path.
            {
                try
                {
                    bakFile.CopyTo(path, true);
                    bakFile.Delete();
                }
                catch (Exception ex)
                {
                    logger?.Warning($"Error recovering {bakFile.FullName}: {ex.Message}");
                    logger?.Debug(ex.StackTrace);
                }
            }
            text = File.ReadAllText(path);
            return text;
        }

        /// <summary>
        /// Writes a string to file. If a file exists at the path, 
        /// it is copied to a .bak file before being overwritten.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="text"></param>
        /// <exception cref="IOException">Thrown when there's a problem writing to the file.</exception>
        public void WriteStringToFile(string path, string text)
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
        /// Downloads a file from the specified URI to the specified <see cref="DownloadContainer"/>.
        /// All exceptions are stored in the DownloadResult.
        /// </summary>
        /// <param name="downloadUri"></param>
        /// <param name="downloadContainer"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns> 
        public async Task<DownloadedContainer> DownloadFileAsync(Uri downloadUri, DownloadContainer downloadContainer, CancellationToken cancellationToken)
        {
            int statusCode = 0;
            if (downloadUri == null)
                return new DownloadedContainer(null, DownloadResultStatus.InvalidRequest, 0, "DownloadUri is null.",
                    new ArgumentException("DownloadUri is null.", nameof(downloadUri)));
            //if (!overwriteExisting && File.Exists(target))
            //    return new DownloadResult(null, DownloadResultStatus.IOFailed, 0);
            try
            {
                using (IWebResponseMessage? response = await webClient.GetAsync(downloadUri, cancellationToken).ConfigureAwait(false))
                {
                    statusCode = response?.StatusCode ?? 0;
                    if (response == null) throw new WebClientException($"Response was null for '{downloadUri}'.");
                    if (response.Content == null) throw new WebClientException($"Response's content was null for '{downloadUri}'.");
                    try
                    {
                        await downloadContainer.ReceiveDataAsync(response.Content, cancellationToken).ConfigureAwait(false);
                    }
                    catch (IOException ex)
                    {
                        // Also catches DirectoryNotFoundException
                        return new DownloadedContainer(null, DownloadResultStatus.IOFailed, statusCode, response.ReasonPhrase, ex);
                    }
                    catch (InvalidOperationException ex)
                    {
                        // File already exists and overwrite is false.
                        return new DownloadedContainer(null, DownloadResultStatus.IOFailed, statusCode, response.ReasonPhrase, ex);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        return new DownloadedContainer(null, DownloadResultStatus.NetFailed, statusCode, response?.ReasonPhrase, ex);
                    }
                }
            }
            catch (WebClientException ex)
            {
                int faultedCode = ex.Response?.StatusCode ?? 0;
                DownloadResultStatus downloadResultStatus = DownloadResultStatus.NetFailed;
                if (faultedCode == 404)
                    downloadResultStatus = DownloadResultStatus.NetNotFound;
                return new DownloadedContainer(null, downloadResultStatus, faultedCode, ex.Response?.ReasonPhrase, ex.Response?.Exception ?? ex);
            }
            catch (OperationCanceledException ex)
            {
                return new DownloadedContainer(null, DownloadResultStatus.Canceled, 0, ex?.Message, ex);
            }
            catch (Exception ex)
            {
                return new DownloadedContainer(null, DownloadResultStatus.NetFailed, 0, ex?.Message, ex);
            }
            return new DownloadedContainer(downloadContainer, DownloadResultStatus.Success, statusCode);
        }


        /// <summary>
        /// Takes a directory path and, if needed, shortens it to account for the longest file name.
        /// </summary>
        /// <param name="extractDirectory"></param>
        /// <param name="longestEntryName"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        /// <exception cref="PathTooLongException">Thrown if shortening the path enough is impossible.</exception>
        public string GetValidPath(string extractDirectory, int longestEntryName, int padding = 0)
        {
            int extLength = extractDirectory.Length;
            DirectoryInfo? dir = new DirectoryInfo(extractDirectory);
            int minLength = dir.Parent.FullName.Length + 2;
            string? dirName = dir.Name;
            int diff = MaxFileSystemPathLength - extLength - longestEntryName - padding;
            if (diff < 0)
            {

                if (dirName.Length + diff > 0)
                {
                    logger?.Debug($"'{extractDirectory}' is too long, attempting to shorten.");
                    extractDirectory = extractDirectory.Substring(0, minLength + dirName.Length + diff);
                }
                else
                {
                    logger?.Error($"'{extractDirectory}' is too long, couldn't shorten enough.");
                    throw new PathTooLongException(extractDirectory);
                }
            }
            return extractDirectory.TrimEnd(invalidTrailingPathChars);
        }

        public ZipExtractResult ExtractZip(string zipPath, string extractDirectory, bool overwriteTarget = true)
        {
            if (string.IsNullOrEmpty(zipPath))
                throw new ArgumentNullException(nameof(zipPath));
            FileInfo zipFile = new FileInfo(zipPath);
            if (!zipFile.Exists)
                throw new ArgumentException($"File at zipPath {zipFile.FullName} does not exist.", nameof(zipPath));
            using FileStream fs = zipFile.OpenRead();
            return ExtractZip(fs, extractDirectory, overwriteTarget);
        }

        /// <summary>
        /// Extracts a zip file to the specified directory. If an exception is thrown during extraction, it is stored in ZipExtractResult.
        /// </summary>
        /// <param name="zipPath">Path to zip file</param>
        /// <param name="extractDirectory">Directory to extract to</param>
        /// <param name="deleteZip">If true, deletes zip file after extraction</param>
        /// <param name="overwriteTarget">If true, overwrites existing files with the zip's contents</param>
        /// <returns></returns>
        public ZipExtractResult ExtractZip(Stream zipStream, string extractDirectory, bool overwriteTarget = true, string? sourcePath = null)
        {
            if (zipStream == null)
                throw new ArgumentNullException(nameof(zipStream));
            if (string.IsNullOrEmpty(extractDirectory))
                throw new ArgumentNullException(nameof(extractDirectory));

            ZipExtractResult result = new ZipExtractResult
            {
                ResultStatus = ZipExtractResultStatus.Unknown
            };

            string? createdDirectory = null;
            List<string>? createdFiles = new List<string>();
            try
            {
                //Logger.log?.Info($"ExtractDirectory is {extractDirectory}");
                using (ZipArchive? zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                {
                    //Logger.log?.Info("Zip opened");
                    //extractDirectory = GetValidPath(extractDirectory, zipArchive.Entries.Select(e => e.Name).ToArray(), shortDirName, overwriteTarget);
                    int longestEntryName = zipArchive.Entries.Select(e => e.Name).Max(n => n.Length);
                    try
                    {
                        extractDirectory = Path.GetFullPath(extractDirectory); // Could theoretically throw an exception: Argument/ArgumentNull/Security/NotSupported/PathTooLong
                        extractDirectory = GetValidPath(extractDirectory, longestEntryName, 3);
                        if (!overwriteTarget && Directory.Exists(extractDirectory))
                        {
                            int pathNum = 2;
                            string finalPath;
                            do
                            {
                                string? append = $" ({pathNum})";
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
                    string? toBeCreated = extractDirectoryExists ? null : extractDirectory; // For cleanup
                    try { Directory.CreateDirectory(extractDirectory); }
                    catch (Exception ex)
                    {
                        result.Exception = ex;
                        result.ResultStatus = ZipExtractResultStatus.DestinationFailed;
                        return result;
                    }

                    result.CreatedOutputDirectory = !extractDirectoryExists;
                    createdDirectory = string.IsNullOrEmpty(toBeCreated) ? null : extractDirectory;
                    // TODO: Ordering so largest files extracted first. If the extraction is interrupted, theoretically the song's hash won't match Beat Saver's.
                    foreach (ZipArchiveEntry? entry in zipArchive.Entries.OrderByDescending(e => e.Length))
                    {
                        if (!entry.FullName.Equals(entry.Name)) // If false, the entry is a directory or file nested in one
                            continue;
                        string? entryPath = Path.Combine(extractDirectory, entry.Name);
                        bool fileExists = File.Exists(entryPath);
                        if (overwriteTarget || !fileExists)
                        {
                            try
                            {
                                entry.ExtractToFile(entryPath, overwriteTarget);
                                createdFiles.Add(entryPath);
                            }
                            catch (InvalidDataException ex) // Entry is missing, corrupt, or compression method isn't supported
                            {
                                logger?.Error($"Error extracting {extractDirectory}, archive appears to be damaged.");
                                logger?.Error(ex);
                                result.Exception = ex;
                                result.ResultStatus = ZipExtractResultStatus.SourceFailed;
                            }
                            catch (Exception ex)
                            {
                                logger?.Error($"Error extracting {extractDirectory}");
                                logger?.Error(ex);
                                result.Exception = ex;
                                result.ResultStatus = ZipExtractResultStatus.DestinationFailed;

                            }
                            if (result.Exception != null)
                            {
                                foreach (string? file in createdFiles)
                                {
                                    TryDelete(file);
                                }
                                return result;
                            }
                        }
                    }
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
                logger?.Error($"Error extracting zip from {sourcePath ?? "Stream"}");
                logger?.Error(ex);
                try
                {
                    if (!string.IsNullOrEmpty(createdDirectory))
                    {
                        Directory.Delete(createdDirectory, true);
                    }
                    else // TODO: What is this doing here...
                    {
                        foreach (string? file in createdFiles)
                        {
                            File.Delete(file);
                        }
                    }
                }
                catch (Exception cleanUpException)
                {
                    // Failed at cleanup
                    logger?.Debug($"Failed to clean up zip file: {cleanUpException.Message}");
                }

                result.Exception = ex;
                result.ResultStatus = ZipExtractResultStatus.SourceFailed;
                return result;
            }
        }
        /// <summary>
        /// Replaces any illegal directory path characters.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public static string GetSafeDirectoryPath(string directory)
        {
            StringBuilder retStr = new StringBuilder(directory);
            foreach (char character in Path.GetInvalidPathChars())
            {
                retStr.Replace(character.ToString(), string.Empty);
            }
            return retStr.ToString();
        }
        /// <summary>
        /// Replaces any illegal directory filename characters.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetSafeFileName(string fileName)
        {
            StringBuilder retStr = new StringBuilder(fileName);
            foreach (char character in Path.GetInvalidFileNameChars())
            {
                retStr.Replace(character.ToString(), string.Empty);
            }
            return retStr.ToString();
        }

        /// <summary>
        /// Attempts to delete a file at the given path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool TryDelete(string filePath)
        {
            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                logger?.Error($"Failed to delete file at '{filePath}': {ex.Message}");
            }
            return false;
        }
    }

    /// <summary>
    /// Result of a zip extraction.
    /// </summary>
    public class ZipExtractResult
    {
        /// <summary>
        /// Zip extraction's output directory.
        /// </summary>
        public string? OutputDirectory { get; set; }
        /// <summary>
        /// True if a directory was created during extraction.
        /// </summary>
        public bool CreatedOutputDirectory { get; set; }
        /// <summary>
        /// Result of the extraction.
        /// </summary>
        public ZipExtractResultStatus ResultStatus { get; set; }
        /// <summary>
        /// Any <see cref="System.Exception"/> that may have occurred during extraction.
        /// </summary>
        public Exception? Exception { get; set; }
    }
    /// <summary>
    /// Status code for a zip extraction.
    /// </summary>
    public enum ZipExtractResultStatus
    {
        /// <summary>
        /// Extraction hasn't been attempted.
        /// </summary>
        Unknown = 0,
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
