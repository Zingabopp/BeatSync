using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Utilities
{
    public interface IPauseManager
    {
        bool IsPaused { get; }
        void Pause();
        void Unpause();
        Task WaitForPause(CancellationToken cancellationToken);
    }
}
