using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    /// <summary>
    /// Thread-safe in-memory store for server-issued DID authentication nonces.
    /// Each nonce is tied to a DID, expires after <see cref="NonceTtlSeconds"/>, and is
    /// consumed (deleted) on first use so it cannot be replayed.
    ///
    /// For multi-node deployments replace this with a distributed cache (Redis, etc.)
    /// that shares state across all ONODE instances.
    /// </summary>
    public static class DIDChallengeStore
    {
        public const int NonceTtlSeconds = 300; // 5 minutes

        private sealed record Entry(string Nonce, DateTime ExpiresUtc);

        // Key = DID string, Value = pending challenge entry.
        // One pending challenge per DID at a time; issuing a new one invalidates the previous.
        private static readonly ConcurrentDictionary<string, Entry> _store = new();

        /// <summary>Generates a new cryptographically random nonce for the given DID and stores it.</summary>
        public static string Issue(string did)
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            _store[did] = new Entry(nonce, DateTime.UtcNow.AddSeconds(NonceTtlSeconds));
            PurgeExpired();
            return nonce;
        }

        /// <summary>
        /// Validates that <paramref name="nonce"/> was issued for <paramref name="did"/> and has not expired.
        /// Consumes (removes) the nonce on success so it cannot be reused.
        /// Returns false if the nonce is unknown, expired, or belongs to a different DID.
        /// </summary>
        public static bool ConsumeIfValid(string did, string nonce)
        {
            if (!_store.TryRemove(did, out var entry))
                return false;

            if (entry.ExpiresUtc < DateTime.UtcNow)
                return false;

            return string.Equals(entry.Nonce, nonce, StringComparison.Ordinal);
        }

        private static void PurgeExpired()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _store)
                if (kvp.Value.ExpiresUtc < now)
                    _store.TryRemove(kvp.Key, out _);
        }
    }
}
