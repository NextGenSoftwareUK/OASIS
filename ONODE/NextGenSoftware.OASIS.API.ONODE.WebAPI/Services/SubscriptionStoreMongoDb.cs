using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.SubscriptionStore;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>MongoDB-backed store for subscriptions, credits balance, and API usage.</summary>
    public class SubscriptionStoreMongoDb : ISubscriptionStore
    {
        private readonly IMongoCollection<SubscriptionRecord> _subscriptions;
        private readonly IMongoCollection<CreditsBalanceRecord> _credits;
        private readonly IMongoCollection<ApiUsageRecord> _usage;

        public bool IsConfigured => _subscriptions != null;

        public SubscriptionStoreMongoDb(IConfiguration configuration)
        {
            // Use SubscriptionStore if set; otherwise use same MongoDB as MongoDBOASIS (from OASIS_DNA.json)
            var conn = configuration["SubscriptionStore:ConnectionString"]
                ?? configuration["OASIS:StorageProviders:MongoDBOASIS:ConnectionString"];
            var dbName = configuration["SubscriptionStore:DatabaseName"]
                ?? configuration["OASIS:StorageProviders:MongoDBOASIS:DBName"]
                ?? "OASISAPI_DEV";
            if (string.IsNullOrWhiteSpace(conn))
            {
                // No MongoDB configured: use in-memory fallback is handled by caller; we throw on use
                _subscriptions = null;
                _credits = null;
                _usage = null;
                return;
            }
            var client = new MongoClient(conn);
            var db = client.GetDatabase(dbName);
            _subscriptions = db.GetCollection<SubscriptionRecord>("subscriptions");
            _credits = db.GetCollection<CreditsBalanceRecord>("credits_balance");
            _usage = db.GetCollection<ApiUsageRecord>("api_usage");
            CreateIndexes(db);
        }

        private void CreateIndexes(IMongoDatabase db)
        {
            try
            {
                var subCol = db.GetCollection<SubscriptionRecord>("subscriptions");
                subCol.Indexes.CreateOne(new CreateIndexModel<SubscriptionRecord>(
                    Builders<SubscriptionRecord>.IndexKeys.Ascending(x => x.AvatarId),
                    new CreateIndexOptions { Unique = false }));
                var credCol = db.GetCollection<CreditsBalanceRecord>("credits_balance");
                credCol.Indexes.CreateOne(new CreateIndexModel<CreditsBalanceRecord>(
                    Builders<CreditsBalanceRecord>.IndexKeys.Ascending(x => x.AvatarId),
                    new CreateIndexOptions { Unique = true }));
                var useCol = db.GetCollection<ApiUsageRecord>("api_usage");
                useCol.Indexes.CreateOne(new CreateIndexModel<ApiUsageRecord>(
                    Builders<ApiUsageRecord>.IndexKeys.Ascending(x => x.AvatarId).Ascending(x => x.PeriodStart),
                    new CreateIndexOptions { Unique = true }));
            }
            catch
            {
                // Index creation best-effort
            }
        }

        public async Task<SubscriptionRecord> GetSubscriptionAsync(string avatarId)
        {
            if (!IsConfigured) return null;
            var cursor = await _subscriptions.FindAsync(x => x.AvatarId == avatarId).ConfigureAwait(false);
            return await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task<SubscriptionRecord> GetSubscriptionByStripeSubscriptionIdAsync(string stripeSubscriptionId)
        {
            if (!IsConfigured) return null;
            var cursor = await _subscriptions.FindAsync(x => x.StripeSubscriptionId == stripeSubscriptionId).ConfigureAwait(false);
            return await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task UpsertSubscriptionAsync(SubscriptionRecord record)
        {
            if (!IsConfigured) return;
            record.UpdatedAt = DateTime.UtcNow;
            await _subscriptions.ReplaceOneAsync(
                x => x.AvatarId == record.AvatarId,
                record,
                new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
        }

        public async Task<decimal> GetCreditsBalanceAsync(string avatarId)
        {
            if (!IsConfigured) return 0;
            var cursor = await _credits.FindAsync(x => x.AvatarId == avatarId).ConfigureAwait(false);
            var doc = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
            return doc?.BalanceUsd ?? 0;
        }

        public async Task AddCreditsAsync(string avatarId, decimal amountUsd)
        {
            if (!IsConfigured) return;
            var filter = Builders<CreditsBalanceRecord>.Filter.Eq(x => x.AvatarId, avatarId);
            var update = Builders<CreditsBalanceRecord>.Update
                .Inc(x => x.BalanceUsd, amountUsd)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);
            var opts = new FindOneAndUpdateOptions<CreditsBalanceRecord>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };
            await _credits.FindOneAndUpdateAsync(filter, update, opts).ConfigureAwait(false);
        }

        public async Task<bool> DeductCreditsAsync(string avatarId, decimal amountUsd)
        {
            if (!IsConfigured) return false;
            var filter = Builders<CreditsBalanceRecord>.Filter.And(
                Builders<CreditsBalanceRecord>.Filter.Eq(x => x.AvatarId, avatarId),
                Builders<CreditsBalanceRecord>.Filter.Gte(x => x.BalanceUsd, amountUsd));
            var update = Builders<CreditsBalanceRecord>.Update
                .Inc(x => x.BalanceUsd, -amountUsd)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);
            var result = await _credits.UpdateOneAsync(filter, update).ConfigureAwait(false);
            return result.ModifiedCount > 0;
        }

        public async Task<int> GetUsageAsync(string avatarId, DateTime periodStartUtc)
        {
            if (!IsConfigured) return 0;
            var cursor = await _usage.FindAsync(x => x.AvatarId == avatarId && x.PeriodStart == periodStartUtc).ConfigureAwait(false);
            var doc = await cursor.FirstOrDefaultAsync().ConfigureAwait(false);
            return doc?.RequestCount ?? 0;
        }

        public async Task<int> IncrementUsageAsync(string avatarId, DateTime periodStartUtc)
        {
            if (!IsConfigured) return 0;
            var filter = Builders<ApiUsageRecord>.Filter.And(
                Builders<ApiUsageRecord>.Filter.Eq(x => x.AvatarId, avatarId),
                Builders<ApiUsageRecord>.Filter.Eq(x => x.PeriodStart, periodStartUtc));
            var update = Builders<ApiUsageRecord>.Update
                .Inc(x => x.RequestCount, 1)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);
            var opts = new FindOneAndUpdateOptions<ApiUsageRecord>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };
            var doc = await _usage.FindOneAndUpdateAsync(filter, update, opts).ConfigureAwait(false);
            return doc?.RequestCount ?? 1;
        }
    }
}
