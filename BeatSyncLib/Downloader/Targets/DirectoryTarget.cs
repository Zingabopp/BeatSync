using BeatSaber.SongHashing;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using BeatSyncLib.Configs;
using BeatSyncLib.Playlists;
using BeatSyncLib.Utilities;
using SongFeedReaders.Feeds;
using SongFeedReaders.Logging;
using SongFeedReaders.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSyncLib.Filtering.Hashing;
using BeatSyncLib.Filtering.History;
using ISong = SongFeedReaders.Models.ISong;

namespace BeatSyncLib.Downloader.Targets
{
    public class DirectoryTarget : BeatmapTarget
    {
        public IBeatmapHasher Hasher { get; private set; }
        public override string TargetName => nameof(DirectoryTarget);
        public FileIO FileIO { get; private set; }
        public ISongHashCollection SongHashCollection { get; private set; }
        public HistoryManager? HistoryManager { get; private set; }
        public PlaylistManager? PlaylistManager { get; private set; }
        public string SongsDirectory { get; private set; } = null!;
        public bool OverwriteTarget { get; private set; }
        public bool UnzipBeatmaps { get; private set; } = true;


        public DirectoryTarget(FileIO fileIO, ISongHashCollection songHashCollection,
            IBeatmapHasher hasher, HistoryManager? historyManager = null, PlaylistManager? playlistManager = null,
            ILogFactory? logFactory = null)
            : base(logFactory)
        {
            FileIO = fileIO ?? throw new ArgumentNullException(nameof(fileIO));
            SongHashCollection = songHashCollection ?? throw new ArgumentNullException(nameof(songHashCollection));
            Hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            HistoryManager = historyManager;
            PlaylistManager = playlistManager;
        }

        public DirectoryTarget(string songsDirectory, bool overwriteTarget, bool unzipBeatmaps, FileIO fileIO, ISongHashCollection songHashCollection,
            IBeatmapHasher hasher, HistoryManager? historyManager = null, PlaylistManager? playlistManager = null,
            ILogFactory? logFactory = null)
            : this(fileIO, songHashCollection, hasher, historyManager, playlistManager, logFactory)
        {
            Configure(songsDirectory, overwriteTarget, unzipBeatmaps);
        }

        public DirectoryTarget Configure(string songsDirectory, bool overwriteTarget, bool unzipBeatmaps)
        {
            SongsDirectory = songsDirectory;
            OverwriteTarget = overwriteTarget;
            UnzipBeatmaps = unzipBeatmaps;
            return this;
        }

        public override Task<BeatmapState> GetTargetBeatmapStateAsync(string beatmapHash, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(SongsDirectory))
                throw new InvalidOperationException($"DirectoryTarget has not been configured with a valid path");
            BeatmapState state = BeatmapState.Wanted;
            if (SongHashCollection != null)
            {
                if (SongHashCollection.HashingState == HashingState.NotStarted)
                    Logger?.Warning($"SongHasher hasn't hashed any songs yet.");
                else if (SongHashCollection.HashingState == HashingState.InProgress)
                    Logger?.Warning($"SongHasher hasn't finished hashing.");
                //await SongHasher.InitializeAsync().ConfigureAwait(false);
                if (SongHashCollection.HashExists(beatmapHash))
                    state = BeatmapState.Exists;
            }
            if (state == BeatmapState.Wanted && HistoryManager != null)
            {
                if (!HistoryManager.IsInitialized)
                    HistoryManager.Initialize();
                if (HistoryManager.TryGetValue(beatmapHash, out HistoryEntry entry))
                {
                    if (!entry.AllowRetry)
                    {
                        state = BeatmapState.NotWanted;
                        entry.Flag = HistoryFlag.Deleted;
                    }
                }
            }
            return Task.FromResult(state);
        }

        public override async Task<TargetResult> TransferAsync(ISong beatmap, Stream sourceStream, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(SongsDirectory))
                throw new InvalidOperationException($"DirectoryTarget has not been configured with a valid path");
            if (beatmap == null)
                throw new ArgumentNullException(nameof(beatmap));
            string? directoryPath = null;
            ZipExtractResult? zipResult = null;
            string directoryName;
            string? hashAfterDownload = null;
            if (beatmap.Name != null && beatmap.LevelAuthorName != null)
                directoryName = Util.GetSongDirectoryName(beatmap.Key, beatmap.Name, beatmap.LevelAuthorName);
            else if (beatmap.Key != null)
                directoryName = beatmap.Key;
            else
                directoryName = beatmap.Hash ?? Path.GetRandomFileName();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                directoryPath = Path.Combine(SongsDirectory, directoryName);
                if (!Directory.Exists(SongsDirectory))
                    throw new BeatmapTargetTransferException($"Parent directory doesn't exist: '{SongsDirectory}'");
                if (UnzipBeatmaps)
                {
                    zipResult = await Task.Run(() => FileIO.ExtractZip(sourceStream, directoryPath, OverwriteTarget)).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(beatmap.Hash) && zipResult.OutputDirectory != null)
                    {
#if ASYNC
                        HashResult hashResult = await Hasher.HashDirectoryAsync(zipResult.OutputDirectory, cancellationToken).ConfigureAwait(false);
#else
                        HashResult hashResult = await Task.Run(() => Hasher.HashDirectory(zipResult.OutputDirectory, cancellationToken)).ConfigureAwait(false);
#endif
                        if (hashResult.ResultType == HashResultType.Error)
                            Logger?.Warning($"Unable to get hash for '{beatmap}': {hashResult.Message}.");
                        else if (hashResult.ResultType == HashResultType.Warn)
                            Logger?.Warning($"Hash warning for '{beatmap}': {hashResult.Message}.");
                        else
                        {
                            hashAfterDownload = hashResult.Hash;
                            if (hashAfterDownload != beatmap.Hash)
                                throw new BeatmapTargetTransferException($"Extracted song hash doesn't match expected hash: {beatmap.Hash} != {hashAfterDownload}");
                        }
                    }
                    TargetResult = new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, 
                        zipResult.ResultStatus == ZipExtractResultStatus.Success, hashAfterDownload, 
                        zipResult, zipResult.Exception);
                }
                else
                {
                    string zipDestination = directoryPath + ".zip";
                    try
                    {
                        using (FileStream fs = File.Open(zipDestination, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                        {
                            await sourceStream.CopyToAsync(fs, 81920, cancellationToken).ConfigureAwait(false);
                        }

                        if (!string.IsNullOrEmpty(beatmap.Hash))
                        {
#if ASYNC
                            HashResult hashResult = await Hasher.HashZippedBeatmapAsync(zipDestination, cancellationToken).ConfigureAwait(false);
#else
                            HashResult hashResult = await Task.Run(() => Hasher.HashZippedBeatmap(zipDestination, cancellationToken)).ConfigureAwait(false);
#endif
                            if (hashResult.ResultType == HashResultType.Error)
                                Logger?.Warning($"Unable to get hash for '{beatmap}': {hashResult.Message}.");
                            else if (hashResult.ResultType == HashResultType.Warn)
                                Logger?.Warning($"Hash warning for '{beatmap}': {hashResult.Message}.");
                            else
                            {
                                hashAfterDownload = hashResult.Hash;
                                if (hashAfterDownload != beatmap.Hash)
                                    throw new BeatmapTargetTransferException($"Extracted song hash doesn't match expected hash: {beatmap.Hash} != {hashAfterDownload}");
                            }
                        }

                        TargetResult = new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, true, hashAfterDownload, null);
                    }
                    catch (OperationCanceledException ex)
                    {
                        TargetResult = new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, false, hashAfterDownload, ex);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
                    {
                        TargetResult = new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, false, hashAfterDownload, ex);
                    }
#pragma warning restore CA1031 // Do not catch general exception types
                    return TargetResult;
                }
            }
            catch (OperationCanceledException ex)
            {
                TargetResult = new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, false, hashAfterDownload, ex);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                TargetResult = new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, false, hashAfterDownload, zipResult, ex);
                return TargetResult;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            return TargetResult;
        }

        public override Task OnFeedJobsFinished(IEnumerable<JobResult> jobResults, BeatSyncConfig beatSyncConfig, FeedConfigBase? feedConfig, CancellationToken cancellationToken)
        {
            bool hasResults = jobResults.Any();
            if (!hasResults)
                return Task.CompletedTask;
            PlaylistManager? playlistManager = PlaylistManager;

            IPlaylist? playlist = null;
            PlaylistStyle playlistStyle = PlaylistStyle.Append;
            bool playlistSortAscending = true;
            if (feedConfig != null && playlistManager != null && feedConfig.CreatePlaylist)
            {
                BuiltInPlaylist playlistType = feedConfig.FeedPlaylist;
                playlistSortAscending = playlistType.IsSortAscending();
                playlistStyle = feedConfig.PlaylistStyle;
                ISong? first = jobResults.FirstOrDefault()?.Song;
                if (playlistType == BuiltInPlaylist.BeatSaverFavoriteMappers
                    && feedConfig is BeatSaverFavoriteMappers mappersConfig
                    && mappersConfig.SeparateMapperPlaylists
                    && first?.LevelAuthorName is string mapperName)
                {
                    // TODO: Better way to identify mapper
                    playlist = playlistManager.GetOrCreateAuthorPlaylist(mapperName);
                }
                else
                    playlist = playlistManager.GetOrAddPlaylist(playlistType);
                if (playlistStyle == PlaylistStyle.Replace)
                {
                    playlist.Clear();
                }
            }


            TimeSpan offset = new TimeSpan(0, 0, 0, 0, -1);
            DateTime addedTime = DateTime.Now;

            foreach (var result in jobResults)
            {
                ISong? beatmap = result.Song;
                if (beatmap == null)
                    continue;
                DirectoryTarget? target = result.TargetResults
                    .Where(tr => ReferenceEquals(this, tr.Target))
                    .Select(tr => tr.Target)
                    .FirstOrDefault() as DirectoryTarget;
                if (target == null)
                    continue;

                HistoryManager? historyManager = HistoryManager;
                if (historyManager != null)
                {
                    string? songHash = beatmap.Hash;
                    if (songHash != null && songHash.Length > 0)
                    {
                        historyManager.AddOrUpdate(songHash, result.CreateHistoryEntry());
                    }
                }
                if (playlist != null)
                {
                    IPlaylistSong? addedSong = playlist.Add(beatmap);
                    if (addedSong != null)
                    {
                        addedSong.DateAdded = addedTime;
                        addedTime += offset;
                    }
                }
            }
            if (playlist != null && playlistManager != null)
            {
                playlist.Sort();
                playlist.RaisePlaylistChanged();
                playlistManager.StorePlaylist(playlist);
            }
            return Task.CompletedTask;
        }
    }

    public class DirectoryTargetResult : TargetResult
    {
        public ZipExtractResult? ZipExtractResult { get; protected set; }
        public DirectoryTargetResult(ISong beatmap, BeatmapTarget target, BeatmapState songState,
            bool success, string? beatmapHash, Exception? exception)
            : base(beatmap, target, songState, success, beatmapHash, exception)
        { }

        public DirectoryTargetResult(ISong beatmap, BeatmapTarget target, BeatmapState songState,
            bool success, string? beatmapHash, ZipExtractResult? zipExtractResult, Exception? exception)
            : base(beatmap, target, songState, success, beatmapHash, exception)
        {
            ZipExtractResult = zipExtractResult;
        }
    }
}
