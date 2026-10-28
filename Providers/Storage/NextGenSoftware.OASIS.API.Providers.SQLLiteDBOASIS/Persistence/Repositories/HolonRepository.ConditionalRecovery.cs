using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<ConditionalHolonWriteOutcome>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plan = original?.Write.ReplicationEnrollment == null
                ? ConditionalHolonPlan.Create(original, Core.Enums.ProviderType.SQLLiteDBOASIS)
                : ConditionalHolonPlan.CreateReplicating(original, Core.Enums.ProviderType.SQLLiteDBOASIS, original.Write.ReplicationEnrollment);
            var graph = PrepareGraph(original.Roots, original.SaveChildren, original.Recursive, original.MaxChildDepth);
            var fingerprint = ConditionalFingerprint(original, graph);
            if (original.Write.ReplicationEnrollment != null)
                fingerprint = ReplicationFingerprint(fingerprint, original.Write.ReplicationEnrollment);
            var source = _dbContext.Database.GetDbConnection();
            var settings = new SqliteConnectionStringBuilder(source.ConnectionString);
            // A separate read-only file connection cannot observe the writer
            // context's uncommitted receipt or create a missing database.
            if (settings.Mode == SqliteOpenMode.Memory || settings.DataSource == ":memory:")
                throw new InvalidOperationException("Durable operation recovery requires a file-backed SQLite authority.");
            settings.Mode = SqliteOpenMode.ReadOnly;
            settings.Pooling = false;
            using var connection = new SqliteConnection(settings.ToString());
            connection.Open();
            var expectedSource=original.Write.ReplicationEnrollment?.SourceStorageIdentity;
            using var bindingTransaction=expectedSource==null ? null:connection.BeginTransaction(deferred:true);
            if(expectedSource!=null && SqliteStorageIdentity.Read(connection,bindingTransaction).BindingFingerprint!=expectedSource.BindingFingerprint)
                throw new InvalidDataException("The native source does not match the original operation binding.");
            using var command = connection.CreateCommand();
            command.Transaction=bindingTransaction;
            command.CommandText = "SELECT RequestHash, ResultJson FROM CosmicWriteReceipt WHERE OperationId = $id LIMIT 2";
            command.Parameters.AddWithValue("$id", plan.OperationId.ToString("D"));
            using var reader = command.ExecuteReader();
            HolonCommittedDelta delta = null;
            var state = ConditionalHolonReceiptState.Absent;
            if (reader.Read())
            {
                if (reader.IsDBNull(0) || reader.GetString(0) != fingerprint || reader.IsDBNull(1))
                    throw new InvalidDataException("Receipt is not bound to the original conditional intent.");
                _ = ReadConditionalReceipt(reader.GetString(1), original, graph, fingerprint, out delta);
                if (reader.Read()) throw new InvalidDataException("Conditional receipt identity is not unique.");
                state = ConditionalHolonReceiptState.Committed;
            }
            cancellationToken.ThrowIfCancellationRequested();
            result.Result = new ConditionalHolonWriteOutcome(plan.OperationId, plan.ActorId, plan.Authority,
                plan.Operation, state, delta?.SummaryJson);
            result.IsLoaded = true;
        }
        catch (Exception cause)
        {
            result.IsError = true;
            result.ErrorCount = 1;
            result.ErrorCode = cause is OperationCanceledException ? "COSMIC_OPERATION_STATUS_CANCELLED" : "COSMIC_OPERATION_STATUS_UNVERIFIED";
            result.Message = "The original conditional operation receipt could not be verified.";
            result.Exception = cause;
        }
        return Task.FromResult(result);
    }
}
