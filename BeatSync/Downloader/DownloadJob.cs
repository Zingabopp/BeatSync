using BeatSync.Playlists;
using BeatSync.Utilities;
using static SongFeedReaders.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SongFeedReaders;
using SongFeedReaders.Readers;

namespace BeatSync.Downloader
{
    public class DownloadJob : IDownloadJob
    {
        private static readonly string SongTempPath = Path.GetFullPath(Path.Combine("UserData", "BeatSyncTemp"));
        private readonly string CustomLevelsPath;
        private const string BeatSaverDownloadUrlBase = "https://beatsaver.com/api/download/hash/";
        public Exception Exception { get; private set; }
        public event EventHandler<JobStartedEventArgs> OnJobStarted;
        public event EventHandler<JobFinishedEventArgs> OnJobFinished;
        //public PlaylistSong Song { get; private set; }
        public string SongHash { get; private set; }
        public string SongKey { get; private set; }
        public string SongName { get; private set; }
        public string LevelAuthorName { get; private set; }
        private WeakReference<Func<bool>> PauseFlag = new WeakReference<Func<bool>>(() => SongFeedReaders.Utilities.IsPaused);
        public bool Paused
        {
            get
            {
                if (PauseFlag == null)
                    return false;
                if (PauseFlag.TryGetTarget(out var reference))
                    return reference.Invoke();
                return false;
            }
        }
        public string SongDirectory { get; private set; }
        private string _defaultSongDirectoryName;
        public JobResult Result { get; private set; }

        public JobStatus Status { get; private set; }

        /// <summary>
        /// Private constructor to use with the others.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <param name="customLevelsPath"></param>
        private DownloadJob(string customLevelsPath)
        {
            if (string.IsNullOrEmpty(customLevelsPath))
                throw new ArgumentNullException(nameof(customLevelsPath), "customLevelsPath cannot be null when creating a DownloadJob.");
            CustomLevelsPath = customLevelsPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <param name="song"></param>
        /// <param name="customLevelsPath"></param>
        public DownloadJob(PlaylistSong song, string customLevelsPath)
            : this(customLevelsPath)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "song cannot be null.");
            if (string.IsNullOrEmpty(song.Hash))
                throw new ArgumentException("PlaylistSong's hash cannot be null.", nameof(song));
            if (string.IsNullOrEmpty(customLevelsPath))
                throw new ArgumentNullException(nameof(customLevelsPath), "customLevelsPath cannot be null.");
            //Song = song;
            SongHash = song.Hash;
            SongKey = song.Key;
            SongName = song.Name;
            LevelAuthorName = song.LevelAuthorName;
            _defaultSongDirectoryName = Util.GetSongDirectoryName(SongKey, SongName, LevelAuthorName);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <param name="songHash"></param>
        /// <param name="songName"></param>
        /// <param name="songKey"></param>
        /// <param name="mapperName"></param>
        /// <param name="customLevelsPath"></param>
        public DownloadJob(string songHash, string songName, string songKey, string mapperName, string customLevelsPath)
            : this(customLevelsPath)
        {
            if (string.IsNullOrEmpty(songHash))
                throw new ArgumentNullException();
            SongHash = songHash;
            SongKey = songKey;
            SongName = songName;
            LevelAuthorName = mapperName;
            _defaultSongDirectoryName = Util.GetSongDirectoryName(SongKey, SongName, LevelAuthorName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <param name="song"></param>
        /// <param name="customLevelsPath"></param>
        public DownloadJob(ScrapedSong song, string customLevelsPath)
            : this(customLevelsPath)
        {
            SongHash = song.Hash;
            SongKey = song.SongKey;
            SongName = song.SongName;
            LevelAuthorName = song.MapperName;
            _defaultSongDirectoryName = Util.GetSongDirectoryName(SongKey, SongName, LevelAuthorName);
        }

        private DownloadResult downloadResult;
        private ZipExtractResult zipResult;
        private string hashAfterDownload;

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            Status = JobStatus.Downloading;

            bool overwrite = true;
            string extractDirectory = null;
            try
            {
                if (string.IsNullOrEmpty(SongKey))
                {
                    var songDetail = await BeatSaverReader.GetSongByHashAsync(SongHash);
                    SongKey = songDetail.SongKey;
                    _defaultSongDirectoryName = Util.GetSongDirectoryName(SongKey, SongName, LevelAuthorName);
                }
                OnJobStarted?.Invoke(this, new JobStartedEventArgs(SongHash, SongKey, SongName, LevelAuthorName));
                var songDirPath = Path.GetFullPath(Path.Combine(CustomLevelsPath, _defaultSongDirectoryName));
                bool directoryCreated = !Directory.Exists(songDirPath);
                if (cancellationToken.IsCancellationRequested)
                {
                    FinishJob(true);
                    return;
                }
                if (Paused)
                {
                    if (!(await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false)))
                    {
                        FinishJob(true); // Cancellation requested while waiting for Unpause
                        return;
                    }
                }
                
                // Download Zip
                downloadResult = await DownloadSongAsync(SongTempPath).ConfigureAwait(false);

                if ((downloadResult?.Status ?? DownloadResultStatus.Unknown) == DownloadResultStatus.Success)
                {
                    // Extract Zip
                    if (cancellationToken.IsCancellationRequested)
                    {
                        FinishJob(true);
                        return;
                    }
                    if (Paused)
                    {
                        if (!(await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false)))
                        {
                            FinishJob(true); // Cancellation requested while waiting for Unpause
                            return;
                        }
                    }
                    Status = JobStatus.Extracting;

                    zipResult = await Task.Run(() => FileIO.ExtractZip(downloadResult.FilePath, songDirPath, overwrite)).ConfigureAwait(false);
                    // Try to delete zip file
                    try
                    {
                        var deleteSuccessful = await FileIO.TryDeleteAsync(downloadResult.FilePath).ConfigureAwait(false);
                    }
                    catch (IOException ex)
                    {
                        Logger.log?.Warn($"Unable to delete zip file after extraction: {downloadResult.FilePath}.\n{ex.Message}");
                    }
                    extractDirectory = Path.GetFullPath(zipResult.OutputDirectory);
                    if (!overwrite && !songDirPath.Equals(extractDirectory))
                    {
                        directoryCreated = true;
                    }
                    if (zipResult.ResultStatus == ZipExtractResultStatus.Success)
                    {
                        try
                        {
                            if (Paused)
                            {
                                if (!(await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false)))
                                {
                                    FinishJob(true); // Cancellation requested while waiting for Unpause
                                    return;
                                }
                            }
                            int parentPathLength = CustomLevelsPath.Length + 1;
                            hashAfterDownload = (await SongHasher.GetSongHashDataAsync(extractDirectory).ConfigureAwait(false)).songHash;
                            if (!SongHash.Equals(hashAfterDownload))
                                Logger.log?.Warn($"Extracted hash doesn't match Beat Saver hash for '{extractDirectory.Substring(parentPathLength)}'");
                            else
                                Logger.log?.Debug($"Extracted hash matches Beat Saver hash for '{extractDirectory.Substring(parentPathLength)}'");
                        }
                        catch (Exception ex)
                        {
                            Logger.log?.Debug($"Error checking hash of {extractDirectory}\n {ex.Message}\n {ex.StackTrace}");
                        }
                    }
                    else
                        Exception = zipResult.Exception;
                }
                else
                    Exception = downloadResult.Exception;
            }
            catch (Exception ex)
            {
                FinishJob(false, ex);
            }
            // Finish
            FinishJob();
            //await Task.Delay(5000).ConfigureAwait(false);
        }

        private void FinishJob(bool canceled = false, Exception exception = null)
        {
            if (canceled)
                Status = JobStatus.Canceled;
            else
                Status = JobStatus.Finished;
            if (exception != null)
                Exception = exception;
            Result = new JobResult()
            {
                SongHash = SongHash,
                SongKey = SongKey,
                DownloadResult = downloadResult,
                ZipResult = zipResult,
                SongDirectory = SongDirectory,
                HashAfterDownload = hashAfterDownload,
                Exception = exception
            };
            OnJobFinished?.Invoke(this,
                new JobFinishedEventArgs(SongHash,
                Result.Successful,
                downloadResult?.Status ?? DownloadResultStatus.Unknown,
                zipResult?.ResultStatus ?? ZipExtractResultStatus.Unknown,
                SongDirectory));
        }

        public Task RunAsync()
        {
            return RunAsync(CancellationToken.None);
        }

        /// <summary>
        /// Attempts to download a song to the specified target path.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public async Task<DownloadResult> DownloadSongAsync(string target)
        {
            DownloadResult result = null;
            try
            {
                var downloadUri = new Uri(BeatSaverDownloadUrlBase + SongHash.ToLower());
                var downloadTarget = Path.Combine(target, SongKey ?? SongHash);
                result = await FileIO.DownloadFileAsync(downloadUri, downloadTarget, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Error downloading song {SongKey ?? SongHash}.\n{ex.Message}");
                Logger.log?.Debug(ex);
                // TODO: Be more specific
                if (result == null)
                    result = new DownloadResult(null, DownloadResultStatus.Unknown, 0, ex.Message, ex);
            }
            return result;
        }

        public override string ToString()
        {
            string retStr = string.Empty;
            if (!string.IsNullOrEmpty(SongKey))
                retStr = $"({SongKey}) ";
            retStr = retStr + $"{SongName} by {LevelAuthorName}";
            return retStr;
        }
    }
}
