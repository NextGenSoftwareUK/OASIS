using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public sealed partial class NeoHolonReplicationOutbox
{
    public Task<OASISResult<HolonReplicationLineagePage>> ReadLineageObligationsAsync(
        HolonReplicationLineageReadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request == null || _expectedSource == null || request.ExpectedSource.BindingFingerprint != _expectedSource.BindingFingerprint)
            return Task.FromResult(new OASISResult<HolonReplicationLineagePage> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_LINEAGE_INVENTORY_OPTIONS_INVALID", Message = "An exact explicitly bound source lineage is required." });
        if (request.RequiredConsistency == HolonReplicaHeadReadConsistency.NativeSnapshot)
            return Task.FromResult(new OASISResult<HolonReplicationLineagePage> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_LINEAGE_INVENTORY_CONSISTENCY_UNSUPPORTED", Message = "Neo lineage inventory requires explicit read-committed opt-in." });
        return Execute(async tx =>
        {
            var through = request.ThroughSequence;
            if (!through.HasValue)
            {
                var boundary = await (await tx.RunAsync("MATCH (o:CosmicHolonOutbox {SourceId:$source}) RETURN coalesce(max(o.Sequence),0) AS upper",
                    new { source = request.SourceId }).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
                through = boundary["upper"].As<long>();
            }
            var records = await (await tx.RunAsync("MATCH (o:CosmicHolonOutbox) USING INDEX o:CosmicHolonOutbox(Sequence) "
                + "WHERE o.Sequence>$after AND o.Sequence<=$through AND o.SourceId=$source RETURN o ORDER BY o.Sequence LIMIT $limit",
                new { source = request.SourceId, after = request.AfterSequence, through = through.Value, limit = request.PageSize + 1 })
                .ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
            var rows = records.Select(record => ReadOperationObservation(record["o"].As<Neo4j.Driver.INode>())).ToArray();
            return (new OASISResult<HolonReplicationLineagePage>(new(request, through.Value, rows.Take(request.PageSize),
                rows.Length > request.PageSize, HolonReplicaHeadReadConsistency.ReadCommittedValidated)) { IsLoaded = true }, false);
        }, () => { }, cancellationToken);
    }
}
