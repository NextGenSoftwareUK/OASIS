using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Edge.NearbySync
{
    /// <summary>Bounded recently-seen set. A message arriving over several mesh routes is processed once.</summary>
    public sealed class NearbyDedupeCache
    {
        private readonly HashSet<Guid> _seen = new HashSet<Guid>();
        private readonly Queue<Guid> _order = new Queue<Guid>();
        private readonly int _capacity;
        private readonly object _gate = new object();

        public NearbyDedupeCache(int capacity = 4096) { _capacity = capacity; }

        /// <summary>True the first time an id is offered, false for every repeat.</summary>
        public bool TryAdd(Guid id)
        {
            lock (_gate)
            {
                if (!_seen.Add(id))
                    return false;
                _order.Enqueue(id);
                while (_order.Count > _capacity)
                    _seen.Remove(_order.Dequeue());
                return true;
            }
        }
    }
}
