using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Purge
{
    /// <summary>
    /// Purge bookkeeping in the MongoDBOASIS database named in OASIS_DNA.json: HolonPurge (one small document per holon: schedule, hold,
    /// purged), HolonPurgeAudit (insert-only, no content) and AvatarVersionPurge (the per-avatar choice). The worker reads only the
    /// HolonPurge collection, through an index on (Status, PurgeAfterUtc), and never the Holon collection.
    /// </summary>
    public sealed class MongoHolonPurgeStore : IHolonPurgeStore
    {
        private readonly Func<OASISDNA> _dna;
        private IMongoDatabase _db;
        private bool _indexed;
        private readonly object _gate = new object();
        public MongoHolonPurgeStore(Func<OASISDNA> dna) { _dna = dna; }

        private IMongoDatabase Db
        {
            get
            {
                lock (_gate)
                {
                    if (_db == null)
                    {
                        var mongo = _dna()?.OASIS?.StorageProviders?.MongoDBOASIS;
                        if (mongo == null || string.IsNullOrWhiteSpace(mongo.ConnectionString) || string.IsNullOrWhiteSpace(mongo.DBName))
                            throw new InvalidOperationException("Version purge needs StorageProviders.MongoDBOASIS (ConnectionString and DBName) in OASIS_DNA.json.");
                        _db = new MongoClient(mongo.ConnectionString).GetDatabase(mongo.DBName);
                    }
                    return _db;
                }
            }
        }

        private IMongoCollection<BsonDocument> Entries
        {
            get
            {
                var collection = Db.GetCollection<BsonDocument>("HolonPurge");
                if (!_indexed)
                {
                    collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("status").Ascending("purgeAfter")));
                    _indexed = true;
                }
                return collection;
            }
        }
        private IMongoCollection<BsonDocument> Audit => Db.GetCollection<BsonDocument>("HolonPurgeAudit");
        private IMongoCollection<BsonDocument> Avatars => Db.GetCollection<BsonDocument>("AvatarVersionPurge");

        private static BsonValue G(Guid id) => new BsonBinaryData(id, GuidRepresentation.Standard);
        private static DateTime? D(BsonDocument d, string n) => d.TryGetValue(n, out var v) && v.IsValidDateTime ? v.ToUniversalTime() : (DateTime?)null;
        private static Guid Id(BsonDocument d, string n) => d.TryGetValue(n, out var v) && v.IsBsonBinaryData ? v.AsGuid : Guid.Empty;

        private static HolonPurgeEntry ToEntry(BsonDocument d) => new HolonPurgeEntry
        {
            HolonId = d["_id"].AsGuid, OwnerAvatarId = Id(d, "owner"), AppId = d.GetValue("app", BsonNull.Value).IsString ? d["app"].AsString : null,
            Status = d.GetValue("status", "none").AsString, DeletedDateUtc = D(d, "deleted"), PurgeAfterUtc = D(d, "purgeAfter"),
            Hold = d.GetValue("hold", false).AsBoolean, HoldReason = d.GetValue("holdReason", BsonNull.Value).IsString ? d["holdReason"].AsString : null,
            HoldByAvatarId = Id(d, "holdBy"), HoldAtUtc = D(d, "holdAt"), PurgedAtUtc = D(d, "purgedAt"),
            VersionsRemoved = d.GetValue("versionsRemoved", 0).ToInt32(), CancelReason = d.GetValue("cancelReason", BsonNull.Value).IsString ? d["cancelReason"].AsString : null
        };

        public async Task<HolonPurgeEntry> GetAsync(Guid holonId, CancellationToken c)
        {
            var d = await Entries.Find(new BsonDocument("_id", G(holonId))).FirstOrDefaultAsync(c).ConfigureAwait(false);
            return d == null ? null : ToEntry(d);
        }

        public Task SchedulePendingAsync(HolonPurgeEntry e, CancellationToken c)
            => Entries.UpdateOneAsync(new BsonDocument("_id", G(e.HolonId)),
                Builders<BsonDocument>.Update
                    .Set("status", "pending").Set("owner", G(e.OwnerAvatarId)).Set("app", e.AppId == null ? (BsonValue)BsonNull.Value : e.AppId)
                    .Set("deleted", e.DeletedDateUtc ?? DateTime.UtcNow).Set("purgeAfter", e.PurgeAfterUtc ?? DateTime.UtcNow).Unset("cancelReason"),
                new UpdateOptions { IsUpsert = true }, c);

        public Task CancelAsync(Guid holonId, string reason, CancellationToken c)
            => Entries.UpdateOneAsync(new BsonDocument { { "_id", G(holonId) }, { "status", "pending" } },
                Builders<BsonDocument>.Update.Set("status", "cancelled").Set("cancelReason", reason ?? ""), cancellationToken: c);

        public Task SetHoldAsync(Guid holonId, Guid ownerAvatarId, bool hold, string reason, Guid byAvatarId, DateTime nowUtc, CancellationToken c)
            => Entries.UpdateOneAsync(new BsonDocument("_id", G(holonId)),
                Builders<BsonDocument>.Update.Set("hold", hold).Set("holdReason", reason ?? "").Set("holdBy", G(byAvatarId)).Set("holdAt", nowUtc)
                    .SetOnInsert("owner", G(ownerAvatarId)).SetOnInsert("status", "none"),
                new UpdateOptions { IsUpsert = true }, c);

        public async Task<IReadOnlyList<HolonPurgeEntry>> ListDueAsync(DateTime nowUtc, int limit, CancellationToken c)
        {
            var docs = await Entries.Find(new BsonDocument { { "status", "pending" }, { "purgeAfter", new BsonDocument("$lte", nowUtc) } })
                .Sort(Builders<BsonDocument>.Sort.Ascending("purgeAfter")).Limit(Math.Max(1, limit)).ToListAsync(c).ConfigureAwait(false);
            return docs.Select(ToEntry).ToList();
        }

        public Task MarkPurgedAsync(Guid holonId, Guid ownerAvatarId, DateTime purgedAtUtc, int versionsRemoved, CancellationToken c)
            => Entries.UpdateOneAsync(new BsonDocument("_id", G(holonId)),
                Builders<BsonDocument>.Update.Set("status", "purged").Set("purgedAt", purgedAtUtc).Set("versionsRemoved", versionsRemoved)
                    .Set("owner", G(ownerAvatarId)).Unset("purgeAfter"),
                new UpdateOptions { IsUpsert = true }, c);

        public Task WriteAuditAsync(HolonPurgeAuditRecord r, CancellationToken c)
            => Audit.InsertOneAsync(new BsonDocument
            {
                { "_id", Guid.NewGuid().ToString() }, { "holonId", G(r.HolonId) }, { "purgedAt", r.PurgedAtUtc }, { "byAvatarId", G(r.ByAvatarId) },
                { "trigger", r.Trigger ?? "" }, { "app", r.AppId == null ? (BsonValue)BsonNull.Value : r.AppId }, { "versionsRemoved", r.VersionsRemoved },
                { "storesPurged", new BsonArray(r.StoresPurged) }, { "storesNotPurged", new BsonArray(r.StoresNotPurged) }
            }, cancellationToken: c);

        private static PurgeChoice ToChoice(BsonDocument d) => new PurgeChoice
        {
            Enabled = d.TryGetValue("enabled", out var e) && e.IsBoolean ? e.AsBoolean : (bool?)null,
            Days = d.TryGetValue("days", out var n) && n.IsNumeric ? n.ToInt32() : (int?)null
        };

        public async Task<IReadOnlyDictionary<Guid, PurgeChoice>> ListAvatarChoicesAsync(CancellationToken c)
        {
            var docs = await Avatars.Find(new BsonDocument()).ToListAsync(c).ConfigureAwait(false);
            return docs.ToDictionary(d => d["_id"].AsGuid, ToChoice);
        }

        public async Task<PurgeChoice> GetAvatarChoiceAsync(Guid avatarId, CancellationToken c)
        {
            var d = await Avatars.Find(new BsonDocument("_id", G(avatarId))).FirstOrDefaultAsync(c).ConfigureAwait(false);
            return d == null ? new PurgeChoice() : ToChoice(d);
        }

        public Task SetAvatarChoiceAsync(Guid avatarId, PurgeChoice choice, CancellationToken c)
        {
            if (choice == null || choice.IsEmpty) return Avatars.DeleteOneAsync(new BsonDocument("_id", G(avatarId)), c);
            var doc = new BsonDocument { { "_id", G(avatarId) }, { "updated", DateTime.UtcNow } };
            if (choice.Enabled.HasValue) doc["enabled"] = choice.Enabled.Value;
            if (choice.Days.HasValue) doc["days"] = choice.Days.Value;
            return Avatars.ReplaceOneAsync(new BsonDocument("_id", G(avatarId)), doc, new ReplaceOptions { IsUpsert = true }, c);
        }
    }
}
