using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using static SongFeedReaders.Utilities;
using WebUtilities.DownloadContainers;

namespace BeatSyncLib.Downloader.Downloading
{
    public class DownloadJob : IDownloadJob
    {
        private static readonly string BeatSaverHashDownloadUrlBase = "https://beatsaver.com/api/download/hash/";
        private static readonly string BeatSaverKeyDownloadUrlBase = "https://beatsaver.com/api/download/key/";
        public Exception? Exception { get; private set; }
        public event EventHandler<DownloadJobStartedEventArgs> JobStarted;
        public event EventHandler<DownloadJobFinishedEventArgs> JobFinished;
        public event EventHandler<DownloadJobProgressChangedEventArgs> JobProgressChanged;
        public event EventHandler<DownloadJobStatusChangedEventArgs> JobStatusChanged;

        private readonly List<DownloadFinishedCallback> downloadFinishedCallbacks = new List<DownloadFinishedCallback>();
        private DownloadResult _downloadResult;

        //public PlaylistSong Song { get; private set; }
        public string? FileLocation => (DownloadResult?.DownloadContainer as FileDownloadContainer)?.FilePath;
        public string? SongHash { get; private set; }
        public string? SongKey { get; private set; }
        public string? SongName { get; private set; }
        public string? LevelAuthorName { get; private set; }
        public bool Paused { get; private set; }
        public DownloadResult DownloadResult { get; private set; }
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
                EventHandler<DownloadJobStatusChangedEventArgs> eventHandler = JobStatusChanged;
                eventHandler?.Invoke(this, new DownloadJobStatusChangedEventArgs(oldStatus, value));
            }
        }
        public bool CanPause { get; private set; } = true;

        private readonly DownloadContainer _downloadContainer;

        public bool SupportsProgressUpdates => true;
        private CancellationToken _runCancellationToken;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="jobFinishedCallback"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private DownloadJob(DownloadContainer container, DownloadFinishedCallback? jobFinishedCallback = null)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "DownloadJob must have a download container.");
            _downloadContainer = container;
            _downloadContainer.ProgressChanged += _downloadContainer_ProgressChanged;
            AddDownloadFinishedCallback(jobFinishedCallback);
        }

        private void _downloadContainer_ProgressChanged(object sender, DownloadProgress e)
        {
            EventHandler<DownloadJobProgressChangedEventArgs> handler = JobProgressChanged;
            if (handler != null)
            {
                DownloadJobStatus status = Status;
                ProgressValue progress = new ProgressValue(e.TotalBytesDownloaded, e.TotalDownloadSize);
                handler(this, new DownloadJobProgressChangedEventArgs(status, progress));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="song"></param>
        /// <param name="container"></param>
        /// <param name="jobFinishedCallback"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public DownloadJob(ISong song, DownloadContainer container, DownloadFinishedCallback? jobFinishedCallback = null)
            : this(container, jobFinishedCallback)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "song cannot be null.");
            if ((song.Hash == null || song.Hash.Length == 0) && (song.Key == null || song.Key.Length == 0))
                throw new ArgumentException("ISong must have a Hash or Key.", nameof(song));
            //Song = song;
            SongHash = song.Hash;
            SongKey = song.Key;
            SongName = song.Name;
            LevelAuthorName = song.LevelAuthorName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="songHash"></param>
        /// <param name="songName"></param>
        /// <param name="songKey"></param>
        /// <param name="mapperName"></param>
        /// <param name="container"></param>
        /// <param name="jobFinishedCallback"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public DownloadJob(string songHash, string songName, string songKey, string mapperName, DownloadContainer container, DownloadFinishedCallback jobFinishedCallback = null)
            : this(container, jobFinishedCallback)
        {
            if (string.IsNullOrEmpty(songHash))
                throw new ArgumentNullException();
            SongHash = songHash;
            SongKey = songKey;
            SongName = songName;
            LevelAuthorName = mapperName;
        }

        public async Task<DownloadResult> RunAsync(CancellationToken cancellationToken)
        {
            _runCancellationToken = cancellationToken;
            Status = DownloadJobStatus.Downloading;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Paused)
                {
                    Status = DownloadJobStatus.Paused;
                    await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    Status = DownloadJobStatus.Downloading;
                }
                EventHandler<DownloadJobStartedEventArgs> jobStartedHandler = JobStarted;
                jobStartedHandler?.Invoke(this, new DownloadJobStartedEventArgs(SongHash, SongKey, SongName, LevelAuthorName));

                _downloadResult = await DownloadSongAsync(_downloadContainer, cancellationToken).ConfigureAwait(false);
                if (_downloadResult.Exception != null)
                    throw _downloadResult.Exception;
            }
            catch (OperationCanceledException ex)
            {
                return await FinishJob(ex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                string message = $"Error in DownloadJob.RunAsync: {ex.Message}";
                Logger.log?.Warn(message);
                Logger.log?.Debug(ex.StackTrace);
                if (_downloadResult == null)
                    _downloadResult = new DownloadResult(null, DownloadResultStatus.Unknown, 0, message, ex);
                return await FinishJob(ex).ConfigureAwait(false);
            }
            // Finish
            return await FinishJob().ConfigureAwait(false);
        }

        private async Task<DownloadResult> FinishJob(Exception exception = null)
        {
            CanPause = false;
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

            DownloadResult = _downloadResult;
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
                    _downloadResult?.Status ?? DownloadResultStatus.Unknown,
                    DownloadResult.DownloadContainer));
            return DownloadResult;
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
            string? stringForUri = null;
            Uri downloadUri;
            try
            {
                if (SongHash != null && SongHash.Length > 0)
                    stringForUri = BeatSaverHashDownloadUrlBase + SongHash.ToLower();
                else if (SongKey != null && SongKey.Length > 0)
                    stringForUri = BeatSaverKeyDownloadUrlBase + SongKey.ToLower();
                else
                    return new DownloadResult(null, DownloadResultStatus.InvalidRequest, 0, "No SongHash or SongKey provided to the DownloadJob.");
                downloadUri = new Uri(stringForUri);
            }
            catch (FormatException ex)
            {
                return new DownloadResult(null, DownloadResultStatus.InvalidRequest, 0, $"Could not create a valid Uri from '{stringForUri}'.", ex);
            }
            DownloadResult result = await FileIO.DownloadFileAsync(downloadUri, downloadContainer, cancellationToken).ConfigureAwait(false);
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

        public void Pause()
        {
            if (CanPause)
                Paused = true;
        }

        public void Unpause()
        {
            Paused = false;
        }
    }
}
