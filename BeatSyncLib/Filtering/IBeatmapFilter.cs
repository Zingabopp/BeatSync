using System.Threading;
using System.Threading.Tasks;
using SongFeedReaders.Models;

namespace BeatSyncLib.Filtering
{
    public interface IBeatmapFilter
    {
        Task<BeatmapState> GetBeatmapStateAsync(ISong song, CancellationToken cancellationToken);
    }
}