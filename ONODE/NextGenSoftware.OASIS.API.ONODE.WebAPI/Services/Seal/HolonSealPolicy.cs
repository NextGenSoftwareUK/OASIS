using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Seal
{
    /// <summary>
    /// Node-side policy for sealed holon history. Answers from memory only (never the database), so the save path pays nothing.
    /// A save is sealed when the master switch and key are present AND (the holon owner's switch is on OR the calling app's switch is on).
    /// </summary>
    public sealed class HolonSealPolicy : IHolonSealPolicy
    {
        private readonly Func<HolonSealSettings> _settings;
        private readonly ConcurrentDictionary<Guid, bool> _owners = new ConcurrentDictionary<Guid, bool>();
        private volatile HashSet<string> _apps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public HolonSealPolicy(Func<HolonSealSettings> settings) { _settings = settings ?? throw new ArgumentNullException(nameof(settings)); }

        public HolonSealSettings Settings => _settings() ?? new HolonSealSettings();
        public bool MasterEnabled => Settings.Enabled;
        public bool HasKey => SigningKey?.IsUsable == true;
        public bool IsAvailable => MasterEnabled && HasKey;

        public HolonSealKey SigningKey { get { var s = Settings; return new HolonSealKey(s.KeyId, s.Key); } }

        public HolonSealKey FindKey(string keyId)
        {
            var s = Settings;
            if (string.IsNullOrEmpty(keyId)) return null;
            if (string.Equals(keyId, s.KeyId, StringComparison.Ordinal)) return new HolonSealKey(s.KeyId, s.Key);
            return s.RetiredKeys != null && s.RetiredKeys.TryGetValue(keyId, out var secret) ? new HolonSealKey(keyId, secret) : null;
        }

        public bool ShouldSeal(Guid ownerAvatarId, string appId)
        {
            if (!IsAvailable) return false;
            if (ownerAvatarId != Guid.Empty && _owners.TryGetValue(ownerAvatarId, out var on) && on) return true;
            return !string.IsNullOrEmpty(appId) && _apps.Contains(appId);
        }

        public bool OwnerSwitch(Guid avatarId) => _owners.TryGetValue(avatarId, out var on) && on;
        public bool AppSwitch(string appId) => !string.IsNullOrEmpty(appId) && _apps.Contains(appId);
        public void SetOwner(Guid avatarId, bool enabled) { if (enabled) _owners[avatarId] = true; else _owners.TryRemove(avatarId, out _); }
        public void ReplaceOwners(IEnumerable<Guid> avatars) { var keep = new HashSet<Guid>(avatars); foreach (var k in _owners.Keys.ToList()) if (!keep.Contains(k)) _owners.TryRemove(k, out _); foreach (var a in keep) _owners[a] = true; }
        public void ReplaceApps(IEnumerable<string> appIds) => _apps = new HashSet<string>(appIds.Where(a => !string.IsNullOrEmpty(a)), StringComparer.OrdinalIgnoreCase);
    }

    public interface IHolonSealStore
    {
        Task<bool> GetAsync(Guid avatarId, CancellationToken c);
        Task SetAsync(Guid avatarId, bool enabled, CancellationToken c);
        Task<IReadOnlyList<Guid>> ListEnabledAsync(CancellationToken c);
    }

    /// <summary>The per-avatar switch, kept server-side in the MongoDBOASIS database named in OASIS_DNA.json (collection AvatarHolonSeal). Default off: no record = off.</summary>
    public sealed class MongoHolonSealStore : IHolonSealStore
    {
        private readonly Func<OASISDNA> _dna;
        private IMongoDatabase _db;
        private readonly object _gate = new object();
        public MongoHolonSealStore(Func<OASISDNA> dna) { _dna = dna; }

        private IMongoCollection<BsonDocument> Records
        {
            get
            {
                lock (_gate)
                {
                    if (_db == null)
                    {
                        var mongo = _dna()?.OASIS?.StorageProviders?.MongoDBOASIS;
                        if (mongo == null || string.IsNullOrWhiteSpace(mongo.ConnectionString) || string.IsNullOrWhiteSpace(mongo.DBName))
                            throw new InvalidOperationException("Sealed history needs StorageProviders.MongoDBOASIS (ConnectionString and DBName) in OASIS_DNA.json.");
                        _db = new MongoClient(mongo.ConnectionString).GetDatabase(mongo.DBName);
                    }
                    return _db.GetCollection<BsonDocument>("AvatarHolonSeal");
                }
            }
        }

        private static BsonValue G(Guid id) => new BsonBinaryData(id, GuidRepresentation.Standard);

        public async Task<bool> GetAsync(Guid avatarId, CancellationToken c)
        {
            var d = await Records.Find(new BsonDocument("_id", G(avatarId))).FirstOrDefaultAsync(c).ConfigureAwait(false);
            return d != null && d.GetValue("enabled", false).AsBoolean;
        }

        public Task SetAsync(Guid avatarId, bool enabled, CancellationToken c)
            => Records.ReplaceOneAsync(new BsonDocument("_id", G(avatarId)),
                new BsonDocument { { "_id", G(avatarId) }, { "enabled", enabled }, { "updated", DateTime.UtcNow } },
                new ReplaceOptions { IsUpsert = true }, c);

        public async Task<IReadOnlyList<Guid>> ListEnabledAsync(CancellationToken c)
        {
            var docs = await Records.Find(new BsonDocument("enabled", true)).ToListAsync(c).ConfigureAwait(false);
            return docs.Select(d => d["_id"].AsGuid).ToList();
        }
    }
}
