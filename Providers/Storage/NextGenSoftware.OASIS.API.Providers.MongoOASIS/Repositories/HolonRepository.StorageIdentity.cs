using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal MongoHolonReplicationOutbox CaptureReplicationOutbox()=>new(_dbContext);
    internal MongoHolonReplicationOutbox CaptureReplicationOutbox(CosmicStorageIdentity expectedSource)=>new(_dbContext,expectedSource);

    internal async Task<CosmicStorageIdentity> CaptureStorageIdentityAsync(CancellationToken cancellationToken)
    {
        // Capture the same native handle that owns repository operations. Public
        // provider ConnectionString/DBName/Database may have changed independently.
        var database = _dbContext.MongoDB ?? throw new InvalidOperationException("The repository native database is closed.");
        var collection = database.GetCollection<Entities.Holon>(MongoDbContext.HolonCollectionName);
        using var cursor = await database.ListCollectionsAsync(new ListCollectionsOptions
        { Filter = new BsonDocument("name", collection.CollectionNamespace.CollectionName) }, cancellationToken).ConfigureAwait(false);
        var entries = await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
        if (entries.Count != 1 || !entries[0].TryGetValue("type", out var type) || type != "collection" ||
            !entries[0].TryGetValue("info", out var info) || !info.IsBsonDocument ||
            !info.AsBsonDocument.TryGetValue("uuid", out var uuid) || !uuid.IsBsonBinaryData ||
            uuid.AsBsonBinaryData.SubType != BsonBinarySubType.UuidStandard)
            throw new InvalidDataException("The native holon collection has no canonical durable UUID; it was not created or repaired.");
        var identity = uuid.AsBsonBinaryData.ToGuid(GuidRepresentation.Standard);
        if (identity == Guid.Empty) throw new InvalidDataException("The native holon collection UUID is empty.");

        return StorageIdentity(database, identity);
    }

    internal async Task<BsonDocument> ReadBoundConditionalReceiptAsync(Guid operation,CosmicStorageIdentity expected,
        CancellationToken token)
    {
        var database=_dbContext.MongoDB ?? throw new InvalidOperationException("The repository native database is closed.");
        using var session=await database.Client.StartSessionAsync(cancellationToken:token).ConfigureAwait(false);
        session.StartTransaction(new TransactionOptions(readConcern:ReadConcern.Majority,writeConcern:WriteConcern.WMajority));
        try
        {
            await ValidateReplicaStorageBindingAsync(database,session,expected,token,"COSMIC_SOURCE_STORAGE_BINDING").ConfigureAwait(false);
            return await database.GetCollection<BsonDocument>("CosmicWriteReceipt").WithReadPreference(ReadPreference.Primary)
                .Find(session,new BsonDocument("_id",operation.ToString("D"))).SingleOrDefaultAsync(token).ConfigureAwait(false);
        }
        finally {if(session.IsInTransaction)await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);}
    }

    private static CosmicStorageIdentity StorageIdentity(IMongoDatabase database, Guid identity)
    {
        var settings = database.Client.Settings;
        // Only actual namespace, seed endpoints and connection topology enter the
        // digest. Credentials and mutable public descriptors are excluded.
        var fields = new[] { "mongo-storage-endpoint-v1", database.DatabaseNamespace.DatabaseName,
            string.Join(",", settings.Servers.Select(server => server.ToString()).OrderBy(value => value, StringComparer.Ordinal)),
            settings.ReplicaSetName ?? string.Empty, settings.UseTls.ToString(), settings.DirectConnection.ToString() };
        var canonical = string.Concat(fields.Select(value => Encoding.UTF8.GetByteCount(value).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + value));
        using var hash = SHA256.Create();
        var endpoint = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", "");
        return new CosmicStorageIdentity(ProviderType.MongoDBOASIS, endpoint, identity);
    }

    internal static async Task ValidateReplicaStorageBindingAsync(IMongoDatabase database, IClientSessionHandle session,
        CosmicStorageIdentity expected, CancellationToken token,string codePrefix="COSMIC_REPLICA_STORAGE_BINDING")
    {
        if (expected == null) return;
        if (!Guid.TryParseExact(expected.StoreId, "D", out var uuid) || uuid == Guid.Empty || uuid.ToString("D") != expected.StoreId)
            throw new MongoReplicaStorageBindingException(codePrefix+"_INVALID", "A canonical native Mongo collection UUID is required.");
        if (StorageIdentity(database, uuid).BindingFingerprint != expected.BindingFingerprint)
            throw new MongoReplicaStorageBindingException(codePrefix+"_MISMATCH", "The native Mongo endpoint does not match the enrolled receiver.");
        try
        {
            // Mongo's native UUID assertion and namespace acquisition share the
            // transaction. A separate listCollections preflight would race DDL.
            // No documents or persistent cursor are requested by this admission.
            var command = new BsonDocument {
                { "find", MongoDbContext.HolonCollectionName },
                { "collectionUUID", new BsonBinaryData(uuid, GuidRepresentation.Standard) },
                { "filter", new BsonDocument("$expr", false) },
                { "singleBatch", true }, { "limit", 1 }
            };
            var response = await database.RunCommandAsync<BsonDocument>(session, command,
                ReadPreference.Primary, token).ConfigureAwait(false);
            if (!response.TryGetValue("cursor", out var cursor) || !cursor.IsBsonDocument ||
                cursor["id"] != 0 || !cursor.AsBsonDocument.TryGetValue("firstBatch", out var batch) ||
                !batch.IsBsonArray || batch.AsBsonArray.Count != 0)
                throw new MongoReplicaStorageBindingException(codePrefix+"_INVALID", "Native UUID admission returned an unexpected cursor.");
        }
        catch (MongoCommandException cause)
        {
            var mismatch = cause.CodeName == "CollectionUUIDMismatch" || cause.Code == 361 || cause.Code == 26;
            throw new MongoReplicaStorageBindingException(mismatch ? codePrefix+"_MISMATCH"
                : codePrefix+"_INVALID", "Native collection UUID admission was refused; no unbound apply was attempted.", cause);
        }
    }
}

internal sealed class MongoReplicaStorageBindingException(string code, string message, Exception cause = null)
    : InvalidOperationException(message, cause)
{ internal string Code { get; } = code; }
