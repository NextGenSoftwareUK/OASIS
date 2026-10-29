using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public sealed partial class NeoHolonReplicationOutbox
{
    public Task<OASISResult<HolonReplicationOperationPage>> ReadOperationObligationsAsync(
        HolonReplicationOperationReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || _expectedSource == null || request.ExpectedSource.BindingFingerprint != _expectedSource.BindingFingerprint)
            return Task.FromResult(new OASISResult<HolonReplicationOperationPage> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_INVENTORY_OPTIONS_INVALID", Message = "An exact explicitly bound source operation is required." });
        if (request.RequiredConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot)
            return Task.FromResult(new OASISResult<HolonReplicationOperationPage> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_INVENTORY_CONSISTENCY_UNSUPPORTED", Message = "Neo operation inventory requires explicit read-committed opt-in." });
        return Execute(async tx =>
        {
            // The pinned runtime's scalar string predicate disagrees with its
            // native range-index ordering for supplementary Unicode. A cursor
            // therefore uses the required operation/target index explicitly.
            // This is one native plan per declared input shape, never a retry.
            var query = request.AfterTargetId == null
                ? "MATCH (o:CosmicHolonOutbox {OperationId:$operation}) RETURN o ORDER BY o.TargetId LIMIT $limit"
                : "MATCH (o:CosmicHolonOutbox) USING INDEX o:CosmicHolonOutbox(OperationId,TargetId) "
                    + "WHERE o.OperationId=$operation AND o.TargetId>$after RETURN o ORDER BY o.TargetId LIMIT $limit";
            var records = await (await tx.RunAsync(query,
                new { operation = request.OperationId.ToString("D"), after = request.AfterTargetId, limit = request.PageSize + 1 })
                .ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            var rows = records.Select(record => ReadOperationObservation(record["o"].As<Neo4j.Driver.INode>())).ToArray();
            return (new OASISResult<HolonReplicationOperationPage>(new(request, rows.Take(request.PageSize),
                rows.Length > request.PageSize, HolonReplicaHeadReadConsistency.ReadCommittedValidated)) { IsLoaded = true }, false);
        }, () => { }, cancellationToken);
    }
    private static HolonReplicationOperationObligation ReadOperationObservation(Neo4j.Driver.INode row)
    {
        var state = ValidateRow(row); var binding = ValidatePayload(row, state.Operation);
        var inspection = new HolonDeliveryInspection(Long(row, "Sequence"), state.Operation, Text(row, "TargetId"),
            state.State, state.Attempts, Text(row, "LastErrorCode"), null, state.Evidence, binding);
        return HolonReplicationOperationObservation.Capture(inspection, Text(row, "SourceId"), ProviderType.Neo4jOASIS,
            Guid.Parse(Text(row, "ActorId")), Text(row, "PayloadDigest"), Text(row, "LeaseToken"), Text(row, "LeaseWorkerId"),
            Ticks(row, "LeaseUntilUtcTicks"), Ticks(row, "NextAttemptUtcTicks"), Ticks(row, "CompletedAtUtcTicks"), Text(row, "RecoveryEvidenceDigest"));
    }

}
