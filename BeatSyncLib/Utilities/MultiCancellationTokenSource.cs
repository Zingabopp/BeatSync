using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BeatSyncLib.Utilities
{
    public sealed class MultiCancellationTokenSource : IDisposable
    {
        public bool Disposed { get; private set; }
        private readonly object _lock = new object();
        private readonly List<CancellationToken> cancellationTokens = new List<CancellationToken>();
        private readonly List<CancellationTokenRegistration> tokenRegistrations = new List<CancellationTokenRegistration>();
        private readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        public CancellationToken Token => CancellationTokenSource.Token;

        public MultiCancellationTokenSource()
        {

        }

        public void AddToken(CancellationToken cancellationToken)
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(MultiCancellationTokenSource));
            if (!cancellationToken.CanBeCanceled)
                return;
            lock (_lock)
            {
                if (!CancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokens.Add(cancellationToken);
                    tokenRegistrations.Add(cancellationToken.Register(TryCancel));
                }
            }
        }

        private void TryCancel()
        {
            lock (_lock)
            {
                if (cancellationTokens.All(t => t.IsCancellationRequested))
                    CancellationTokenSource.Cancel();
                foreach (CancellationTokenRegistration reg in tokenRegistrations)
                {
                    reg.Dispose();
                }
                tokenRegistrations.Clear();
            }
        }

        public void Dispose()
        {
            Disposed = true;
            CancellationTokenSource?.Dispose();
            foreach (CancellationTokenRegistration reg in tokenRegistrations)
            {
                reg.Dispose();
            }
            tokenRegistrations.Clear();
        }
    }
}
