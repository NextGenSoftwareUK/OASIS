using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public sealed partial class MongoHolonReplicationOutbox : IHolonReplicationPayloadAuditOutbox
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
        return Observe(async session =>
        {
            var rows = await _rows.Find(session, new BsonDocument("Sequence", expected.Sequence)).Limit(2)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            if (rows.Count != 1) throw new System.IO.InvalidDataException("The exact original obligation is absent or duplicated.");
            HolonReplicationPayloadReceiptVerifier.ValidateObservation(expected, ReadOperationObservation(rows[0]));
            return new OASISResult<string>(rows[0]["PayloadJson"].AsString) { IsLoaded = true };
        }, cancellationToken);
    }
}
