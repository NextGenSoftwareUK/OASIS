using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// </summary>
    public class RateLimiter
    {
        private readonly Dictionary<string, Queue<DateTime>> _requestTimestamps = new Dictionary<string, Queue<DateTime>>();
        private readonly object _lock = new object();

        public bool TryAcquire(string key, int maxRequests, TimeSpan window)
        {
            lock (_lock)
            {
                if (!_requestTimestamps.TryGetValue(key, out var timestamps))
                {
                    timestamps = new Queue<DateTime>();
                    _requestTimestamps[key] = timestamps;
                }

                var now = DateTime.UtcNow;
                var cutoff = now - window;
                while (timestamps.Count > 0 && timestamps.Peek() < cutoff)
                    timestamps.Dequeue();

                if (timestamps.Count >= maxRequests)
                    return false;

                timestamps.Enqueue(now);
                return true;
            }
        }
    }
}
