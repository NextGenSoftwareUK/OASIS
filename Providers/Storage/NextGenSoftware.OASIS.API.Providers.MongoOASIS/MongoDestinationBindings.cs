using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

/// <summary>Reads and approves physical destinations inside the source owner's native transaction.</summary>
internal static class MongoDestinationBindings
{
    private static string Key(CosmicStorageIdentity source, ProviderType target)
        => source.BindingFingerprint + ":" + target;

    internal static async Task<(HolonReplicationDestinationBinding Binding, bool Recorded)> ReadAsync(
        IMongoDatabase database, IClientSessionHandle session, CosmicStorageIdentity source,
        ProviderType target, CancellationToken token)
    {
        var approvals = database.GetCollection<BsonDocument>("CosmicDestinationBinding");
        var row = await approvals.Find(session, new BsonDocument("_id", Key(source, target)))
            .SingleOrDefaultAsync(token).ConfigureAwait(false);
        if (row != null)
        {
            if (!row.TryGetValue("RecordJson", out var json) || !json.IsString
                || !row.TryGetValue("RecordDigest", out var digest) || !digest.IsString)
                throw new InvalidDataException("Invalid native destination approval record.");
            return (HolonDestinationBindingRecord.Read(json.AsString, digest.AsString, source, target), true);
        }

        // Upgrade bootstrap reads committed immutable route meaning, never live
        // target metadata. A missing approval is not permission to ignore history.
        var prefix = "hyperdrive-target-v2:" + target + ":";
        var filter = new BsonDocument {
            { "SourceId", HolonReplicationRouteBinding.NativeSourceId(source) },
            { "TargetId", new BsonDocument("$regex", "^" + System.Text.RegularExpressions.Regex.Escape(prefix)) }
        };
        using var cursor = await database.GetCollection<BsonDocument>("CosmicHolonOutbox")
            .Find(session, filter).Sort(new BsonDocument("Sequence", -1)).Limit(257)
            .ToCursorAsync(token).ConfigureAwait(false);
        var count = 0;
        long bytes = 0;
        while (await cursor.MoveNextAsync(token).ConfigureAwait(false))
        foreach (var historical in cursor.Current)
        {
            if (++count > 256 || (bytes += historical.ToBson().LongLength) > 16L * 1024 * 1024)
                throw new InvalidDataException("Destination history exceeds the bootstrap budget; explicit migration is required.");
            if (!historical.TryGetValue("OperationId", out var operationText) || !operationText.IsString
                || !Guid.TryParseExact(operationText.AsString, "D", out var operation) || operation == Guid.Empty)
                throw new InvalidDataException("Invalid historical destination operation identity.");
            var route = MongoHolonReplicationOutbox.ValidatePayload(historical, operation);
            if (route == null) continue; // Older transport contains no native identity proof.
            if (route.SourceStorage.BindingFingerprint != source.BindingFingerprint || route.TargetStorage.Authority != target)
                throw new InvalidDataException("Historical destination descriptor is outside the native source scope.");
            return (new HolonReplicationDestinationBinding(route, operation, operation), false);
        }
        return (null, false);
    }

    internal static async Task ApproveAsync(IMongoDatabase database, IClientSessionHandle session,
        HolonReplicationEnrollment enrollment, Guid operation, CancellationToken token)
    {
        foreach (var proposed in enrollment.NativeRoutes)
        {
            var known = await ReadAsync(database, session, proposed.SourceStorage, proposed.TargetStorage.Authority, token).ConfigureAwait(false);
            if (known.Binding != null && known.Binding.Route.TargetStorage.BindingFingerprint != proposed.TargetStorage.BindingFingerprint)
                throw new HolonDestinationRebindRequiredException();
            if (known.Recorded) continue;
            var binding = known.Binding ?? new HolonReplicationDestinationBinding(proposed, operation, operation);
            var json = HolonDestinationBindingRecord.Capture(binding);
            // The unique native _id serializes concurrent first approvals. A losing
            // transaction must fail, never silently replace the winning identity.
            await database.GetCollection<BsonDocument>("CosmicDestinationBinding").InsertOneAsync(session,
                new BsonDocument { { "_id", Key(proposed.SourceStorage, proposed.TargetStorage.Authority) },
                    { "RecordJson", json }, { "RecordDigest", HolonDestinationBindingRecord.Digest(json) } },
                cancellationToken: token).ConfigureAwait(false);
        }
    }
}
