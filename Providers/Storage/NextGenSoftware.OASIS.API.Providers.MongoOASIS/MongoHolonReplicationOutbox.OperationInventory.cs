using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public sealed partial class MongoHolonReplicationOutbox
{
    public Task<OASISResult<HolonReplicationOperationPage>> ReadOperationObligationsAsync(
        HolonReplicationOperationReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || _expectedSource == null || request.ExpectedSource.BindingFingerprint != _expectedSource.BindingFingerprint)
            return Task.FromResult(new OASISResult<HolonReplicationOperationPage> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OPERATION_INVENTORY_OPTIONS_INVALID", Message = "An exact explicitly bound source operation is required." });
        return Observe(async session =>
        {
            var filter = new BsonDocument("OperationId", request.OperationId.ToString("D"));
            if (request.AfterTargetId != null) filter.Add("TargetId", new BsonDocument("$gt", request.AfterTargetId));
            var rows = new List<HolonReplicationOperationObligation>();
            using var cursor = await _rows.FindAsync(session, filter, new FindOptions<BsonDocument> {
                Sort = new BsonDocument("TargetId", 1), Collation = Collation.Simple, Limit = request.PageSize + 1, BatchSize = 1
            }, cancellationToken).ConfigureAwait(false);
            while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            foreach (var row in cursor.Current)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var state = ValidateRow(row); var binding = ValidatePayload(row, state.Operation);
                var inspection = new HolonDeliveryInspection(row["Sequence"].AsInt64, state.Operation, row["TargetId"].AsString,
                    state.State, state.Attempts, NullableString(row, "LastErrorCode"), null, state.Evidence, binding);
                rows.Add(HolonReplicationOperationObservation.Capture(inspection, row["SourceId"].AsString, ProviderType.MongoDBOASIS,
                    Guid.Parse(row["ActorId"].AsString), row["PayloadDigest"].AsString, NullableString(row, "LeaseToken"),
                    NullableString(row, "LeaseWorkerId"), Ticks(row, "LeaseUntilUtcTicks"), Ticks(row, "NextAttemptUtcTicks"),
                    Ticks(row, "CompletedAtUtcTicks"), NullableString(row, "RecoveryEvidenceDigest")));
            }
            return new OASISResult<HolonReplicationOperationPage>(new(request, rows.Take(request.PageSize),
                rows.Count > request.PageSize, HolonReplicaHeadReadConsistency.NativeSnapshot)) { IsLoaded = true };
        }, cancellationToken);
    }
}
