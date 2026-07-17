using System;
using System.Security.Cryptography;
using NextGenSoftware.OASIS.API.Core.Helpers;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers
{
    /// <summary>
    /// Redis-backed DID challenge store for multi-node ONODE deployments.
    /// Each nonce is stored as a Redis string key with a native TTL so expiry
    /// is handled by Redis without any background cleanup needed on the .NET side.
    ///
    /// Configure in OASISDNA:
    ///   "DIDChallengeStore": {
    ///     "Provider": "Redis",
    ///     "RedisConnectionString": "localhost:6379",
    ///     "RedisKeyPrefix": "oasis:did:challenge:",
    ///     "NonceTtlSeconds": 300
    ///   }
    /// </summary>
    public sealed class RedisDIDChallengeStore : IDIDChallengeStore
    {
        private readonly IDatabase _db;
        private readonly string _keyPrefix;
        private readonly int _ttlSeconds;

        public RedisDIDChallengeStore(string connectionString, string keyPrefix = "oasis:did:challenge:", int ttlSeconds = DIDChallengeStore.NonceTtlSeconds)
        {
            var muxer   = ConnectionMultiplexer.Connect(connectionString);
            _db         = muxer.GetDatabase();
            _keyPrefix  = keyPrefix;
            _ttlSeconds = ttlSeconds;
        }

        public string Issue(string did)
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            _db.StringSet(Key(did), nonce, TimeSpan.FromSeconds(_ttlSeconds));
            return nonce;
        }

        public bool ConsumeIfValid(string did, string nonce)
        {
            var key = Key(did);

            // Lua script: atomic get-and-delete so two concurrent requests can't both consume the same nonce
            const string lua = @"
                local v = redis.call('GET', KEYS[1])
                if v == ARGV[1] then
                    redis.call('DEL', KEYS[1])
                    return 1
                end
                return 0";

            var result = (long)_db.ScriptEvaluate(lua, new RedisKey[] { key }, new RedisValue[] { nonce });
            return result == 1;
        }

        private string Key(string did) => _keyPrefix + did;
    }
}
