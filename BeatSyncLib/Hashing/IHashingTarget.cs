using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Hashing
{
    /// <summary>
    /// Represents a target location that contains beatmaps to be hashed.
    /// </summary>
    public interface IHashingTarget
    {
        /// <summary>
        /// Returns true if <see cref="InitializeAsync(CancellationToken)"/> has finished.
        /// </summary>
        bool Ready { get; }
        /// <summary>
        /// Initializes the <see cref="IHashingTarget"/>. This should be able to be called multiple times.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Number of beatmaps hashed</returns>
        Task<int> InitializeAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Returns true if a beatmap with the given <paramref name="beatmapHash"/> was found.
        /// </summary>
        /// <param name="beatmapHash"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="InitializeAsync(CancellationToken)"/> wasn't called or hasn't finished.</exception>
        bool BeatmapExists(string beatmapHash);
    }
}
