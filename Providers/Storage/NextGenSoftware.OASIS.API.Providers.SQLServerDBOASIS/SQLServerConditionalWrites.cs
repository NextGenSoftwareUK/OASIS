using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;

public partial class SQLServerDBOASIS : IConditionalHolonStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)
        => ExecuteConditionalHolonWriteAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(
        ConditionalHolonWriteRequest request, CancellationToken cancellationToken = default)
    {
        var token = cancellationToken;
        var commitEntered = false;
        try
        {
            token.ThrowIfCancellationRequested();
            var plan = ConditionalHolonPlan.Create(request, ReplicaAuthority);
            if (!IsProviderActivated)
                throw new InvalidOperationException("Activate SQL Server before committing a conditional write.");
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, token).ConfigureAwait(false);
            await using (var receipt = new SqlCommand(
                "SELECT RequestHash,ResultJson FROM dbo.OASISWriteReceipts WITH (UPDLOCK,HOLDLOCK) WHERE OperationId=@Id;",
                connection, transaction))
            {
                receipt.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = plan.OperationId;
                await using var reader = await receipt.ExecuteReaderAsync(token).ConfigureAwait(false);
                if (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    if (reader.GetString(0) != plan.Fingerprint)
                        return ReplicaFailure("COSMIC_OPERATION_ID_REUSED", "The SQL Server operation ID belongs to different content.");
                    return ConditionalHolonAcknowledgement.Create(ConditionalHolonReceipt.Read(reader.GetString(1), plan, out _), plan.Operation);
                }
            }

            var previous = new Dictionary<Guid, Holon?>();
            var conflicts = new List<CosmicConcurrencyConflict>();
            var conditions = request.Write.Conditions.ToDictionary(condition => condition.HolonId);
            foreach (var node in plan.Nodes.OrderBy(node => node.Snapshot.Id))
            {
                var id = node.Snapshot.Id;
                var current = await ReadLockedReplicaHolonAsync(connection, transaction, id, token).ConfigureAwait(false);
                previous.Add(id, current);
                var expected = conditions[id].ExpectedRevisionId;
                var actual = current == null ? (Guid?)null
                    : CosmicConcurrency.GetProviderRevisionId(current, ReplicaAuthority) ?? Guid.Empty;
                if (expected != actual)
                    conflicts.Add(new CosmicConcurrencyConflict { HolonId = id, ExpectedRevisionId = expected,
                        ActualRevisionId = actual, ActualVersionId = current?.VersionId, ActualVersion = current?.Version });
            }
            if (conflicts.Count != 0)
                throw new CosmicConcurrencyException(conflicts);

            var now = DateTime.UtcNow;
            var records = new Dictionary<Guid, IHolon>();
            var saving = plan.Operation == ConditionalHolonOperation.Save;
            foreach (var node in plan.Nodes)
            {
                var old = previous[node.Snapshot.Id];
                if (saving && old?.IsDeleted == true)
                    throw new StorageLifecycleConflictException("An active save cannot overwrite a SQL Server tombstone.");
                var record = saving ? node.Snapshot : StoredRecordJson.Project<Holon>(old
                    ?? throw new InvalidOperationException("A conditional delete requires a current SQL Server record."));
                record.ChildRoleManifestJson = HolonChildRolePersistence.Resolve(node,
                    saving ? HolonChildRoleWriteKind.SourceSave : HolonChildRoleWriteKind.SourceDelete,
                    old != null, old?.ChildRoleManifestJson);
                record.Version = old == null ? 1 : checked(old.Version + 1);
                record.PreviousVersionId = old?.VersionId ?? Guid.Empty;
                record.VersionId = Guid.NewGuid();
                record.CreatedDate = old?.CreatedDate ?? now;
                record.CreatedByAvatarId = old?.CreatedByAvatarId ?? plan.ActorId;
                record.CreatedProviderType = old == null ? new(ReplicaAuthority) : old.CreatedProviderType;
                record.ModifiedDate = old == null ? DateTime.MinValue : now;
                record.ModifiedByAvatarId = old == null ? Guid.Empty : plan.ActorId;
                record.InstanceSavedOnProviderType = new EnumValue<Core.Enums.ProviderType>(ReplicaAuthority);
                if (!saving)
                {
                    record.IsActive = false;
                    record.DeletedDate = now;
                    record.DeletedByAvatarId = plan.ActorId;
                }
                record.ProviderUniqueStorageKey ??= new();
                record.ProviderUniqueStorageKey[ReplicaAuthority] = record.Id.ToString("D");
                CosmicConcurrency.SetProviderRevisionId(record, ReplicaAuthority, Guid.NewGuid());
                var stored = ReadHolonSnapshot(record.Id, Serialize(record), record.Version);
                if (plan.Operation == ConditionalHolonOperation.HardDelete)
                    await DeleteCurrentHolonAsync(connection, transaction, stored.Id, token).ConfigureAwait(false);
                else
                    await WriteHolonSnapshotAsync(connection, transaction, stored, token).ConfigureAwait(false);
                records.Add(stored.Id, stored);
            }

            foreach (var edge in plan.ChildIds)
                records[edge.Key].Children = edge.Value.Select(id => records[id]).ToList();
            var delta = HolonCommittedDelta.Capture(plan.OperationId, plan.ActorId, plan.Authority, plan.Operation,
                now, previous.Values.OfType<Holon>(), records.Values, retainStoredValues: false);
            var json = ConditionalHolonReceipt.Write(plan.RootIds.Select(id => records[id]).ToArray(), plan, delta);
            var acknowledged = ConditionalHolonReceipt.Read(json, plan, out _);
            await using var insert = new SqlCommand(
                "INSERT dbo.OASISWriteReceipts (OperationId,RequestHash,ResultJson) VALUES (@Id,@Hash,@Json);",
                connection, transaction);
            insert.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = plan.OperationId;
            insert.Parameters.Add("@Hash", SqlDbType.VarChar, 64).Value = plan.Fingerprint;
            insert.Parameters.Add("@Json", SqlDbType.NVarChar, -1).Value = json;
            await ExecuteReplicaMutationAsync(insert, token).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();
            commitEntered = true;
            await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            return ConditionalHolonAcknowledgement.Create(acknowledged, plan.Operation);
        }
        catch (CosmicConcurrencyException cause) { return ReplicaFailure(CosmicConcurrencyException.Code, cause.Message, cause); }
        catch (StorageLifecycleConflictException cause) { return ReplicaFailure(StorageLifecycleConflictException.ErrorCode, cause.Message, cause); }
        catch (Exception cause)
        {
            if (!commitEntered)
                token.ThrowIfCancellationRequested();
            return ReplicaFailure(commitEntered ? "COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED" : "COSMIC_CONDITIONAL_WRITE_FAILED",
                "SQL Server conditional write failed.", commitEntered
                    ? new SqlServerReplicaReconciliationException(request.Write.OperationId, cause) : cause);
        }
    }
}
