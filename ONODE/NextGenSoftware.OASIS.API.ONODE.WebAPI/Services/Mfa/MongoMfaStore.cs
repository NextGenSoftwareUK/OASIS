using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa
{
    /// <summary>
    /// MFA state kept server-side in the MongoDBOASIS database named in OASIS_DNA.json (collections AvatarMfa, AvatarMfaChallenges,
    /// AvatarMfaEvents). Not stored on the avatar, so no API user can read or edit it. Challenges expire by TTL index.
    /// </summary>
    public class MongoMfaStore : IMfaStore
    {
        private readonly Func<OASISDNA> _dna;
        private IMongoDatabase _db;
        private readonly object _gate = new object();
        public MongoMfaStore(Func<OASISDNA> dna) { _dna = dna; }

        private IMongoDatabase Db()
        {
            lock (_gate)
            {
                if (_db != null) return _db;
                var dna = _dna();
                var mongo = dna?.OASIS?.StorageProviders?.MongoDBOASIS;
                if (mongo == null || string.IsNullOrWhiteSpace(mongo.ConnectionString) || string.IsNullOrWhiteSpace(mongo.DBName))
                    throw new InvalidOperationException("MFA needs StorageProviders.MongoDBOASIS (ConnectionString and DBName) in OASIS_DNA.json.");
                var db = new MongoClient(mongo.ConnectionString).GetDatabase(mongo.DBName);
                db.GetCollection<BsonDocument>("AvatarMfaChallenges").Indexes.CreateOne(new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending("expires"), new CreateIndexOptions { Name = "ix_ttl", ExpireAfter = TimeSpan.Zero }));
                var days = dna.OASIS.Security?.Mfa?.EventRetentionDays ?? 730;
                var events = db.GetCollection<BsonDocument>("AvatarMfaEvents");
                events.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("avatar").Descending("t"), new CreateIndexOptions { Name = "ix_avatar_time" }));
                events.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("t"),
                    new CreateIndexOptions { Name = "ix_ttl", ExpireAfter = days > 0 ? TimeSpan.FromDays(days) : (TimeSpan?)null }));
                return _db = db;
            }
        }

        private static BsonValue G(Guid id) => new BsonBinaryData(id, GuidRepresentation.Standard);
        private IMongoCollection<BsonDocument> Records => Db().GetCollection<BsonDocument>("AvatarMfa");
        private IMongoCollection<BsonDocument> Challenges => Db().GetCollection<BsonDocument>("AvatarMfaChallenges");
        private IMongoCollection<BsonDocument> Events => Db().GetCollection<BsonDocument>("AvatarMfaEvents");

        public async Task<MfaRecord> GetAsync(Guid avatarId, CancellationToken c)
        {
            var d = await Records.Find(new BsonDocument("_id", G(avatarId))).FirstOrDefaultAsync(c).ConfigureAwait(false);
            if (d == null) return null;
            return new MfaRecord
            {
                AvatarId = avatarId,
                Enabled = d.GetValue("enabled", false).AsBoolean,
                SecretCipher = d.GetValue("secret", BsonNull.Value).IsBsonNull ? null : d["secret"].AsString,
                PendingSecretCipher = d.GetValue("pending", BsonNull.Value).IsBsonNull ? null : d["pending"].AsString,
                RecoveryHashes = d.GetValue("recovery", new BsonArray()).AsBsonArray.Select(v => v.AsString).ToList(),
                LastStep = d.GetValue("lastStep", -1L).ToInt64(),
                FailedCount = d.GetValue("failed", 0).ToInt32(),
                LockedUntil = d.GetValue("lockedUntil", BsonNull.Value).IsBsonNull ? (DateTime?)null : d["lockedUntil"].ToUniversalTime(),
                UpdatedAt = d.GetValue("updated", DateTime.MinValue).ToUniversalTime()
            };
        }

        public Task SaveAsync(MfaRecord r, CancellationToken c)
            => Records.ReplaceOneAsync(new BsonDocument("_id", G(r.AvatarId)), new BsonDocument
            {
                { "_id", G(r.AvatarId) }, { "enabled", r.Enabled },
                { "secret", r.SecretCipher == null ? BsonNull.Value : (BsonValue)r.SecretCipher },
                { "pending", r.PendingSecretCipher == null ? BsonNull.Value : (BsonValue)r.PendingSecretCipher },
                { "recovery", new BsonArray(r.RecoveryHashes ?? new List<string>()) },
                { "lastStep", r.LastStep }, { "failed", r.FailedCount },
                { "lockedUntil", r.LockedUntil.HasValue ? (BsonValue)r.LockedUntil.Value : BsonNull.Value },
                { "updated", r.UpdatedAt }
            }, new ReplaceOptions { IsUpsert = true }, c);

        public Task DeleteAsync(Guid avatarId, CancellationToken c) => Records.DeleteOneAsync(new BsonDocument("_id", G(avatarId)), c);

        public Task SaveChallengeAsync(MfaChallenge ch, CancellationToken c)
            => Challenges.InsertOneAsync(new BsonDocument { { "_id", ch.Id }, { "avatar", G(ch.AvatarId) }, { "expires", ch.ExpiresAt }, { "attempts", 0 }, { "ip", ch.IpAddress == null ? BsonNull.Value : (BsonValue)ch.IpAddress } }, cancellationToken: c);

        public async Task<MfaChallenge> GetChallengeAsync(string id, CancellationToken c)
        {
            var d = await Challenges.Find(new BsonDocument("_id", id)).FirstOrDefaultAsync(c).ConfigureAwait(false);
            return d == null ? null : new MfaChallenge { Id = id, AvatarId = d["avatar"].AsGuid, ExpiresAt = d["expires"].ToUniversalTime(), Attempts = d["attempts"].ToInt32() };
        }

        public async Task<int> AddChallengeAttemptAsync(string id, CancellationToken c)
        {
            var d = await Challenges.FindOneAndUpdateAsync(new BsonDocument("_id", id), new BsonDocument("$inc", new BsonDocument("attempts", 1)),
                new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After }, c).ConfigureAwait(false);
            return d == null ? 0 : d["attempts"].ToInt32();
        }

        public async Task<bool> ConsumeChallengeAsync(string id, CancellationToken c)
            => (await Challenges.DeleteOneAsync(new BsonDocument("_id", id), c).ConfigureAwait(false)).DeletedCount == 1;

        public Task AddEventAsync(MfaEvent e, CancellationToken c)
            => Events.InsertOneAsync(new BsonDocument { { "_id", G(e.Id) }, { "t", e.Timestamp }, { "avatar", G(e.AvatarId) }, { "actor", G(e.ActorId) }, { "kind", e.Kind }, { "detail", e.Detail == null ? BsonNull.Value : (BsonValue)e.Detail } }, cancellationToken: c);

        public async Task<IReadOnlyList<MfaEvent>> GetEventsAsync(Guid avatarId, int limit, CancellationToken c)
        {
            var docs = await Events.Find(new BsonDocument("avatar", G(avatarId))).Sort(new BsonDocument("t", -1)).Limit(limit).ToListAsync(c).ConfigureAwait(false);
            return docs.Select(d => new MfaEvent
            {
                Id = d["_id"].AsGuid, Timestamp = d["t"].ToUniversalTime(), AvatarId = avatarId, ActorId = d["actor"].AsGuid, Kind = d["kind"].AsString,
                Detail = d["detail"].IsBsonNull ? null : d["detail"].AsString
            }).ToList();
        }
    }
}
