using BeatSyncLib.Hashing;
using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
using BeatSyncLib.Downloader.Targets;
using SongFeedReaders.Data;
using SongFeedReaders.Readers.BeatSaver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SongFeedReaders.Utilities;

namespace BeatSyncLib.Downloader
{
    public class Job
    {
        public static readonly string SongTempPath = Path.GetFullPath(Path.Combine("UserData", "BeatSyncTemp"));
        private readonly string CustomLevelsPath;
        private const string BeatSaverDownloadUrlBase = "https://beatsaver.com/api/download/hash/";
        public Exception Exception { get; private set; }
        public event EventHandler<DownloadJobStartedEventArgs> OnJobStarted;
        public event EventHandler<DownloadJobFinishedEventArgs> OnJobFinished;

        private Action<Job> JobFinishedCallback;
        public DownloadManager DownloadManager { get; set; }
        //public PlaylistSong Song { get; private set; }
        public string SongHash { get; private set; }
        public string SongKey { get; private set; }
        public string SongName { get; private set; }
        public string LevelAuthorName { get; private set; }
        public bool Paused => DownloadManager.Paused;
        public string SongDirectory { get; private set; }
        private string _defaultSongDirectoryName;
        public JobResult Result { get; private set; }

        public DownloadJobStatus Status { get; private set; }

        protected ISongTarget[] Targets;

        /// <summary>
        /// Private constructor to use with the others.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <param name="customLevelsPath"></param>
        private Job(ICollection<ISongTarget> targets, Action<Job> jobFinishedCallback = null)
        {
            if (targets == null || targets.Count == 0)
                throw new ArgumentNullException(nameof(targets), $"A {nameof(Job)} must have at least one ISongTarget.");
            Targets = targets.ToArray();
            JobFinishedCallback = jobFinishedCallback;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <param name="song"></param>
        /// <param name="customLevelsPath"></param>
        public Job(IPlaylistSong song, ICollection<ISongTarget> targets, Action<Job> jobFinishedCallback = null)
            : this(targets, jobFinishedCallback)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "song cannot be null.");
            if (string.IsNullOrEmpty(song.Hash))
                throw new ArgumentException("PlaylistSong's hash cannot be null.", nameof(song));
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
        public Job(string songHash, string songName, string songKey, string mapperName, ICollection<ISongTarget> targets, Action<Job> jobFinishedCallback = null)
            : this(targets, jobFinishedCallback)
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
        public Job(ScrapedSong song, ICollection<ISongTarget> targets, Action<Job> jobFinishedCallback = null)
            : this(targets, jobFinishedCallback)
        {
            SongHash = song.Hash;
            SongKey = song.SongKey;
            SongName = song.SongName;
            LevelAuthorName = song.MapperName;
            _defaultSongDirectoryName = Util.GetSongDirectoryName(SongKey, SongName, LevelAuthorName);
        }

        private DownloadResult downloadResult;
        private TargetResult[] targetResults;
        private string hashAfterDownload;

        // TODO: This is horrendous
        [Obsolete("Needs to be redone/removed")]
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            Status = DownloadJobStatus.Downloading;

            bool overwrite = true;
            string extractDirectory = null;
            try
            {
                if (string.IsNullOrEmpty(SongKey))
                {
                    SongFeedReaders.Readers.PageReadResult result = await BeatSaverReader.GetSongByHashAsync(SongHash, cancellationToken).ConfigureAwait(false);
                    SongKey = result?.Songs?.FirstOrDefault()?.SongKey;
                    _defaultSongDirectoryName = Util.GetSongDirectoryName(SongKey, SongName, LevelAuthorName);
                }
                OnJobStarted?.Invoke(this, new DownloadJobStartedEventArgs(SongHash, SongKey, SongName, LevelAuthorName));
                string songDirPath = Path.GetFullPath(Path.Combine(CustomLevelsPath, _defaultSongDirectoryName));
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
                downloadResult = await DownloadSongAsync(SongTempPath, cancellationToken).ConfigureAwait(false);
                List<TargetResult> targetResults;
                if (downloadResult.Status == DownloadResultStatus.Canceled)
                {
                    FinishJob(true);
                    return;
                }
                else if ((downloadResult?.Status ?? DownloadResultStatus.Unknown) == DownloadResultStatus.Success)
                {
                    Stream resultStream = null;
                    targetResults = new List<TargetResult>(Targets.Length);
                    try
                    {
                        foreach (ISongTarget target in Targets)
                        {
                            if (resultStream == null)
                                resultStream = downloadResult.DownloadContainer.GetResultStream();
                            TargetResult result = await target.TransferAsync(resultStream).ConfigureAwait(false);
                            targetResults.Add(result);
                            if (resultStream.CanSeek)
                                resultStream.Seek(0, SeekOrigin.Begin);
                            else
                            {
                                resultStream.Dispose();
                                resultStream = null;
                            }
                        }
                    }
                    finally
                    {
                        resultStream?.Dispose();
                    }
                    #region Zip Extraction (obsolete)
                    //// Extract Zip
                    //if (cancellationToken.IsCancellationRequested)
                    //{
                    //    FinishJob(true);
                    //    return;
                    //}
                    //if (Paused)
                    //{
                    //    if (!(await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false)))
                    //    {
                    //        FinishJob(true); // Cancellation requested while waiting for Unpause
                    //        return;
                    //    }
                    //}
                    ////Status = DownloadJobStatus.Extracting;

                    //this.targetResults = await Task.Run(() => FileIO.ExtractZip(downloadResult.DownloadContainer.GetResultStream(), songDirPath, overwrite)).ConfigureAwait(false);
                    //// Try to delete zip file
                    //if (downloadResult.DownloadContainer is DownloadFileContainer fileContainer)
                    //{
                    //    try
                    //    {
                    //        bool deleteSuccessful = await FileIO.TryDeleteAsync(fileContainer.FilePath).ConfigureAwait(false);
                    //    }
                    //    catch (IOException ex)
                    //    {
                    //        Logger.log?.Warn($"Unable to delete zip file after extraction: {fileContainer.FilePath}.\n{ex.Message}");
                    //    }
                    //}
                    //extractDirectory = Path.GetFullPath(this.targetResults.OutputDirectory);
                    //if (!overwrite && !songDirPath.Equals(extractDirectory))
                    //{
                    //    directoryCreated = true;
                    //}
                    //if (this.targetResults.ResultStatus == ZipExtractResultStatus.Success)
                    //{
                    //    try
                    //    {
                    //        if (Paused)
                    //        {
                    //            if (!(await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false)))
                    //            {
                    //                FinishJob(true); // Cancellation requested while waiting for Unpause
                    //                return;
                    //            }
                    //        }
                    //        int parentPathLength = CustomLevelsPath.Length + 1;
                    //        hashAfterDownload = (await SongHasher.GetSongHashDataAsync(extractDirectory).ConfigureAwait(false)).songHash;
                    //        if (!SongHash.Equals(hashAfterDownload))
                    //            Logger.log?.Warn($"Extracted hash doesn't match Beat Saver hash for '{extractDirectory.Substring(parentPathLength)}'");
                    //        else
                    //            Logger.log?.Debug($"Extracted hash matches Beat Saver hash for '{extractDirectory.Substring(parentPathLength)}'");
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Logger.log?.Debug($"Error checking hash of {extractDirectory}\n {ex.Message}\n {ex.StackTrace}");
                    //    }
                    //}
                    //else
                    //    Exception = this.targetResults.Exception;
                    #endregion
                }
                else
                    Exception = downloadResult.Exception;
            }
            catch (OperationCanceledException ex)
            {
                FinishJob(true, ex);
                return;
            }
            catch (Exception ex)
            {
                Logger.log?.Warn($"Error in DownloadJob.RunAsync: {ex.Message}");
                Logger.log?.Debug(ex.StackTrace);
                FinishJob(false, ex);
                return;
            }
            // Finish
            FinishJob();
            //await Task.Delay(5000).ConfigureAwait(false);
        }

        [Obsolete("OnJobFinished invoke")]
        private void FinishJob(bool canceled = false, Exception exception = null)
        {
            if (canceled || exception is OperationCanceledException)
                Status = DownloadJobStatus.Canceled;
            else
            {
                Status = DownloadJobStatus.Finished;
            }
            if (exception != null)
                Exception = exception;
            Result = new JobResult()
            {
                SongHash = SongHash,
                SongKey = SongKey,
                DownloadResult = downloadResult,
                TargetResults = targetResults,
                SongDirectory = SongDirectory,
                HashAfterDownload = hashAfterDownload,
                Exception = exception
            };
            //OnJobFinished?.Invoke(this,
            //    new JobFinishedEventArgs(SongHash,
            //    Result.Successful,
            //    downloadResult?.Status ?? DownloadResultStatus.Unknown,
            //    zipResult?.ResultStatus ?? ZipExtractResultStatus.Unknown,
            //    SongDirectory));
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
        public async Task<DownloadResult> DownloadSongAsync(string target, CancellationToken cancellationToken)
        {
            DownloadResult result = null;
            try
            {
                Uri downloadUri = new Uri(BeatSaverDownloadUrlBase + SongHash.ToLower());
                string downloadTarget = Path.Combine(target, SongKey ?? SongHash);
                //result = await FileIO.DownloadFileAsync(downloadUri, downloadTarget, cancellationToken, true).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                result = new DownloadResult(null, DownloadResultStatus.Canceled, 0, ex.Message, ex);
            }
            catch (Exception ex)
            {
                Logger.log?.Error($"Uncaught error downloading song {SongKey ?? SongHash} in DownloadJob.DownloadSongAsync: \n{ex.Message}");
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
#if DEBUG
            retStr = retStr + $"({Status.ToString()})";
#endif
            return retStr;
        }
    }
}
