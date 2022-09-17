using BeatSyncLib.Configs;
using SongFeedReaders.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Downloader.Targets
{
    /// <summary>
    /// Destination for beatmaps
    /// </summary>
    public interface IBeatmapsTarget
    {
        string TargetName { get; }

        /// <summary>
        /// Checks the song state of the destination.
        /// </summary>
        /// <param name="songHash"></param>
        /// <returns></returns>
        Task<BeatmapState> GetTargetSongStateAsync(string songHash, CancellationToken cancellationToken);
        /// <summary>
        /// Transfers a beatmap zip file to a target. Any exceptions are caught and returned in <see cref="TargetResult.Exception"/>
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TargetResult> TransferAsync(ISong song, Stream sourceStream, CancellationToken cancellationToken);

        /// <summary>
        /// Perform an action on completion of feed jobs(Adding beatmaps to playlists).
        /// </summary>
        /// <param name="jobResults"></param>
        /// <returns></returns>
        Task OnFeedJobsFinished(IEnumerable<JobResult> jobResults, BeatSyncConfig beatSyncConfig, FeedConfigBase? feedConfig, CancellationToken cancellationToken);
    }
}
