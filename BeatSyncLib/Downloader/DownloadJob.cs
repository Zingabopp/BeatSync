using static SongFeedReaders.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SongFeedReaders;
using SongFeedReaders.Readers.BeatSaver;
using SongFeedReaders.Data;
using BeatSyncLib.Utilities;
using BeatSyncLib.Hashing;
using BeatSyncLib.Playlists;

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

        private List<DownloadFinishedCallback> downloadFinishedCallbacks = new List<DownloadFinishedCallback>();
        private string _targetFile;
        private DownloadResult _downloadResult;

        //public PlaylistSong Song { get; private set; }
        public string FileLocation { get; private set; }
        public string SongHash { get; private set; }
        public string SongKey { get; private set; }
        public string SongName { get; private set; }
        public string LevelAuthorName { get; private set; }
        public bool Paused { get; private set; }
        public DownloadResult DownloadResult { get; private set; }
        private DownloadJobStatus _status;
        public DownloadJobStatus Status 
        { get { return _status; }
            private set
            {
                if (Status == value)
                    return;
                DownloadJobStatus oldStatus = Status;
                Status = value;
                JobStatusChanged?.Invoke(this, new DownloadJobStatusChangedEventArgs(oldStatus, value));
            }
        }
        public bool CanPause { get; private set; } = true;

        public bool SupportsProgressUpdates => true;

        /// <summary>
        /// Private constructor to use with the others.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <param name="targetFile"></param>
        private DownloadJob(string targetFile, DownloadFinishedCallback jobFinishedCallback = null)
        {
            if (string.IsNullOrEmpty(targetFile))
                throw new ArgumentNullException(nameof(targetFile), "customLevelsPath cannot be null when creating a DownloadJob.");
            _targetFile = targetFile;

            AddDownloadFinishedCallback(jobFinishedCallback);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <param name="song"></param>
        /// <param name="customLevelsPath"></param>
        public DownloadJob(PlaylistSong song, string customLevelsPath, DownloadFinishedCallback jobFinishedCallback = null)
            : this(customLevelsPath, jobFinishedCallback)
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
        public DownloadJob(string songHash, string songName, string songKey, string mapperName, string customLevelsPath, DownloadFinishedCallback jobFinishedCallback = null)
            : this(customLevelsPath, jobFinishedCallback)
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
        public DownloadJob(ScrapedSong song, string targetDirectory, DownloadFinishedCallback jobFinishedCallback = null)
            : this(targetDirectory, jobFinishedCallback)
        {
            SongHash = song.Hash;
            SongKey = song.SongKey;
            SongName = song.SongName;
            LevelAuthorName = song.MapperName;
        }


        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (Paused)
            {
                Status = DownloadJobStatus.Paused;
                if (!(await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false)))
                {
                    FinishJob(true); // Cancellation requested while waiting for Unpause
                    return;
                }
            }
            Status = DownloadJobStatus.Downloading;

            bool overwrite = true;
            try
            {
                JobStarted?.Invoke(this, new DownloadJobStartedEventArgs(SongHash, SongKey, SongName, LevelAuthorName));
                var targetFile = new FileInfo(Path.GetFullPath(Path.Combine(_targetFile)));
                if (cancellationToken.IsCancellationRequested)
                {
                    FinishJob(true);
                    return;
                }
                if (Paused)
                {
                    Status = DownloadJobStatus.Paused;
                    if (!(await WaitUntil(() => !Paused, cancellationToken).ConfigureAwait(false)))
                    {
                        FinishJob(true); // Cancellation requested while waiting for Unpause
                        return;
                    }

                    Status = DownloadJobStatus.Downloading;
                }

                // Download Zip
                _downloadResult = await DownloadSongAsync(targetFile.FullName, cancellationToken).ConfigureAwait(false);
                if (_downloadResult.Status == DownloadResultStatus.Canceled)
                {
                    FinishJob(true);
                    return;
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
                FinishJob(false, ex);
                return;
            }
            // Finish
            FinishJob();
        }

        private void FinishJob(bool canceled = false, Exception exception = null)
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
            FileLocation = DownloadResult?.FilePath;
            foreach (var callback in downloadFinishedCallbacks)
            {
                try
                {
                    callback.Invoke(this);
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
                    FileLocation));
        }

        public Task RunAsync()
        {
            return RunAsync(CancellationToken.None);
        }

        /// <summary>
        /// Attempts to download a song to the specified target path.
        /// </summary>
        /// <param name="target">Full path to the downloaded file.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DownloadResult> DownloadSongAsync(string target, CancellationToken cancellationToken, bool overwriteExisting = true)
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
                result = await FileIO.DownloadFileAsync(downloadUri, target, cancellationToken, overwriteExisting).ConfigureAwait(false);
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
            retStr = retStr + $"{SongName} by {LevelAuthorName}";
#if DEBUG
            retStr = retStr + $"({Status.ToString()})";
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
