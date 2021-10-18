using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
using SongFeedReaders.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using WebUtilities.DownloadContainers;

namespace BeatSyncLib.Downloader.Downloading
{
    public class DownloadJob : IDownloadJob
    {
        public Exception? Exception { get; private set; }
        public event EventHandler<DownloadJobStartedEventArgs>? JobStarted;
        public event EventHandler<DownloadJobFinishedEventArgs>? JobFinished;
        public event EventHandler<DownloadJobProgressChangedEventArgs>? JobProgressChanged;
        public event EventHandler<DownloadJobStatusChangedEventArgs>? JobStatusChanged;

        private readonly List<DownloadFinishedCallback> downloadFinishedCallbacks = new List<DownloadFinishedCallback>();
        //private DownloadResult? _inProgressResult;

        //public PlaylistSong Song { get; private set; }
        public string? FileLocation => (DownloadResult?.DownloadContainer as FileDownloadContainer)?.FilePath;
        public string SongHash { get; private set; }
        public string? SongKey { get; private set; }
        public string? SongName { get; private set; }
        public string? LevelAuthorName { get; private set; }
        public DownloadResult? DownloadResult { get; private set; }
        private DownloadJobStatus _status;
        public DownloadJobStatus Status
        {
            get { return _status; }
            private set
            {
                if (Status == value)
                    return;
                DownloadJobStatus oldStatus = Status;
                _status = value;
                EventHandler<DownloadJobStatusChangedEventArgs>? eventHandler = JobStatusChanged;
                eventHandler?.Invoke(this, new DownloadJobStatusChangedEventArgs(oldStatus, value));
            }
        }

        private readonly DownloadContainer _downloadContainer;
        private readonly FileIO _fileIO;
        public bool SupportsProgressUpdates => true;
        private CancellationToken _runCancellationToken;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        /// <param name="container"></param>
        /// <param name="jobFinishedCallback"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public DownloadJob(ISong song, DownloadContainer container, FileIO fileIO, DownloadFinishedCallback? jobFinishedCallback = null)
        {
            _fileIO = fileIO ?? throw new ArgumentNullException(nameof(fileIO));
            _downloadContainer = container 
                    ?? throw new ArgumentNullException(nameof(container), "DownloadJob must have a download container.");
            _downloadContainer.ProgressChanged += _downloadContainer_ProgressChanged;
            AddDownloadFinishedCallback(jobFinishedCallback);
            if (song == null)
                throw new ArgumentNullException(nameof(song), "song cannot be null.");
            if (song.Hash == null || song.Hash.Length == 0)
                throw new ArgumentException("ISong must have a Hash or Key.", nameof(song));
            SongHash = song.Hash;
            SongKey = song.Key;
            SongName = song.Name;
            LevelAuthorName = song.LevelAuthorName;
        }

        private void _downloadContainer_ProgressChanged(object sender, DownloadProgress e)
        {
            EventHandler<DownloadJobProgressChangedEventArgs>? handler = JobProgressChanged;
            if (handler != null)
            {
                DownloadJobStatus status = Status;
                ProgressValue progress = new ProgressValue(e.TotalBytesDownloaded, e.TotalDownloadSize);
                handler(this, new DownloadJobProgressChangedEventArgs(status, progress));
            }
        }

        public async Task<DownloadResult> RunAsync(CancellationToken cancellationToken)
        {
            _runCancellationToken = cancellationToken;
            Status = DownloadJobStatus.Downloading;
            DownloadResult? result = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                // TODO: Re-add pause capability
                //if (Paused)
                //{
                //    Status = DownloadJobStatus.Paused;
                //    //await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false);
                //    cancellationToken.ThrowIfCancellationRequested();
                //    Status = DownloadJobStatus.Downloading;
                //}
                EventHandler<DownloadJobStartedEventArgs>? jobStartedHandler = JobStarted;
                jobStartedHandler?.Invoke(this, new DownloadJobStartedEventArgs(SongHash, SongKey, SongName, LevelAuthorName));

                result = await DownloadSongAsync(_downloadContainer, cancellationToken).ConfigureAwait(false);
                if (result.Exception != null)
                    throw result.Exception;
            }
            catch (OperationCanceledException ex)
            {
                result = new DownloadResult(null, DownloadResultStatus.Canceled, 0, null, ex);
                return await FinishJob(result, ex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string message = $"Error in DownloadJob.RunAsync: {ex.Message}";
                Logger.log?.Warn(message);
                Logger.log?.Debug(ex.StackTrace);
                if (result == null)
                    result = new DownloadResult(null, DownloadResultStatus.Unknown, 0, message, ex);
                return await FinishJob(result, ex).ConfigureAwait(false);
            }
            // Finish
            return await FinishJob(result).ConfigureAwait(false);
        }

        private async Task<DownloadResult> FinishJob(DownloadResult result, Exception? exception = null)
        {
            if (exception is OperationCanceledException)
                Status = DownloadJobStatus.Canceled;
            else if (exception != null)
            {
                Exception = exception;
                Status = DownloadJobStatus.Faulted;
            }
            else
            {
                Status = DownloadJobStatus.Finished;
            }
            DownloadResult = result ?? throw new ArgumentNullException(nameof(result));
            List<Task> invokedCallbacks = new List<Task>();
            foreach (DownloadFinishedCallback callback in downloadFinishedCallbacks)
            {
                invokedCallbacks.Add(callback.Invoke(this));
            }
            if (invokedCallbacks.Count > 0)
            {
                try
                {
                    await Task.Run(() => Task.WhenAll(invokedCallbacks), _runCancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.log?.Error($"Error in {this.GetType().Name} download finished callback: {ex.Message}");
                    Logger.log?.Debug(ex);
                }
            }
            JobFinished?.Invoke(this,
                    new DownloadJobFinishedEventArgs(SongHash,
                    result.Status, result.DownloadContainer));
            return result;
        }

        public Task<DownloadResult> RunAsync()
        {
            return RunAsync(CancellationToken.None);
        }

        /// <summary>
        /// Attempts to download a song to the specified <see cref="DownloadContainer"/>.
        /// </summary>
        /// <param name="downloadContainer">Target container for the download.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DownloadResult> DownloadSongAsync(DownloadContainer downloadContainer, CancellationToken cancellationToken)
        {
            Uri downloadUri;
            try
            {
                if (SongHash != null && SongHash.Length > 0)
                    downloadUri = SongFeedReaders.Utilities.BeatSaverHelper.GetDownloadUriByHash(SongHash);
                else
                    return new DownloadResult(null, DownloadResultStatus.InvalidRequest, 0, 
                        "No SongHash provided to the DownloadJob.", 
                        new InvalidOperationException($"No SongHash provided to the DownloadJob"));
            }
            catch (FormatException ex)
            {
                return new DownloadResult(null, DownloadResultStatus.InvalidRequest, 0, $"Could not create a valid Uri.", ex);
            }
            DownloadResult result = await _fileIO.DownloadFileAsync(downloadUri, downloadContainer, cancellationToken).ConfigureAwait(false);
            return result;
        }

        public override string ToString()
        {
            string retStr = string.Empty;
            if (!string.IsNullOrEmpty(SongKey))
                retStr = $"({SongKey}) ";
            retStr += $"{SongName} by {LevelAuthorName}";
#if DEBUG
            retStr += $"({Status})";
#endif
            return retStr;
        }

        public void AddDownloadFinishedCallback(DownloadFinishedCallback? callback)
        {
            if (callback != null)
                downloadFinishedCallbacks.Add(callback);
        }
    }
}
