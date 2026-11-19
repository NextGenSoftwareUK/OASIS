using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NativeHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<OASISResult<HolonMigrationFenceObservation>> AdoptPreparedHolonReplicaAsync(
        HolonMigrationReplicaAdoptionPlan plan, Guid owner, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(plan);
        token.ThrowIfCancellationRequested();
        var database = _dbContext.MongoDB;
        var result = await MongoHolonMigrationFence.ExecuteAsync(database, plan.Fence,
            MongoHolonMigrationFence.Action.ApplyPreparedMutation, owner, token, plan.PreparationFingerprint,
            async (session, cancellation) =>
            {
                var receipts = database.GetCollection<BsonDocument>("CosmicMigrationReplicaAdoption");
                var receiptId = new BsonDocument("_id", plan.Fence.MigrationId.ToString("D"));
                var retained = await receipts.Find(session, receiptId).SingleOrDefaultAsync(cancellation).ConfigureAwait(false);
                if (retained != null)
                {
                    if (retained.GetValue("RequestJson", BsonNull.Value) != plan.CanonicalJson
                        || retained.GetValue("RequestHash", BsonNull.Value) != plan.Fingerprint
                        || retained.GetValue("PreparationHash", BsonNull.Value) != plan.PreparationFingerprint
                        || retained.GetValue("RecordCount", BsonNull.Value) != plan.Records.Count)
                        throw new MongoMigrationFenceException("COSMIC_MIGRATION_ADOPTION_RECEIPT_MISMATCH", "Original adoption receipt cannot be replaced.");
                    return false;
                }
                var heads = database.GetCollection<BsonDocument>("CosmicReplicaHead");
                // All V2 writers share the owner gates acquired by the native
                // transaction owner. Validate the complete graph before any head CAS.
                foreach (var row in plan.Records)
                {
                    cancellation.ThrowIfCancellationRequested();
                    var body = await heads.Find(session, new BsonDocument("_id", row.Expected.HolonId.ToString("D")))
                        .SingleOrDefaultAsync(cancellation).ConfigureAwait(false);
                    if (body == null) throw new MongoMigrationFenceException("COSMIC_MIGRATION_ADOPTION_CAS_FAILED", "Expected replica head is absent.");
                    var actual = ReadProtectedReplicaHead(body, row.Expected.HolonId).Evidence;
                    if (JsonSerializer.Serialize(actual) != JsonSerializer.Serialize(row.Head))
                        throw new MongoMigrationFenceException("COSMIC_MIGRATION_ADOPTION_CAS_FAILED", "Native head differs from the approved observation.");
                    var current = await database.GetCollection<BsonDocument>(MongoDbContext.HolonCollectionName)
                        .Find(session, new BsonDocument("HolonId", row.Expected.HolonId.ToString("D"))).Limit(2).ToListAsync(cancellation).ConfigureAwait(false);
                    if (current.Count > 1 || (current.Count == 1) != row.HasCurrentRecord)
                        throw new MongoMigrationFenceException("COSMIC_MIGRATION_ADOPTION_CAS_FAILED", "Current record existence differs from the approved observation.");
                    HolonReplicaHeadRecord.ValidateCurrent(row.Expected.HolonId, ProviderType.MongoDBOASIS,
                        current.Count == 1 ? Helpers.DataHelper.ConvertMongoEntityToOASISHolon(BsonSerializer.Deserialize<NativeHolon>(current[0]), mapChildren: false) : null, actual);
                }
                foreach (var row in plan.Records)
                {
                    var matched = await heads.UpdateOneAsync(session, new BsonDocument {
                        { "_id", row.Expected.HolonId.ToString("D") }, { "SourceId", plan.OriginalSourceId },
                        { "SourceAuthority", row.Head.SourceAuthority.ToString() }, { "SnapshotDigest", row.Head.SnapshotDigest },
                        { "ProviderRevisionId", row.Head.ProviderRevisionId.ToString("D") }, { "HardDeleted", row.Head.HardDeleted } },
                        new BsonDocument("$set", new BsonDocument("SourceId", plan.ReplacementSourceId)), cancellationToken: cancellation).ConfigureAwait(false);
                    if (matched.MatchedCount != 1 || matched.ModifiedCount != (plan.OriginalSourceId == plan.ReplacementSourceId ? 0 : 1))
                        throw new MongoMigrationFenceException("COSMIC_MIGRATION_ADOPTION_CAS_FAILED", "Atomic native head comparison failed.");
                }
                await receipts.InsertOneAsync(session, new BsonDocument { { "_id", plan.Fence.MigrationId.ToString("D") },
                    { "RequestJson", plan.CanonicalJson }, { "RequestHash", plan.Fingerprint },
                    { "PreparationHash", plan.PreparationFingerprint }, { "RecordCount", plan.Records.Count } }, cancellationToken: cancellation).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);
        if (!result.IsError)
        {
            result.MetaData ??= new();
            result.MetaData["COSMIC.MigrationAdoptionReceipt"] = plan.Fingerprint;
        }
        return result;
    }
}
