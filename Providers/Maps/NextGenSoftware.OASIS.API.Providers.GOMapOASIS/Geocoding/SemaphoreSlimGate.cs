using System;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geocoding
{
    /// <summary>
    /// Spaces requests out by a minimum interval.
    ///
    /// Nominatim's usage policy caps public use at one request per second and blocks
    /// clients that exceed it, so honouring the limit is part of the integration
    /// working rather than an optional nicety.
    /// </summary>
    public class SemaphoreSlimGate
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly TimeSpan _interval;

        private DateTime _lastRequestUtc = DateTime.MinValue;

        public SemaphoreSlimGate(TimeSpan interval)
        {
            _interval = interval;
        }

        /// <summary>Returns once enough time has passed since the previous call.</summary>
        public async Task WaitAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                TimeSpan sinceLast = DateTime.UtcNow - _lastRequestUtc;

                if (sinceLast < _interval)
                    await Task.Delay(_interval - sinceLast, cancellationToken).ConfigureAwait(false);

                _lastRequestUtc = DateTime.UtcNow;
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
