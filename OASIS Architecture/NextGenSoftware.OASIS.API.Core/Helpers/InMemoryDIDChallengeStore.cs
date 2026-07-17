using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    /// <summary>
    /// In-process DID challenge store. Fast, zero-dependency, suitable for single-node ONODE deployments.
    /// For multi-node deployments use RedisDIDChallengeStore (in ONODE.WebAPI) so all nodes share state.
    /// </summary>
    public sealed class InMemoryDIDChallengeStore : IDIDChallengeStore
    {
        private sealed record Entry(string Nonce, DateTime ExpiresUtc);
        private readonly ConcurrentDictionary<string, Entry> _store = new();
        private readonly int _ttlSeconds;

        public InMemoryDIDChallengeStore(int ttlSeconds = DIDChallengeStore.NonceTtlSeconds)
            => _ttlSeconds = ttlSeconds;

        public string Issue(string did)
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            _store[did] = new Entry(nonce, DateTime.UtcNow.AddSeconds(_ttlSeconds));
            PurgeExpired();
            return nonce;
        }

        public bool ConsumeIfValid(string did, string nonce)
        {
            if (!_store.TryRemove(did, out var entry))
                return false;

            if (entry.ExpiresUtc < DateTime.UtcNow)
                return false;

            return string.Equals(entry.Nonce, nonce, StringComparison.Ordinal);
        }

        private void PurgeExpired()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _store)
                if (kvp.Value.ExpiresUtc < now)
                    _store.TryRemove(kvp.Key, out _);
        }
    }
}
