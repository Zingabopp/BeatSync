using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Filtering.Hashing
{
    public interface ISongHashCollection
    {
        public event EventHandler<int>? CollectionRefreshed;

        public HashingState HashingState { get; }
        public int Count { get; }
        public int CountMissing { get; }

        public Task<int> RefreshHashesAsync(CancellationToken cancellationToken);
        public Task<int> RefreshHashesAsync(bool missingOnly, CancellationToken cancellationToken);
        public Task<int> RefreshHashesAsync(bool missingOnly, IProgress<double>? progress, CancellationToken cancellationToken);

        public bool HashExists(string hash);
    }

    public enum HashingState
    {
        NotStarted,
        InProgress,
        Finished
    }
}
