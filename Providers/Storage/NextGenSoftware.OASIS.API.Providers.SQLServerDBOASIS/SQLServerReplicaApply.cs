using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;

public partial class SQLServerDBOASIS : IBoundCommittedHolonReplicaStorageProvider
{
    private const Core.Enums.ProviderType ReplicaAuthority = Core.Enums.ProviderType.SQLServerDBOASIS;

    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        => ApplyCommittedHolonReplicaAsync(request).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
        => ApplySqlServerReplicaAsync(request, null, cancellationToken);

    public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage)
        => ApplyBoundCommittedHolonReplicaAsync(request, expectedStorage).GetAwaiter().GetResult();

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
        => ApplySqlServerReplicaAsync(request, expectedStorage ?? throw new ArgumentNullException(nameof(expectedStorage)),
            cancellationToken);

    private async Task<OASISResult<IEnumerable<IHolon>>> ApplySqlServerReplicaAsync(
        CommittedHolonReplicaRequest request, CosmicStorageIdentity? expectedStorage, CancellationToken token)
    {
        var commitEntered = false;
        try
        {
            token.ThrowIfCancellationRequested();
            CommittedHolonReplicaPlan.ValidateNativeDestination(request, expectedStorage);
            var plan = CommittedHolonReplicaPlan.Create(request, ReplicaAuthority);
            if (!IsProviderActivated)
                throw new InvalidOperationException("Activate the enrolled SQL Server database before applying a replica.");

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, token)
                .ConfigureAwait(false);
            var identity = await ReadSqlServerStorageIdentityAsync(connection, transaction, token).ConfigureAwait(false);
            if (expectedStorage != null && identity.BindingFingerprint != expectedStorage.BindingFingerprint)
                return ReplicaFailure("COSMIC_REPLICA_STORAGE_BINDING_MISMATCH", "The SQL Server database differs from the enrolled replica store.");

            await using (var receipt = new SqlCommand(
                "SELECT RequestHash, ResultJson FROM dbo.OASISReplicaReceipts WITH (UPDLOCK,HOLDLOCK) WHERE OperationId=@Id;",
                connection, transaction))
            {
                receipt.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = plan.OperationId;
                await using var reader = await receipt.ExecuteReaderAsync(token).ConfigureAwait(false);
                if (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    if (reader.GetString(0) != plan.Fingerprint)
                        return ReplicaFailure("COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID", "The SQL Server receipt belongs to another request.");
                    return ConditionalHolonAcknowledgement.Create(ConditionalHolonReceipt.Read(reader.GetString(1), plan), plan.Operation);
                }
            }

            var records = new Dictionary<Guid, IHolon>();
            foreach (var node in plan.Nodes.OrderBy(node => node.Snapshot.Id))
            {
                var record = node.Snapshot;
                var current = await ReadLockedReplicaHolonAsync(connection, transaction, record.Id, token).ConfigureAwait(false);
                var hasHead = false;
                await using (var head = new SqlCommand(@"
SELECT SourceId, SourceAuthority, SnapshotJson, SnapshotDigest, RevisionId, HardDeleted
FROM dbo.OASISReplicaHeads WITH (UPDLOCK,HOLDLOCK) WHERE Id=@Id;", connection, transaction))
                {
                    head.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = record.Id;
                    await using var reader = await head.ExecuteReaderAsync(token).ConfigureAwait(false);
                    hasHead = await reader.ReadAsync(token).ConfigureAwait(false);
                    if (hasHead)
                    {
                        var evidence = HolonReplicaHeadRecord.Read(record.Id, ReplicaAuthority, reader.GetString(0),
                            reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetBoolean(5)).Evidence;
                        HolonReplicaHeadRecord.ValidateCurrent(record.Id, ReplicaAuthority, current, evidence);
                        if (evidence.SourceId != plan.SourceId || evidence.SourceAuthority != plan.SourceAuthority
                            || evidence.VersionId != record.PreviousVersionId || evidence.Version != record.Version - 1)
                            return ReplicaFailure("COSMIC_REPLICA_LINEAGE_CONFLICT", "The SQL Server replica lineage differs from the committed source.");
                    }
                    else if (current != null || record.Version != 1)
                        return ReplicaFailure("COSMIC_REPLICA_LINEAGE_CONFLICT", "The SQL Server replica requires an initial source revision.");
                }

                record.ProviderUniqueStorageKey ??= new();
                record.ProviderUniqueStorageKey[ReplicaAuthority] = record.Id.ToString("D");
                record.PreviousVersionProviderUniqueStorageKey?.Remove(ReplicaAuthority);
                record.ProviderMetaData?.Remove(ReplicaAuthority);
                CosmicConcurrency.SetProviderRevisionId(record, ReplicaAuthority, Guid.NewGuid());
                record.InstanceSavedOnProviderType = new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(ReplicaAuthority);
                var stored = ReadHolonSnapshot(record.Id, Serialize(record), record.Version);
                plan.ValidateAcknowledgedRecord(stored);
                var hardDelete = plan.Operation == ConditionalHolonOperation.HardDelete;
                if (hardDelete)
                {
                    await using var delete = new SqlCommand(
                        "DELETE FROM OASISHolons WHERE Id=@Id; SELECT @@ROWCOUNT;", connection, transaction);
                    delete.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = stored.Id;
                    if (await delete.ExecuteScalarAsync(token).ConfigureAwait(false) is not 1)
                        throw new InvalidDataException("SQL Server did not delete exactly one current replica row.");
                }
                else
                    await WriteHolonSnapshotAsync(connection, transaction, stored, token).ConfigureAwait(false);

                var snapshot = StoredRecordJson.Serialize<IHolon>(stored);
                await using var saveHead = new SqlCommand(hasHead
                    ? @"UPDATE dbo.OASISReplicaHeads SET SourceId=@Source, SourceAuthority=@Authority, SnapshotJson=@Json,
SnapshotDigest=@Digest, RevisionId=@Revision, HardDeleted=@Deleted WHERE Id=@Id;"
                    : @"INSERT dbo.OASISReplicaHeads (Id,SourceId,SourceAuthority,SnapshotJson,SnapshotDigest,RevisionId,HardDeleted)
VALUES (@Id,@Source,@Authority,@Json,@Digest,@Revision,@Deleted);", connection, transaction);
                saveHead.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = stored.Id;
                saveHead.Parameters.Add("@Source", SqlDbType.NVarChar, 256).Value = plan.SourceId;
                saveHead.Parameters.Add("@Authority", SqlDbType.VarChar, 64).Value = plan.SourceAuthority.ToString();
                saveHead.Parameters.Add("@Json", SqlDbType.NVarChar, -1).Value = snapshot;
                saveHead.Parameters.Add("@Digest", SqlDbType.VarChar, 64).Value = HolonRecoveryEvidenceJson.Digest(snapshot);
                saveHead.Parameters.Add("@Revision", SqlDbType.VarChar, 36).Value =
                    CosmicConcurrency.GetProviderRevisionId(stored, ReplicaAuthority)!.Value.ToString("D");
                saveHead.Parameters.Add("@Deleted", SqlDbType.Bit).Value = hardDelete;
                await ExecuteReplicaMutationAsync(saveHead, token).ConfigureAwait(false);
                records.Add(stored.Id, stored);
            }

            foreach (var edge in plan.ChildIds)
                records[edge.Key].Children = edge.Value.Select(id => records[id]).ToList();
            var json = ConditionalHolonReceipt.Write(plan.RootIds.Select(id => records[id]).ToArray(), plan);
            var acknowledged = ConditionalHolonReceipt.Read(json, plan);
            await using (var receipt = new SqlCommand(
                "INSERT dbo.OASISReplicaReceipts (OperationId,RequestHash,ResultJson) VALUES (@Id,@Hash,@Json);", connection, transaction))
            {
                receipt.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = plan.OperationId;
                receipt.Parameters.Add("@Hash", SqlDbType.VarChar, 64).Value = plan.Fingerprint;
                receipt.Parameters.Add("@Json", SqlDbType.NVarChar, -1).Value = json;
                await ExecuteReplicaMutationAsync(receipt, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            commitEntered = true;
            await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
            return ConditionalHolonAcknowledgement.Create(acknowledged, plan.Operation);
        }
        catch (Exception cause)
        {
            if (!commitEntered)
                token.ThrowIfCancellationRequested();
            return ReplicaFailure(commitEntered ? "COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED" : "COSMIC_REPLICA_WRITE_FAILED",
                "SQL Server replica apply failed.", commitEntered
                    ? new SqlServerReplicaReconciliationException(request.OperationId, cause) : cause);
        }
    }

    private static async Task<Holon?> ReadLockedReplicaHolonAsync(SqlConnection connection, SqlTransaction transaction,
        Guid id, CancellationToken token)
    {
        await using var read = new SqlCommand(
            "SELECT DataJson,ParentHolonId,HolonType,IsDeleted FROM OASISHolons WITH (UPDLOCK,HOLDLOCK) WHERE Id=@Id;",
            connection, transaction);
        read.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
        await using var reader = await read.ExecuteReaderAsync(token).ConfigureAwait(false);
        if (!await reader.ReadAsync(token).ConfigureAwait(false))
            return null;
        var holon = ReadHolonSnapshot(id, reader.GetString(0), 0);
        var parent = reader.IsDBNull(1) ? Guid.Empty : reader.GetGuid(1);
        if (holon.ParentHolonId != parent || (int)holon.HolonType != reader.GetInt32(2) || holon.IsDeleted != reader.GetBoolean(3))
            throw new InvalidDataException("The SQL Server holon projection differs from its stored snapshot.");
        return holon;
    }

    private static async Task ExecuteReplicaMutationAsync(SqlCommand command, CancellationToken token)
    {
        if (await command.ExecuteNonQueryAsync(token).ConfigureAwait(false) != 1)
            throw new InvalidDataException("The SQL Server replica mutation did not affect exactly one row.");
    }

    private static OASISResult<IEnumerable<IHolon>> ReplicaFailure(string code, string message, Exception? cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

    private sealed class SqlServerReplicaReconciliationException(Guid operationId, Exception cause)
        : Exception("SQL Server replica commit requires reconciliation against the original receipt.", cause),
            INativeMutationReconciliationRequired
    {
        public Guid OperationId { get; } = operationId;
        public string OriginalTransactionId => string.Empty;
    }
}
