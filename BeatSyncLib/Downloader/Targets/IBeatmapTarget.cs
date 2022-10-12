using BeatSyncLib.Filtering;
using SongFeedReaders.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SongFeedReaders.Feeds;
using SongFeedReaders.Utilities;

namespace BeatSyncLib.Downloader.Targets
{
    /// <summary>
    /// Destination for beatmaps
    /// </summary>
    public interface IBeatmapTarget
    {
        string TargetName { get; }

        /// <summary>
        /// Checks the beatmap state of the destination.
        /// </summary>
        /// <param name="beatmapHash"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<BeatmapState> GetTargetBeatmapStateAsync(string beatmapHash, CancellationToken cancellationToken);
        /// <summary>
        /// Checks the beatmap state of the destination.
        /// </summary>
        /// <param name="beatmap"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<BeatmapState> GetTargetBeatmapStateAsync(ISong beatmap, CancellationToken cancellationToken);

        /// <summary>
        /// Transfers a beatmap zip file to a target. Any exceptions are caught and returned in <see cref="TargetResult.Exception"/>
        /// </summary>
        /// <param name="beatmap"></param>
        /// <param name="sourceStream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TargetResult> TransferAsync(ISong beatmap, Stream sourceStream, CancellationToken cancellationToken);

        Task<TargetResult> ProcessFeedResult(FeedResult feedResult, PauseToken pauseToken, CancellationToken cancellationToken);
    }
}
