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
using BeatSyncLib.Filtering;
using BeatSyncLib.Filtering.Hashing;
using BeatSyncLib.Filtering.History;
using SongFeedReaders.Utilities;
using ISong = SongFeedReaders.Models.ISong;
using Util = BeatSyncLib.Utilities.Util;

namespace BeatSyncLib.Downloader.Targets
{
    public class DirectoryTarget : IBeatmapTarget
    {
        public IBeatmapHasher Hasher { get; private set; }
        private IBeatmapFilter[] _filters = Array.Empty<IBeatmapFilter>();
        private readonly PlaylistManager? _playlistManager = null;
        public string TargetName => nameof(DirectoryTarget);
        public FileIO FileIO { get; private set; }
        public string SongsDirectory { get; private set; } = null!;
        public bool OverwriteTarget { get; private set; }
        public bool UnzipBeatmaps { get; private set; } = true;
        private readonly ILogger? _logger;

        public DirectoryTarget(FileIO fileIO, IEnumerable<IBeatmapFilter>? filters,
            IBeatmapHasher hasher, PlaylistManager? playlistManager = null,
            ILogFactory? logFactory = null)
        {
            _logger = logFactory?.GetLogger();
            FileIO = fileIO ?? throw new ArgumentNullException(nameof(fileIO));
            if (filters != null)
            {
                _filters = filters.ToArray();
            }
            Hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _playlistManager = playlistManager;
        }

        public DirectoryTarget(string songsDirectory, bool overwriteTarget, bool unzipBeatmaps, FileIO fileIO, 
            IEnumerable<IBeatmapFilter> filters, IBeatmapHasher hasher, PlaylistManager? playlistManager = null,
            ILogFactory? logFactory = null)
            : this(fileIO, filters, hasher, playlistManager, logFactory)
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
        public async Task<BeatmapState> GetTargetBeatmapStateAsync(string beatmapHash, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(SongsDirectory))
                throw new InvalidOperationException($"DirectoryTarget has not been configured with a valid path");
            Task<BeatmapState>[] filterResults =
                _filters.Select(f => f.GetBeatmapStateAsync(beatmapHash, cancellationToken)).ToArray();
            await Task.WhenAll(filterResults);
            BeatmapState state = BeatmapState.Wanted;
            foreach (BeatmapState result in filterResults.Select(r => r.Result))
            {
                // This depends on BeatmapState being ordered correctly
                if (state < result)
                {
                    state = result;
                }
            }

            return state;
        }
        public async Task<BeatmapState> GetTargetBeatmapStateAsync(ISong beatmap, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(SongsDirectory))
                throw new InvalidOperationException($"DirectoryTarget has not been configured with a valid path");
            Task<BeatmapState>[] filterResults =
                _filters.Select(f => f.GetBeatmapStateAsync(beatmap, cancellationToken)).ToArray();
            await Task.WhenAll(filterResults);
            BeatmapState state = BeatmapState.Wanted;
            foreach (BeatmapState result in filterResults.Select(r => r.Result))
            {
                // This depends on BeatmapState being ordered correctly
                if (state < result)
                {
                    state = result;
                }
            }

            return state;
        }

        public async Task<TargetResult> TransferAsync(ISong beatmap, Stream sourceStream, CancellationToken cancellationToken)
        {
            // TODO: Cache this task and run post-processing for playlist updating for if more than one feed adds the same beatmapHash
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
                            _logger?.Warning($"Unable to get hash for '{beatmap}': {hashResult.Message}.");
                        else if (hashResult.ResultType == HashResultType.Warn)
                            _logger?.Warning($"Hash warning for '{beatmap}': {hashResult.Message}.");
                        else
                        {
                            hashAfterDownload = hashResult.Hash;
                            if (hashAfterDownload != beatmap.Hash)
                                throw new BeatmapTargetTransferException($"Extracted song hash doesn't match expected hash: {beatmap.Hash} != {hashAfterDownload}");
                        }
                    }
                    return new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, 
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
                                _logger?.Warning($"Unable to get hash for '{beatmap}': {hashResult.Message}.");
                            else if (hashResult.ResultType == HashResultType.Warn)
                                _logger?.Warning($"Hash warning for '{beatmap}': {hashResult.Message}.");
                            else
                            {
                                hashAfterDownload = hashResult.Hash;
                                if (hashAfterDownload != beatmap.Hash)
                                    throw new BeatmapTargetTransferException($"Extracted song hash doesn't match expected hash: {beatmap.Hash} != {hashAfterDownload}");
                            }
                        }

                        return new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, true, hashAfterDownload, null);
                    }
                    catch (OperationCanceledException ex)
                    {
                        return new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, false, hashAfterDownload, ex);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
                    {
                        return new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, false, hashAfterDownload, ex);
                    }
#pragma warning restore CA1031 // Do not catch general exception types
                    //return TargetResult;
                }
            }
            catch (OperationCanceledException ex)
            {
                return new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, false, hashAfterDownload, ex);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                return new DirectoryTargetResult(beatmap, this, BeatmapState.Wanted, false, hashAfterDownload, zipResult, ex);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        public async Task<TargetResult> ProcessFeedResult(FeedResult feedResult, PauseToken pauseToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task OnFeedJobsFinished(IEnumerable<JobResult> jobResults, BeatSyncConfig beatSyncConfig, FeedConfigBase? feedConfig, CancellationToken cancellationToken)
        {
            JobResult[] results = jobResults.ToArray();
            if (results.Length == 0)
            {
                return;
            }

            PlaylistManager? playlistManager = _playlistManager;

            IPlaylist? playlist = null;
            bool playlistSortAscending = true;
            if (feedConfig != null && playlistManager != null && feedConfig.CreatePlaylist)
            {
                BuiltInPlaylist playlistType = feedConfig.FeedPlaylist;
                playlistSortAscending = playlistType.IsSortAscending();
                PlaylistStyle playlistStyle = feedConfig.PlaylistStyle;
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

            foreach (JobResult result in results)
            {
                ISong? beatmap = result.Song;
                if (beatmap == null)
                    continue;
                await Task.WhenAll(_filters.Select(f =>
                    f.UpdateBeatmapStateAsync(beatmap, BeatmapState.Downloaded, cancellationToken)).ToList());
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
            return;
        }
    }
}
