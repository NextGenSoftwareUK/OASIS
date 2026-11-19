using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using INode = Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal static class NeoDestinationBindings
{
    private static string Key(CosmicStorageIdentity source, ProviderType target)
        => source.BindingFingerprint + ":" + target;

    internal static async Task<(HolonReplicationDestinationBinding Binding, bool Recorded)> ReadAsync(
        IAsyncTransaction tx, CosmicStorageIdentity source, ProviderType target)
    {
        var key = Key(source, target);
        var records = await (await tx.RunAsync(
            "MATCH (d:CosmicDestinationBinding {Key:$key}) RETURN d LIMIT 2", new { key }).ConfigureAwait(false))
            .ToListAsync().ConfigureAwait(false);
        if (records.Count > 1) throw new InvalidDataException("Duplicate native destination approvals.");
        if (records.Count == 1)
        {
            var node = records[0]["d"].As<INode>();
            var binding = HolonDestinationBindingRecord.Read(Text(node, "RecordJson"), Text(node, "RecordDigest"), source, target);
            if (Text(node, "EstablishedOperationId") != binding.EstablishedOperationId.ToString("D"))
                throw new InvalidDataException("Destination establishment identity disagrees with its protected record.");
            return (binding, true);
        }

        var history = await tx.RunAsync(
            "MATCH (o:CosmicHolonOutbox) WHERE o.SourceId=$sourceId AND o.TargetId STARTS WITH $prefix "
            + "RETURN o ORDER BY o.Sequence DESC LIMIT 257",
            new { sourceId = HolonReplicationRouteBinding.NativeSourceId(source), prefix = "hyperdrive-target-v2:" + target + ":" })
            .ConfigureAwait(false);
        var count = 0;
        long bytes = 0;
        while (await history.FetchAsync().ConfigureAwait(false))
        {
            var node = history.Current["o"].As<INode>();
            if (++count > 256 || (bytes += Encoding.UTF8.GetByteCount(Text(node, "PayloadJson"))) > 16L * 1024 * 1024)
                throw new InvalidDataException("Destination history exceeds the bootstrap budget; explicit migration is required.");
            if (!Guid.TryParseExact(Text(node, "OperationId"), "D", out var operation) || operation == Guid.Empty)
                throw new InvalidDataException("Invalid historical destination operation identity.");
            var route = NeoHolonReplicationOutbox.ValidatePayload(node, operation);
            if (route == null) continue;
            if (route.SourceStorage.BindingFingerprint != source.BindingFingerprint || route.TargetStorage.Authority != target)
                throw new InvalidDataException("Historical destination descriptor is outside the native source scope.");
            return (new HolonReplicationDestinationBinding(route, operation, operation), false);
        }
        return (null, false);
    }

    internal static async Task ApproveAsync(IAsyncTransaction tx, HolonReplicationEnrollment enrollment, Guid operation)
    {
        foreach (var proposed in enrollment.NativeRoutes)
        {
            var known = await ReadAsync(tx, proposed.SourceStorage, proposed.TargetStorage.Authority).ConfigureAwait(false);
            if (known.Binding != null && known.Binding.Route.TargetStorage.BindingFingerprint != proposed.TargetStorage.BindingFingerprint)
                throw new HolonDestinationRebindRequiredException();
            if (known.Recorded) continue;
            var binding = known.Binding ?? new HolonReplicationDestinationBinding(proposed, operation, operation);
            var json = HolonDestinationBindingRecord.Capture(binding);
            // Unique native Key prevents concurrent first writers from replacing
            // each other's physical destination. Conflicts abort the entire write.
            var written = await (await tx.RunAsync(
                "CREATE (d:CosmicDestinationBinding {Key:$key,RecordJson:$json,RecordDigest:$digest,EstablishedOperationId:$established}) "
                + "RETURN count(d) AS count",
                new { key = Key(proposed.SourceStorage, proposed.TargetStorage.Authority), json,
                    digest = HolonDestinationBindingRecord.Digest(json), established = binding.EstablishedOperationId.ToString("D") })
                .ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
            if (written["count"].As<long>() != 1) throw new InvalidDataException("Native destination approval was not recorded.");
        }
    }

    private static string Text(INode node, string property)
        => node.Properties.TryGetValue(property, out var value) && value is string text
            ? text : throw new InvalidDataException("Invalid destination identity field: " + property);
}
