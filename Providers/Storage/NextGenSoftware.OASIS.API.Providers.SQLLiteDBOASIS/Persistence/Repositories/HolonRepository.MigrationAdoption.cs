using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public Task<OASISResult<HolonMigrationFenceObservation>> AdoptPreparedHolonReplicaAsync(
        HolonMigrationReplicaAdoptionPlan plan, Guid owner, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        token.ThrowIfCancellationRequested();
        var result = SqliteHolonMigrationFence.Execute(_dbContext.Database.GetDbConnection().ConnectionString,
            plan.Fence, SqliteHolonMigrationFence.Action.ApplyPreparedMutation, owner, token,
            plan.PreparationFingerprint, (connection, transaction) =>
            {
                using (var receipt = connection.CreateCommand())
                {
                    receipt.Transaction = transaction;
                    receipt.CommandText = "SELECT RequestJson,RequestHash,PreparationHash,RecordCount FROM CosmicMigrationReplicaAdoption WHERE MigrationId=$id";
                    receipt.Parameters.AddWithValue("$id", plan.Fence.MigrationId.ToString("D"));
                    using var reader = receipt.ExecuteReader();
                    if (reader.Read())
                    {
                        if (reader.GetString(0) != plan.CanonicalJson || reader.GetString(1) != plan.Fingerprint
                            || reader.GetString(2) != plan.PreparationFingerprint || reader.GetInt32(3) != plan.Records.Count)
                            throw new SqliteMigrationFenceException("COSMIC_MIGRATION_ADOPTION_RECEIPT_MISMATCH", "Original migration receipt cannot be replaced.");
                        if (reader.Read()) throw new InvalidDataException("Migration receipt identity is not unique.");
                        return false;
                    }
                }
                using var mapper = new DataContext(connection.ConnectionString);
                mapper.Database.SetDbConnection(connection, contextOwnsConnection: false);
                mapper.Database.UseTransaction(transaction);
                // Validate the whole graph before changing any head. Current business
                // content must match the protected native snapshot as well as its revision.
                foreach (var row in plan.Records)
                {
                    token.ThrowIfCancellationRequested();
                    using var head = connection.CreateCommand(); head.Transaction = transaction;
                    head.CommandText = "SELECT SourceId,SourceAuthority,SnapshotJson,SnapshotDigest,ProviderRevisionId,HardDeleted FROM CosmicReplicaHead WHERE HolonId=$id";
                    head.Parameters.AddWithValue("$id", row.Expected.HolonId.ToString("D"));
                    HolonReplicaHeadEvidence actual;
                    using (var reader = head.ExecuteReader())
                    {
                        if (!reader.Read()) throw new SqliteMigrationFenceException("COSMIC_MIGRATION_ADOPTION_CAS_FAILED", "Expected replica head is absent.");
                        var deleted = reader.GetInt64(5);
                        if (deleted is not (0 or 1)) throw new InvalidDataException("Replica deletion state is invalid.");
                        actual = HolonReplicaHeadRecord.Read(row.Expected.HolonId, ProviderType.SQLLiteDBOASIS,
                            reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), deleted == 1).Evidence;
                        if (reader.Read()) throw new InvalidDataException("Replica head identity is not unique.");
                    }
                    if (JsonSerializer.Serialize(actual) != JsonSerializer.Serialize(row.Head))
                        throw new SqliteMigrationFenceException("COSMIC_MIGRATION_ADOPTION_CAS_FAILED", "Replica head differs from approved evidence.");
                    var key = row.Expected.HolonId.ToString("D");
                    var current = mapper.Holons.AsNoTracking().Where(h => h.Id == key).Take(2).ToList();
                    if (current.Count > 1 || (current.Count == 1) != row.HasCurrentRecord)
                        throw new SqliteMigrationFenceException("COSMIC_MIGRATION_ADOPTION_CAS_FAILED", "Current existence differs from approved evidence.");
                    HolonReplicaHeadRecord.ValidateCurrent(row.Expected.HolonId, ProviderType.SQLLiteDBOASIS,
                        current.Count == 1 ? GetHolonFromEntity(current[0]) : null, actual);
                }
                foreach (var row in plan.Records)
                {
                    token.ThrowIfCancellationRequested();
                    using var update = connection.CreateCommand(); update.Transaction = transaction;
                    update.CommandText = "UPDATE CosmicReplicaHead SET SourceId=$new WHERE HolonId=$id AND SourceId=$old AND SourceAuthority=$authority AND SnapshotDigest=$snapshot AND ProviderRevisionId=$revision AND HardDeleted=$deleted";
                    update.Parameters.AddWithValue("$new", plan.ReplacementSourceId); update.Parameters.AddWithValue("$old", plan.OriginalSourceId);
                    update.Parameters.AddWithValue("$id", row.Expected.HolonId.ToString("D"));
                    update.Parameters.AddWithValue("$authority", row.Head.SourceAuthority.ToString());
                    update.Parameters.AddWithValue("$snapshot", row.Head.SnapshotDigest);
                    update.Parameters.AddWithValue("$revision", row.Head.ProviderRevisionId.ToString("D"));
                    update.Parameters.AddWithValue("$deleted", row.Head.HardDeleted ? 1 : 0);
                    if (update.ExecuteNonQuery() != 1) throw new SqliteMigrationFenceException("COSMIC_MIGRATION_ADOPTION_CAS_FAILED", "Atomic head comparison failed.");
                }
                using var insert = connection.CreateCommand(); insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO CosmicMigrationReplicaAdoption(MigrationId,RequestJson,RequestHash,PreparationHash,RecordCount) VALUES($id,$json,$hash,$preparation,$count)";
                insert.Parameters.AddWithValue("$id", plan.Fence.MigrationId.ToString("D")); insert.Parameters.AddWithValue("$json", plan.CanonicalJson);
                insert.Parameters.AddWithValue("$hash", plan.Fingerprint); insert.Parameters.AddWithValue("$preparation", plan.PreparationFingerprint);
                insert.Parameters.AddWithValue("$count", plan.Records.Count);
                if (insert.ExecuteNonQuery() != 1) throw new InvalidDataException("Migration adoption receipt acknowledgement is incomplete.");
                return true;
            });
        if (!result.IsError)
        {
            result.MetaData ??= new();
            result.MetaData["COSMIC.MigrationAdoptionReceipt"] = plan.Fingerprint;
        }
        return Task.FromResult(result);
    }
}
