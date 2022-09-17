using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSyncLib.Utilities
{
    public sealed class PauseManager : IPauseManager
    {
        private readonly object _lock = new object();
        private TaskCompletionSource<bool>? _paused;
        private Task PauseTask => _paused?.Task ?? Task.CompletedTask;
        public PauseManager()
        {

        }
        public bool IsPaused { get; private set; }
        public void Pause()
        {
            lock (_lock)
            {
                if (PauseTask.IsCompleted)
                {
                    _paused = new TaskCompletionSource<bool>();
                }
                IsPaused = true;
            }
        }

        public void Unpause()
        {
            lock (_lock)
            {
                if (_paused != null)
                    _paused.TrySetResult(true);
                IsPaused = false;
            }
        }

        public async Task WaitForPause(CancellationToken cancellationToken)
        {
            Task pauseTask;
            lock (_lock)
            {
                pauseTask = PauseTask;
            }
            if (!pauseTask.IsCompleted)
            {
#if NET6_0_OR_GREATER
                await PauseTask.WaitAsync(cancellationToken).ConfigureAwait(false);
#else
                var cancelledSource = new TaskCompletionSource<object?>();
                using var reg = cancellationToken.Register(() => cancelledSource.TrySetCanceled(cancellationToken));
                await Task.WhenAny(pauseTask, cancelledSource.Task).ConfigureAwait(false);
                cancelledSource.TrySetResult(null);
#endif
            }
        }
    }
}
