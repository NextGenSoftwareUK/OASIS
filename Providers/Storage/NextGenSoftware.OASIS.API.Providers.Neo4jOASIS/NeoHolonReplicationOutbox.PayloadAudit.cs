using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public sealed partial class NeoHolonReplicationOutbox : IHolonReplicationPayloadAuditOutbox
{
    public Task<OASISResult<string>> ReadFrozenPayloadAsync(CosmicStorageIdentity expectedSource,
        HolonReplicationOperationObligation expected,
        HolonReplicaHeadReadConsistency requiredConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (expectedSource == null || expected == null || _expectedSource == null
            || expectedSource.BindingFingerprint != _expectedSource.BindingFingerprint
            || !Enum.IsDefined(typeof(HolonReplicaHeadReadConsistency), requiredConsistency))
            return Task.FromResult(new OASISResult<string> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_PAYLOAD_AUDIT_OPTIONS_INVALID", Message = "An exact bound native source and observation are required." });
        if (requiredConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot)
            return Task.FromResult(new OASISResult<string> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_PAYLOAD_AUDIT_CONSISTENCY_UNSUPPORTED", Message = "Neo frozen payload audit requires explicit read-committed opt-in." });
        return Execute(async tx =>
        {
            var rows = await (await tx.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) RETURN o LIMIT 2",
                new { sequence = expected.Sequence }).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            if (rows.Count != 1) throw new System.IO.InvalidDataException("The exact original obligation is absent or duplicated.");
            var row = rows[0]["o"].As<Neo4j.Driver.INode>();
            HolonReplicationPayloadReceiptVerifier.ValidateObservation(expected, ReadOperationObservation(row));
            return (new OASISResult<string>(Text(row, "PayloadJson")) { IsLoaded = true }, false);
        }, () => { }, cancellationToken);
    }
}
