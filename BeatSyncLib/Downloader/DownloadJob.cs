using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
using SongFeedReaders.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebUtilities;
using static SongFeedReaders.Utilities;

namespace BeatSyncLib.Downloader
{
    public class DownloadJob : IDownloadJob
    {
        private static readonly string BeatSaverHashDownloadUrlBase = "https://beatsaver.com/api/download/hash/";
        private static readonly string BeatSaverKeyDownloadUrlBase = "https://beatsaver.com/api/download/key/";
        public Exception Exception { get; private set; }
        public event EventHandler<DownloadJobStartedEventArgs> JobStarted;
        public event EventHandler<DownloadJobFinishedEventArgs> JobFinished;
        public event EventHandler<DownloadJobProgressChangedEventArgs> JobProgressChanged;
        public event EventHandler<DownloadJobStatusChangedEventArgs> JobStatusChanged;

        private readonly List<DownloadFinishedCallback> downloadFinishedCallbacks = new List<DownloadFinishedCallback>();
        private DownloadResult _downloadResult;

        //public PlaylistSong Song { get; private set; }
        public string FileLocation => (DownloadResult?.DownloadContainer as DownloadFileContainer)?.FilePath;
        public string SongHash { get; private set; }
        public string SongKey { get; private set; }
        public string SongName { get; private set; }
        public string LevelAuthorName { get; private set; }
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
        /// Private constructor to use with the others.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <param name="targetFile"></param>
        private DownloadJob(DownloadContainer container, DownloadFinishedCallback jobFinishedCallback = null)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "DownloadJob must have a download container.");
            _downloadContainer = container;

            AddDownloadFinishedCallback(jobFinishedCallback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <param name="song"></param>
        /// <param name="customLevelsPath"></param>
        public DownloadJob(IPlaylistSong song, DownloadContainer container, DownloadFinishedCallback jobFinishedCallback = null)
            : this(container, jobFinishedCallback)
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

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <param name="song"></param>
        /// <param name="targetDirectory"></param>
        public DownloadJob(ScrapedSong song, DownloadContainer container, DownloadFinishedCallback jobFinishedCallback = null)
            : this(container, jobFinishedCallback)
        {
            SongHash = song.Hash;
            SongKey = song.SongKey;
            SongName = song.SongName;
            LevelAuthorName = song.MapperName;
        }


        public async Task<DownloadResult> RunAsync(CancellationToken cancellationToken)
        {
            _runCancellationToken = cancellationToken;
            if (Paused)
            {
                Status = DownloadJobStatus.Paused;
                if (!(await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false)))
                {
                    return await FinishJob(true).ConfigureAwait(false); // Cancellation requested while waiting for Unpause
                }
            }
            Status = DownloadJobStatus.Downloading;

            try
            {
                JobStarted?.Invoke(this, new DownloadJobStartedEventArgs(SongHash, SongKey, SongName, LevelAuthorName));
                if (cancellationToken.IsCancellationRequested)
                {
                    return await FinishJob(true).ConfigureAwait(false);
                }
                if (Paused)
                {
                    Status = DownloadJobStatus.Paused;
                    if (!(await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false)))
                    {
                        return await FinishJob(true).ConfigureAwait(false); // Cancellation requested while waiting for Unpause
                    }

                    Status = DownloadJobStatus.Downloading;
                }

                // Download Zip
                _downloadResult = await DownloadSongAsync(_downloadContainer, cancellationToken).ConfigureAwait(false);
                if (_downloadResult.Status == DownloadResultStatus.Canceled)
                {
                    return await FinishJob(true).ConfigureAwait(false);
                }
                else
                    Exception = _downloadResult.Exception;
            }
            catch (Exception ex)
            {
                string message = $"Error in DownloadJob.RunAsync: {ex.Message}";
                Logger.log?.Warn(message);
                Logger.log?.Debug(ex.StackTrace);
                _downloadResult = new DownloadResult(null, DownloadResultStatus.Unknown, 0, message, ex);
                return await FinishJob(false, ex).ConfigureAwait(false);
            }
            // Finish
            return await FinishJob().ConfigureAwait(false);
        }

        private async Task<DownloadResult> FinishJob(bool canceled = false, Exception exception = null)
        {
            CanPause = false;
            if (canceled || exception is OperationCanceledException)
                Status = DownloadJobStatus.Canceled;
            else
            {
                Status = DownloadJobStatus.Finished;
            }
            if (exception != null)
            {
                Exception = exception;
                Status = DownloadJobStatus.Faulted;
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
        /// Attempts to download a song to the specified target path.
        /// </summary>
        /// <param name="target">Full path to the downloaded file.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DownloadResult> DownloadSongAsync(DownloadContainer downloadContainer, CancellationToken cancellationToken, bool overwriteExisting = true)
        {
            DownloadResult result = null;
            try
            {
                Uri downloadUri;
                if (!string.IsNullOrEmpty(SongHash))
                    downloadUri = new Uri(BeatSaverHashDownloadUrlBase + SongHash.ToLower());
                else if (!string.IsNullOrEmpty(SongKey))
                    downloadUri = new Uri(BeatSaverDownloadUrlKeyBase + SongKey.ToLower());
                else
                    return new DownloadResult(null, DownloadResultStatus.InvalidRequest, 0, "No SongHash or SongKey provided to the DownloadJob.");
                result = await FileIO.DownloadFileAsync(downloadUri, downloadContainer, cancellationToken, overwriteExisting).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                result = new DownloadResult(null, DownloadResultStatus.Canceled, 0, ex.Message, ex);
            }
            return result;
        }

        public override string ToString()
        {
            string retStr = string.Empty;
            if (!string.IsNullOrEmpty(SongKey))
                retStr = $"({SongKey}) ";
            retStr += $"{SongName} by {LevelAuthorName}";
#if DEBUG
            retStr += $"({Status.ToString()})";
#endif
            return retStr;
        }

        public void AddDownloadFinishedCallback(DownloadFinishedCallback callback)
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
