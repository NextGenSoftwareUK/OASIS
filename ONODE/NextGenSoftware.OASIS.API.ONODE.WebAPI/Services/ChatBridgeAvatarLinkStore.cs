using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.ChatBridge;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// MongoDB-backed store for platform user → OASIS avatar links.
    /// Uses the same MongoDB connection as the rest of the ONODE services.
    /// Collection: ChatBridgeAvatarLinks
    /// </summary>
    public class ChatBridgeAvatarLinkStore : IChatBridgeAvatarLinkStore
    {
        private readonly IMongoCollection<BridgedAvatarLinkDocument> _collection;

        public ChatBridgeAvatarLinkStore(IConfiguration configuration)
        {
            var conn = configuration["SubscriptionStore:ConnectionString"]
                ?? configuration["OASIS:StorageProviders:MongoDBOASIS:ConnectionString"];
            var dbName = configuration["SubscriptionStore:DatabaseName"]
                ?? configuration["OASIS:StorageProviders:MongoDBOASIS:DBName"]
                ?? "OASISAPI_DEV";

            if (string.IsNullOrWhiteSpace(conn))
            {
                _collection = null;
                return;
            }

            var client = new MongoClient(conn);
            var db = client.GetDatabase(dbName);
            _collection = db.GetCollection<BridgedAvatarLinkDocument>("ChatBridgeAvatarLinks");

            var indexKeys = Builders<BridgedAvatarLinkDocument>.IndexKeys
                .Ascending(x => x.Platform)
                .Ascending(x => x.PlatformUserId);
            _collection.Indexes.CreateOne(new CreateIndexModel<BridgedAvatarLinkDocument>(indexKeys));
        }

        public bool IsConfigured => _collection != null;

        public async Task<BridgedAvatarLinkDocument> GetByDiscordUserIdAsync(ulong userId)
        {
            if (_collection == null) return null;
            var id = $"discord:{userId}";
            return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task<BridgedAvatarLinkDocument> GetByTelegramUserIdAsync(long userId)
        {
            if (_collection == null) return null;
            var id = $"telegram:{userId}";
            return await _collection.Find(x => x.Id == id).FirstOrDefaultAsync().ConfigureAwait(false);
        }

        public async Task UpsertAsync(BridgedAvatarLinkDocument doc)
        {
            if (_collection == null || doc == null) return;
            await _collection.ReplaceOneAsync(
                Builders<BridgedAvatarLinkDocument>.Filter.Eq(x => x.Id, doc.Id),
                doc,
                new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
        }

        public async Task<bool> RemoveByDiscordUserIdAsync(ulong userId)
        {
            if (_collection == null) return false;
            var result = await _collection.DeleteOneAsync(x => x.Id == $"discord:{userId}").ConfigureAwait(false);
            return result.DeletedCount > 0;
        }

        public async Task<bool> RemoveByTelegramUserIdAsync(long userId)
        {
            if (_collection == null) return false;
            var result = await _collection.DeleteOneAsync(x => x.Id == $"telegram:{userId}").ConfigureAwait(false);
            return result.DeletedCount > 0;
        }
    }
}
