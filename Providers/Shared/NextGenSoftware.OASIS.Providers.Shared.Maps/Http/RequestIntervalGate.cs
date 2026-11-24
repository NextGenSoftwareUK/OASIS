using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Http
{
    /// <summary>
    /// Enforces a minimum interval between requests to one service, as the usage
    /// policies of the public OSRM and Nominatim instances require (one per second).
    /// </summary>
    public sealed class RequestIntervalGate
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _interval;
        private long _lastTimestamp;

        public RequestIntervalGate(TimeSpan interval)
        {
            if (interval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
            _interval = interval;
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_lastTimestamp != 0)
                {
                    TimeSpan remaining = _interval - Stopwatch.GetElapsedTime(_lastTimestamp);
                    if (remaining > TimeSpan.Zero) await Task.Delay(remaining, cancellationToken).ConfigureAwait(false);
                }

                _lastTimestamp = Stopwatch.GetTimestamp();
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
