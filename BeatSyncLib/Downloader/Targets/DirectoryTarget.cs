using BeatSyncLib.Hashing;
using BeatSyncLib.History;
using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
using SongFeedReaders.Data;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader.Targets
{
    public class DirectoryTarget : SongTarget, ITargetWithHistory, ITargetWithPlaylists
    {
        public override string TargetName => nameof(DirectoryTarget);
        public SongHasher SongHasher { get; protected set; }
        public HistoryManager? HistoryManager { get; protected set; }
        public PlaylistManager? PlaylistManager { get; protected set; }
        public string SongsDirectory { get; protected set; }
        public bool OverwriteTarget { get; protected set; }

        public DirectoryTarget(string songsDirectory, bool overwriteTarget, SongHasher songHasher, HistoryManager? historyManager, PlaylistManager? playlistManager)
            : base()
        {
            SongsDirectory = Path.GetFullPath(songsDirectory);
            SongHasher = songHasher;
            HistoryManager = historyManager;
            PlaylistManager = playlistManager;
            OverwriteTarget = overwriteTarget;
        }

        public override async Task<SongState> CheckSongExistsAsync(string songHash)
        {
            SongState state = SongState.Wanted;
            if (HistoryManager != null)
            {
                if (!HistoryManager.IsInitialized)
                    HistoryManager.Initialize();
                if (HistoryManager.TryGetValue(songHash, out HistoryEntry entry))
                {
                    if (!entry.AllowRetry)
                    {
                        if (entry.Flag == HistoryFlag.Downloaded)
                            state = SongState.Exists;
                        else
                            state = SongState.NotWanted;
                    }
                }
            }
            if (SongHasher != null)
            {
                if (!SongHasher.Initialized)
                    await SongHasher.InitializeAsync().ConfigureAwait(false);
                if (SongHasher.ExistingSongs.ContainsKey(songHash))
                    state = SongState.Exists;
            }
            return state;
        }

        public override async Task<TargetResult> TransferAsync(ISong song, Stream sourceStream, CancellationToken cancellationToken)
        {
            if (song == null)
                throw new ArgumentNullException(nameof(song), "Song cannot be null for TransferAsync.");
            string directoryPath = null;
            ZipExtractResult zipResult = null;
            string directoryName = Util.GetSongDirectoryName(song.Key, song.Name, song.LevelAuthorName);
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return new TargetResult(this, SongState.Wanted, false, new OperationCanceledException());
                directoryPath = Path.Combine(SongsDirectory, directoryName);
                if (!Directory.Exists(SongsDirectory))
                    throw new SongTargetTransferException($"Parent directory doesn't exist: '{SongsDirectory}'");
                zipResult = await Task.Run(() => FileIO.ExtractZip(sourceStream, directoryPath, OverwriteTarget)).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(song.Hash))
                {
                    string hashAfterDownload = (await SongHasher.GetSongHashDataAsync(zipResult.OutputDirectory).ConfigureAwait(false)).songHash;
                    if (hashAfterDownload != song.Hash)
                        throw new SongTargetTransferException($"Extracted song hash doesn't match expected hash: {song.Hash} != {hashAfterDownload}");
                }
                TargetResult = new DirectoryTargetResult(this, SongState.Wanted, zipResult.ResultStatus == ZipExtractResultStatus.Success, zipResult, zipResult.Exception);
                return TargetResult;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                TargetResult = new DirectoryTargetResult(this, SongState.Wanted, false, zipResult, ex);
                return TargetResult;
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }

    public class DirectoryTargetResult : TargetResult
    {
        public ZipExtractResult ZipExtractResult { get; private set; }
        public DirectoryTargetResult(SongTarget target, SongState songState, bool success, ZipExtractResult zipExtractResult, Exception exception)
            : base(target, songState, success, exception)
        {
            ZipExtractResult = zipExtractResult;
        }
    }
}
