using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS : IBoundCommittedHolonReplicaStorageProvider
{
    private const Core.Enums.ProviderType ReplicaAuthority = Core.Enums.ProviderType.OracleDBOASIS;

    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        => ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
        => ApplyOracleReplicaAsync(request, null, cancellationToken);

    public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage)
        => ApplyBoundCommittedHolonReplicaAsync(request, expectedStorage).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedStorage);
        return ApplyOracleReplicaAsync(request, expectedStorage, cancellationToken);
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> ApplyOracleReplicaAsync(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity? expectedStorage, CancellationToken token)
    {
        var commitEntered = false;
        try
        {
            token.ThrowIfCancellationRequested();
            CommittedHolonReplicaPlan.ValidateNativeDestination(request, expectedStorage);
            var plan = CommittedHolonReplicaPlan.Create(request, ReplicaAuthority);
            if (!IsProviderActivated)
                throw new InvalidOperationException("Activate the enrolled Oracle store before applying a replica.");

            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);
            await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token).ConfigureAwait(false);
            var identity = await ReadOracleStorageIdentityAsync(connection, transaction, token).ConfigureAwait(false);
            if (identity == null || (expectedStorage != null && identity.BindingFingerprint != expectedStorage.BindingFingerprint))
                return ConditionalFailure("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", "The Oracle schema differs from the enrolled replica store.");

            await AcquireWriteOperationLockAsync(connection, transaction, plan.OperationId, token).ConfigureAwait(false);
            await using (var receipt = CreateCommand("SELECT REQUEST_HASH, RESULT_JSON FROM OASIS_REPLICA_RECEIPTS WHERE OPERATION_ID=:Id", connection))
            {
                receipt.Transaction = transaction;
                receipt.Parameters.Add("Id", OracleDbType.Raw).Value = plan.OperationId.ToByteArray();
                await using var reader = await receipt.ExecuteReaderAsync(token).ConfigureAwait(false);
                if (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    if (reader.GetString(0) != plan.Fingerprint)
                        return ConditionalFailure("COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID", "The Oracle replica receipt belongs to another request.");
                    return ConditionalAcknowledgement(ConditionalHolonReceipt.Read(reader.GetString(1), plan), plan.Operation);
                }
            }

            var records = new Dictionary<Guid, IHolon>();
            foreach (var node in plan.Nodes.OrderBy(node => node.Snapshot.Id))
            {
                var record = node.Snapshot;
                string? original = null;
                Holon? current = null;
                await using (var read = CreateCommand("SELECT DATA_JSON,HOLON_TYPE,IS_DELETED,PARENT_HOLON_ID FROM OASIS_HOLONS WHERE ID=:Id FOR UPDATE", connection))
                {
                    read.Transaction = transaction;
                    read.Parameters.Add("Id", OracleDbType.Raw).Value = record.Id.ToByteArray();
                    await using var reader = await read.ExecuteReaderAsync(token).ConfigureAwait(false);
                    if (await reader.ReadAsync(token).ConfigureAwait(false))
                    {
                        original = reader.GetString(0);
                        current = Deserialize<Holon>(original);
                        var parent = reader.IsDBNull(3) ? Guid.Empty : new Guid((byte[])reader.GetValue(3));
                        ValidateNativeHolonProjection(current, reader.GetInt32(1), reader.GetInt32(2), parent);
                    }
                }

                var hasHead = false;
                await using (var head = CreateCommand("SELECT SOURCE_ID,SOURCE_AUTHORITY,SNAPSHOT_JSON,SNAPSHOT_DIGEST,REVISION_ID,HARD_DELETED FROM OASIS_REPLICA_HEADS WHERE ID=:Id FOR UPDATE", connection))
                {
                    head.Transaction = transaction;
                    head.Parameters.Add("Id", OracleDbType.Raw).Value = record.Id.ToByteArray();
                    await using var reader = await head.ExecuteReaderAsync(token).ConfigureAwait(false);
                    hasHead = await reader.ReadAsync(token).ConfigureAwait(false);
                    if (hasHead)
                    {
                        var evidence = HolonReplicaHeadRecord.Read(record.Id, ReplicaAuthority, reader.GetString(0),
                            reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetInt32(5) == 1).Evidence;
                        HolonReplicaHeadRecord.ValidateCurrent(record.Id, ReplicaAuthority, current, evidence);
                        if (evidence.SourceId != plan.SourceId || evidence.SourceAuthority != plan.SourceAuthority
                            || evidence.VersionId != record.PreviousVersionId || evidence.Version != record.Version - 1)
                            return OracleReplicaLineageConflict();
                    }
                    else if (current != null || record.Version != 1)
                        return OracleReplicaLineageConflict();
                }

                record.ProviderUniqueStorageKey ??= new();
                record.ProviderUniqueStorageKey[ReplicaAuthority] = record.Id.ToString("D");
                record.PreviousVersionProviderUniqueStorageKey?.Remove(ReplicaAuthority);
                record.ProviderMetaData?.Remove(ReplicaAuthority);
                record.InstanceSavedOnProviderType = new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(ReplicaAuthority);
                var json = await StageRecordHistoryAsync(record, original).ConfigureAwait(false);
                var staged = Deserialize<Holon>(json);
                plan.ValidateAcknowledgedRecord(staged);
                var hardDelete = plan.Operation == ConditionalHolonOperation.HardDelete;
                await using (var write = hardDelete
                    ? CreateCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id", connection)
                    : CreateCanonicalWriteCommand(staged, json, "OASIS_HOLONS", current == null, connection, transaction))
                {
                    write.Transaction = transaction;
                    if (hardDelete)
                        write.Parameters.Add("Id", OracleDbType.Raw).Value = staged.Id.ToByteArray();
                    if (await write.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1)
                        throw new InvalidDataException("Oracle replica mutation did not affect exactly one row.");
                }

                await using var saveHead = CreateCommand(hasHead
                    ? "UPDATE OASIS_REPLICA_HEADS SET SOURCE_ID=:SourceId,SOURCE_AUTHORITY=:Authority,SNAPSHOT_JSON=:Json,SNAPSHOT_DIGEST=:Digest,REVISION_ID=:Revision,HARD_DELETED=:Deleted WHERE ID=:Id"
                    : "INSERT INTO OASIS_REPLICA_HEADS (ID,SOURCE_ID,SOURCE_AUTHORITY,SNAPSHOT_JSON,SNAPSHOT_DIGEST,REVISION_ID,HARD_DELETED) VALUES (:Id,:SourceId,:Authority,:Json,:Digest,:Revision,:Deleted)", connection);
                saveHead.Transaction = transaction;
                var snapshot = Core.Serialization.StoredRecordJson.Serialize<IHolon>(staged);
                saveHead.Parameters.Add("Id", OracleDbType.Raw).Value = staged.Id.ToByteArray();
                saveHead.Parameters.Add("SourceId", OracleDbType.Varchar2).Value = plan.SourceId;
                saveHead.Parameters.Add("Authority", OracleDbType.Varchar2).Value = plan.SourceAuthority.ToString();
                saveHead.Parameters.Add("Json", OracleDbType.Clob).Value = snapshot;
                saveHead.Parameters.Add("Digest", OracleDbType.Varchar2).Value = HolonRecoveryEvidenceJson.Digest(snapshot);
                saveHead.Parameters.Add("Revision", OracleDbType.Varchar2).Value = CosmicConcurrency.GetProviderRevisionId(staged, ReplicaAuthority)!.Value.ToString("D");
                saveHead.Parameters.Add("Deleted", OracleDbType.Int32).Value = hardDelete ? 1 : 0;
                if (await saveHead.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1)
                    throw new InvalidDataException("Oracle replica head mutation did not affect exactly one row.");
                records.Add(staged.Id, staged);
            }

            foreach (var edge in plan.ChildIds)
                records[edge.Key].Children = edge.Value.Select(id => records[id]).ToList();
            var receiptJson = ConditionalHolonReceipt.Write(plan.RootIds.Select(id => records[id]).ToArray(), plan);
            var acknowledged = ConditionalHolonReceipt.Read(receiptJson, plan);
            await using (var receipt = CreateCommand("INSERT INTO OASIS_REPLICA_RECEIPTS (OPERATION_ID,REQUEST_HASH,RESULT_JSON) VALUES (:Id,:Hash,:Json)", connection))
            {
                receipt.Transaction = transaction;
                receipt.Parameters.Add("Id", OracleDbType.Raw).Value = plan.OperationId.ToByteArray();
                receipt.Parameters.Add("Hash", OracleDbType.Varchar2).Value = plan.Fingerprint;
                receipt.Parameters.Add("Json", OracleDbType.Clob).Value = receiptJson;
                if (await receipt.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1)
                    throw new InvalidDataException("Oracle did not persist exactly one replica receipt.");
            }
            await using (var release = CreateCommand("DELETE FROM OASIS_WRITE_OPERATION_LOCKS WHERE OPERATION_ID=:Id", connection))
            {
                release.Transaction = transaction;
                release.Parameters.Add("Id", OracleDbType.Raw).Value = plan.OperationId.ToByteArray();
                if (await release.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1)
                    throw new InvalidDataException("Oracle replica operation guard changed before commit.");
            }
            token.ThrowIfCancellationRequested();
            commitEntered = true;
            await CommitLifecycleTransactionAsync(transaction).ConfigureAwait(false);
            return ConditionalAcknowledgement(acknowledged, plan.Operation);
        }
        catch (OperationCanceledException) when (!commitEntered && token.IsCancellationRequested) { throw; }
        catch (OracleException cause) when (!commitEntered && cause.Number == 1013 && token.IsCancellationRequested)
        {
            throw new OperationCanceledException("Oracle cancelled replica apply.", cause, token);
        }
        catch (Exception cause)
        {
            return ConditionalFailure(commitEntered ? "COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED" : "COSMIC_REPLICA_WRITE_FAILED",
                "Oracle replica apply failed.", commitEntered ? new OracleConditionalReconciliationException(request.OperationId, cause) : cause);
        }
    }

    private static OASISResult<IEnumerable<IHolon>> OracleReplicaLineageConflict()
        => ConditionalFailure("COSMIC_REPLICA_LINEAGE_CONFLICT", "The Oracle replica lineage does not match the committed source.");
}
