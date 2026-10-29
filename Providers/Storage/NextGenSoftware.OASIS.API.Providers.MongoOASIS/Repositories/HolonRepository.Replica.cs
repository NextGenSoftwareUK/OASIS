using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NativeHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

internal sealed class MongoReplicaConflictException(IEnumerable<Guid> ids)
    : InvalidOperationException("Replica source lineage, local revision or enrollment differs for: " + string.Join(",", ids));
internal sealed class MongoReplicaHeadException(string message, Exception cause = null) : IOException(message, cause);

public partial class HolonRepository
{
    private async Task PrepareCommittedReplicaEntitiesAsync(IMongoDatabase database, IClientSessionHandle session, List<NativeHolon> entries,
        IReadOnlyDictionary<Guid, BsonDocument> observations, CommittedHolonReplicaPlan plan, CancellationToken cancellation)
    {
        var heads = database.GetCollection<BsonDocument>("CosmicReplicaHead");
        var previous = new Dictionary<Guid, IHolon>(); var conflicts = new List<Guid>();
        foreach (var row in entries.OrderBy(row => row.HolonId))
        {
            var current = observations.TryGetValue(row.HolonId, out var body) ? BsonSerializer.Deserialize<NativeHolon>(body) : null;
            var head = await heads.Find(session, new BsonDocument("_id", row.HolonId.ToString("D")))
                .SingleOrDefaultAsync(cancellation).ConfigureAwait(false);
            if (head == null)
            {
                if (current != null || row.Version != 1 || row.PreviousVersionId != Guid.Empty) conflicts.Add(row.HolonId);
                previous.Add(row.HolonId, null); continue;
            }
            Holon stored; Guid revision; bool hardDeleted; string sourceId; string sourceKind;
            try
            {
                var validated = ReadProtectedReplicaHead(head, row.HolonId);
                stored = validated.Snapshot; revision = validated.Evidence.ProviderRevisionId;
                hardDeleted = validated.Evidence.HardDeleted;
                sourceId = validated.Evidence.SourceId; sourceKind = validated.Evidence.SourceAuthority.ToString();
            }
            catch (Exception cause) when (cause is InvalidDataException or ArgumentException or FormatException)
            { throw new MongoReplicaHeadException("Committed MongoDB replica head is invalid.", cause); }
            previous.Add(row.HolonId, current == null ? stored : DataHelper.ConvertMongoEntityToOASISHolon(current, false));
            if (sourceId != plan.SourceId || sourceKind != plan.SourceAuthority.ToString()
                || row.PreviousVersionId != stored.VersionId || row.Version - 1 != stored.Version
                || (hardDeleted ? current != null : current == null || current.VersionId != stored.VersionId
                    || current.Version != stored.Version || current.ProviderRevisionId != revision)) conflicts.Add(row.HolonId);
        }
        if (conflicts.Count != 0) throw new MongoReplicaConflictException(conflicts);
        foreach (var row in entries)
        {
            var old = previous[row.HolonId];
            row.ProviderMetaData ??= new Dictionary<ProviderType, Dictionary<string, string>>();
            row.ProviderMetaData[ProviderType.MongoDBOASIS] = old?.ProviderMetaData != null
                && old.ProviderMetaData.TryGetValue(ProviderType.MongoDBOASIS, out var local) && local != null
                ? new Dictionary<string, string>(local) : new Dictionary<string, string>();
            // Only the target owns its native identity and metadata namespace.
            row.Id = old?.ProviderUniqueStorageKey != null && old.ProviderUniqueStorageKey.TryGetValue(ProviderType.MongoDBOASIS, out var key)
                ? key : ObjectId.GenerateNewId().ToString();
            row.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
            row.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = row.Id;
        }
    }

    private static HolonReplicaHeadRecord ReadProtectedReplicaHead(BsonDocument head, Guid id)
    {
        string Text(string name) => head.TryGetValue(name, out var value) && value.IsString ? value.AsString
            : throw new InvalidDataException("Committed replica head field is missing or invalid: " + name);
        if (!head.TryGetValue("HardDeleted", out var deleted) || !deleted.IsBoolean)
            throw new InvalidDataException("Committed replica deletion state is invalid.");
        return HolonReplicaHeadRecord.Read(id, ProviderType.MongoDBOASIS, Text("SourceId"), Text("SourceAuthority"),
            Text("SnapshotJson"), Text("SnapshotDigest"), Text("ProviderRevisionId"), deleted.AsBoolean);
    }

    private async Task WriteCommittedReplicaHeadsAsync(IMongoDatabase database, IClientSessionHandle session,
        IReadOnlyDictionary<Guid, BsonDocument> bodies, CommittedHolonReplicaPlan plan, CancellationToken cancellation)
    {
        var heads = database.GetCollection<BsonDocument>("CosmicReplicaHead");
        foreach (var pair in bodies.OrderBy(pair => pair.Key))
        {
            var record = DataHelper.ConvertMongoEntityToOASISHolon(BsonSerializer.Deserialize<NativeHolon>(pair.Value), false);
            var snapshot = StoredRecordJson.Serialize<IHolon>(record);
            var head = new BsonDocument { { "_id", pair.Key.ToString("D") }, { "SourceId", plan.SourceId },
                { "SourceAuthority", plan.SourceAuthority.ToString() }, { "SnapshotJson", snapshot }, { "SnapshotDigest", ReplicaHeadDigest(snapshot) },
                { "ProviderRevisionId", CosmicConcurrency.GetProviderRevisionId(record, ProviderType.MongoDBOASIS)!.Value.ToString("D") },
                { "HardDeleted", plan.Operation == ConditionalHolonOperation.HardDelete } };
            var saved = await heads.ReplaceOneAsync(session, new BsonDocument("_id", head["_id"]), head,
                new ReplaceOptions { IsUpsert = true }, cancellation).ConfigureAwait(false);
            if (!saved.IsAcknowledged || saved.MatchedCount != 1 && saved.UpsertedId == null)
                throw new InvalidDataException("Committed MongoDB replica head was not acknowledged.");
        }
    }

    private static string ReplicaHeadDigest(string snapshot)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshot)));
}
