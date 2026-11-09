using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public Task<OASISResult<CommittedHolonReplicaReceiptEvidence>> ReadCommittedHolonReplicaReceiptAsync(
        CommittedHolonReplicaReceiptReadRequest request, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != ProviderType.SQLLiteDBOASIS)
            return Task.FromResult(ReplicaReceiptError("COSMIC_REPLICA_RECEIPT_OPTIONS_INVALID", "An exact SQLite receipt intent and binding are required."));
        try
        {
            var plan = CommittedHolonReplicaReceiptProjection.Prepare(request);
            var settings = new SqliteConnectionStringBuilder(_dbContext.Database.GetDbConnection().ConnectionString);
            if (settings.Mode == SqliteOpenMode.Memory || settings.DataSource == ":memory:")
                throw new InvalidOperationException("Historical receipt reads require an existing durable receiver.");
            settings.Mode = SqliteOpenMode.ReadOnly; settings.Pooling = false;
            using var connection = new SqliteConnection(settings.ToString()); connection.Open();
            using var transaction = connection.BeginTransaction(deferred: true);
            if (SqliteStorageIdentity.Read(connection, transaction).BindingFingerprint != request.ExpectedStorage.BindingFingerprint)
                return Task.FromResult(ReplicaReceiptError("COSMIC_REPLICA_RECEIPT_STORAGE_MISMATCH", "The physical receiver differs from the reviewed binding."));
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "SELECT RequestHash,ResultJson FROM CosmicWriteReceipt WHERE OperationId=$id LIMIT 2";
            command.Parameters.AddWithValue("$id", plan.OperationId.ToString("D"));
            using var reader = command.ExecuteReader();
            string fingerprint = null, acknowledgement = null;
            if (reader.Read())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1)) throw new InvalidDataException("Historical receipt is incomplete.");
                fingerprint = reader.GetString(0); acknowledgement = reader.GetString(1);
                if (reader.Read()) throw new InvalidDataException("Historical receipt identity is not unique.");
            }
            var evidence = CommittedHolonReplicaReceiptProjection.Project(request, plan, fingerprint, acknowledgement,
                HolonReplicaHeadReadConsistency.NativeSnapshot);
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new OASISResult<CommittedHolonReplicaReceiptEvidence>(evidence) { IsLoaded = true });
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception cause)
        {
            return Task.FromResult(ReplicaReceiptError("COSMIC_REPLICA_RECEIPT_UNVERIFIED", "Historical delivery could not be verified; no mutation was attempted.", cause));
        }
    }
    private static OASISResult<CommittedHolonReplicaReceiptEvidence> ReplicaReceiptError(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
