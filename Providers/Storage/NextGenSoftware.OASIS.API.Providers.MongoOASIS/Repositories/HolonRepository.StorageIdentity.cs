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
    internal async Task<CosmicStorageIdentity> CaptureStorageIdentityAsync(CancellationToken cancellationToken)
    {
        // Capture the same native handle that owns repository operations. Public
        // provider ConnectionString/DBName/Database may have changed independently.
        var database = _dbContext.MongoDB ?? throw new InvalidOperationException("The repository native database is closed.");
        var collection = database.GetCollection<Entities.Holon>(MongoDbContext.HolonCollectionName);
        var settings = database.Client.Settings;
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
}
