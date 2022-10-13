using System.Threading;
using System.Threading.Tasks;
using SongFeedReaders.Models;

namespace BeatSyncLib.Filtering
{
    public interface IBeatmapFilter
    {
        /// <summary>
        /// Gets the current state of the beatmapHash from the filter.
        /// </summary>
        /// <param name="beatmapHash"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<BeatmapState> GetBeatmapStateAsync(string beatmapHash, CancellationToken cancellationToken);
        /// <summary>
        /// Gets the current state of the beatmapHash from the filter.
        /// </summary>
        /// <param name="beatmap"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<BeatmapState> GetBeatmapStateAsync(ISong beatmap, CancellationToken cancellationToken);
        /// <summary>
        /// Updates the state of the given beatmapHash in the filter.
        /// </summary>
        /// <param name="beatmap"></param>
        /// <param name="newState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateBeatmapStateAsync(ISong beatmap, BeatmapState newState, CancellationToken cancellationToken);
    }
}