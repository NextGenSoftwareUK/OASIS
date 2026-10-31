using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    // Each committed batch is independently restartable. Selection does not capture
    // a before-image: snapshots are read only after the native holon lock is held.
    internal async Task UpgradeCqlProjectionsAsync(CancellationToken cancellationToken = default)
    {
        using var session = _driver.AsyncSession();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var transaction = await session.BeginTransactionAsync(config => config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            try
            {
                var selected = await (await transaction.RunAsync(
                    "MATCH (h:Holon) OPTIONAL MATCH (h)-[p:HAS_CQL_PROJECTION]->(h) "
                    + "WITH h,collect(p) AS projections WHERE size(projections)<>1 "
                    + "OR any(p IN projections WHERE coalesce(p.SchemaVersion,0)<>$schema "
                    + "OR coalesce(p.ProviderRevisionId,'')<>coalesce(h.ProviderRevisionId,'')) "
                    + "WITH h ORDER BY h.Id LIMIT 100 "
                    + "SET h.__oasisHolonLock=true REMOVE h.__oasisHolonLock "
                    + "WITH h OPTIONAL MATCH (h)-[current:HAS_CQL_PROJECTION]->(h) "
                    + "RETURN h,collect(current) AS projections",
                    new { schema = NeoCqlProjection.SchemaVersion }).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
                if (selected.Count == 0)
                {
                    await transaction.CommitAsync().ConfigureAwait(false);
                    return;
                }
                var rows = new List<Dictionary<string, object>>();
                foreach (var item in selected)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var node = item["h"].As<INode>();
                    var existing = item["projections"].As<List<IRelationship>>();
                    if (existing.Count > 1)
                        throw new InvalidDataException("Neo4j holon has multiple CQL projection owners.");
                    if (existing.Count == 1 && existing[0].Properties.TryGetValue("SchemaVersion", out var storedSchema))
                    {
                        if (storedSchema is not long schema || schema < 0)
                            throw new InvalidDataException("Neo4j CQL projection schema version is corrupt.");
                        if (schema > NeoCqlProjection.SchemaVersion)
                            throw new InvalidDataException("Neo4j CQL projection was written by a newer schema; downgrade is not supported.");
                    }
                    var record = ReadHolonNode(node); // Validate canonical identity and physical lookup/revision fields.
                    var projection = NeoCqlProjection.Build((string)node["SnapshotJson"]);
                    projection["ProviderRevisionId"] = node.Properties.TryGetValue("ProviderRevisionId", out var revision) ? revision : "";
                    rows.Add(new() { ["id"] = record.Id.ToString(), ["projection"] = projection });
                }
                var changed = await (await transaction.RunAsync(
                    "UNWIND $rows AS row MATCH (h:Holon {Id:row.id}) "
                    + "MERGE (h)-[p:HAS_CQL_PROJECTION]->(h) "
                    + "SET p=row.projection RETURN count(h) AS count", new { rows }).ConfigureAwait(false)).SingleAsync().ConfigureAwait(false);
                if (changed["count"].As<long>() != rows.Count)
                    throw new InvalidDataException("Neo4j CQL projection ownership or upgrade acknowledgement is inconsistent.");
                cancellationToken.ThrowIfCancellationRequested();
                await transaction.CommitAsync().ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                try { await transaction.RollbackAsync().ConfigureAwait(false); }
                catch (Exception rollback) { throw new AggregateException("Neo4j CQL projection upgrade and rollback failed.", failure, rollback); }
                throw;
            }
        }
    }
}
