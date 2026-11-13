using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public sealed partial class MongoHolonReplicationOutbox
{
    public Task<OASISResult<HolonReplicationLineagePage>> ReadLineageObligationsAsync(
        HolonReplicationLineageReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || _expectedSource == null || request.ExpectedSource.BindingFingerprint != _expectedSource.BindingFingerprint)
            return Task.FromResult(new OASISResult<HolonReplicationLineagePage> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_LINEAGE_INVENTORY_OPTIONS_INVALID", Message = "An exact explicitly bound source lineage is required." });
        return Observe(async session =>
        {
            var through = request.ThroughSequence;
            if (!through.HasValue)
            {
                var boundary = await _rows.Find(session, new BsonDocument("SourceId", request.SourceId))
                    .Sort(new BsonDocument("Sequence", -1)).Project<BsonDocument>(new BsonDocument("Sequence", 1))
                    .Limit(1).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                through = boundary == null ? 0L : boundary["Sequence"].AsInt64;
            }
            var filter = new BsonDocument { { "SourceId", request.SourceId },
                { "Sequence", new BsonDocument { { "$gt", request.AfterSequence }, { "$lte", through.Value } } } };
            var rows = new List<HolonReplicationOperationObligation>();
            using var cursor = await _rows.FindAsync(session, filter, new FindOptions<BsonDocument> {
                Sort = new BsonDocument("Sequence", 1), Limit = request.PageSize + 1, BatchSize = 1
            }, cancellationToken).ConfigureAwait(false);
            while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            foreach (var row in cursor.Current) { cancellationToken.ThrowIfCancellationRequested(); rows.Add(ReadOperationObservation(row)); }
            return new OASISResult<HolonReplicationLineagePage>(new(request, through.Value, rows.Take(request.PageSize),
                rows.Count > request.PageSize, HolonReplicaHeadReadConsistency.NativeSnapshot)) { IsLoaded = true };
        }, cancellationToken);
    }
}
