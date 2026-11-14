using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS : IConditionalHolonReceiptEvidenceStorageProvider, ICommittedHolonReplicaReceiptStorageProvider
{
    public async Task<OASISResult<CommittedHolonReplicaReceiptEvidence>> ReadCommittedHolonReplicaReceiptAsync(
        CommittedHolonReplicaReceiptReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || request.ExpectedStorage.Authority != Core.Enums.ProviderType.OracleDBOASIS)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_OPTIONS_INVALID",
                Message = "An exact Oracle replica receipt intent and binding are required." };

        try
        {
            if (!IsProviderActivated)
                throw new InvalidOperationException("Activate the enrolled Oracle store before reading replica receipts.");

            var plan = CommittedHolonReplicaReceiptProjection.Prepare(request);
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
            var identity = await ReadOracleStorageIdentityAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
            if (identity == null || identity.BindingFingerprint != request.ExpectedStorage.BindingFingerprint)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_STORAGE_MISMATCH",
                    Message = "The physical Oracle schema differs from the reviewed replica store." };

            await using var command = CreateCommand(
                "SELECT REQUEST_HASH, RESULT_JSON FROM OASIS_REPLICA_RECEIPTS WHERE OPERATION_ID=:Id", connection);
            command.Transaction = transaction;
            command.Parameters.Add("Id", OracleDbType.Raw).Value = plan.OperationId.ToByteArray();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var found = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var evidence = CommittedHolonReplicaReceiptProjection.Project(request, plan,
                found ? reader.GetString(0) : null, found ? reader.GetString(1) : null,
                HolonReplicaHeadReadConsistency.NativeSnapshot);
            if (found && await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidDataException("The Oracle replica receipt has duplicate operation identities.");
            return new(evidence) { IsLoaded = true };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OracleException cause) when (cause.Number == 1013 && cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Oracle cancelled the replica receipt read.", cause, cancellationToken);
        }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_REPLICA_RECEIPT_UNVERIFIED",
                Message = "The Oracle replica receipt could not be verified.", Exception = cause };
        }
    }

    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonReceiptEvidenceAsync(
        ConditionalHolonReceiptEvidenceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ReadOracleConditionalOutcomeAsync(request.Original, request.ExpectedSource, request, cancellationToken);
    }

    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadBoundConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CosmicStorageIdentity expectedSource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expectedSource);
        return ReadOracleConditionalOutcomeAsync(original, expectedSource, null, cancellationToken);
    }

    public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CancellationToken cancellationToken = default)
        => ReadOracleConditionalOutcomeAsync(original, original?.Write.ReplicationEnrollment?.SourceStorageIdentity,
            null, cancellationToken);

    private async Task<OASISResult<ConditionalHolonWriteOutcome>> ReadOracleConditionalOutcomeAsync(
        ConditionalHolonWriteRequest original, CosmicStorageIdentity? expectedSource,
        ConditionalHolonReceiptEvidenceRequest? evidenceRequest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            ArgumentNullException.ThrowIfNull(original);
            var authority = Core.Enums.ProviderType.OracleDBOASIS;
            var enrollment = original.Write.ReplicationEnrollment;
            var plan = enrollment == null ? ConditionalHolonPlan.Create(original, authority)
                : ConditionalHolonPlan.CreateReplicating(original, authority, enrollment);
            if (!IsProviderActivated)
                throw new InvalidOperationException("Activate the enrolled Oracle store before reading its receipt.");

            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
            if (expectedSource != null)
            {
                var identity = await ReadOracleStorageIdentityAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
                if (identity == null || identity.BindingFingerprint != expectedSource.BindingFingerprint
                    || (enrollment?.SourceStorageIdentity is { } source && source.BindingFingerprint != identity.BindingFingerprint))
                    return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_SOURCE_STORAGE_BINDING_MISMATCH",
                        Message = "The physical Oracle store differs from the requested source." };
            }

            await using var command = CreateCommand(
                "SELECT REQUEST_HASH, RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id", connection);
            command.Transaction = transaction;
            command.Parameters.Add("Id", OracleDbType.Raw).Value = plan.OperationId.ToByteArray();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var state = ConditionalHolonReceiptState.Absent;
            ConditionalHolonReceiptEvidence? evidence = null;
            HolonCommittedDelta? delta = null;
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (reader.GetString(0) != plan.Fingerprint || reader.IsDBNull(1))
                    throw new InvalidDataException("The Oracle receipt differs from the original conditional write.");
                var roots = ReadConditionalReceipt(reader.GetString(1), plan, out delta);
                if (evidenceRequest != null)
                    evidence = ConditionalHolonReceiptEvidenceProjection.Create(evidenceRequest, roots);
                state = ConditionalHolonReceiptState.Committed;
            }

            return new(new ConditionalHolonWriteOutcome(plan.OperationId, plan.ActorId, authority,
                plan.Operation, state, delta?.SummaryJson, evidence)) { IsLoaded = true };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OracleException cause) when (cause.Number == 1013 && cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("Oracle cancelled the requested storage operation.", cause, cancellationToken);
        }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_CONDITIONAL_RECEIPT_READ_FAILED",
                Message = "The Oracle conditional receipt could not be verified.", Exception = cause };
        }
    }
}
