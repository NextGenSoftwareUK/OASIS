using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static Holon ReadHolonNode(Neo4j.Driver.INode node)
    {
        if (!node.Properties.TryGetValue("SnapshotJson", out var json) || json is not string snapshot || string.IsNullOrWhiteSpace(snapshot))
            throw new InvalidDataException("Neo4j holon canonical snapshot is missing.");
        var holon = OasisJson.Deserialize<Holon>(snapshot);
        if (holon == null || !Guid.TryParse(node["Id"].As<string>(), out var id) || id == Guid.Empty || holon.Id != id)
            throw new InvalidDataException("Neo4j holon snapshot identity differs from its index.");
        return holon;
    }

    private async Task<OASISResult<IHolon>> SaveOneHolonSnapshotAsync(IHolon holon, bool children, bool recursive, int maximumDepth)
    {
        var batch = await SaveHolonSnapshotsAsync(new[] { holon }, children, recursive, maximumDepth, 0);
        return new OASISResult<IHolon> { IsError = batch.IsError, IsSaved = batch.IsSaved,
            Result = batch.IsError ? null : batch.Result.Single(), ErrorCode = batch.ErrorCode, Exception = batch.Exception, Message = batch.Message };
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonSnapshotsAsync(IEnumerable<IHolon> holons,
        bool children, bool recursive, int maximumDepth, int initialDepth)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        var dispatched = false;
        try
        {
            ArgumentNullException.ThrowIfNull(holons);
            if (!IsProviderActivated || _driver == null) throw new InvalidOperationException("Neo4j provider is not activated.");
            if (maximumDepth < 0 || initialDepth < 0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
            var roots = holons.ToList();
            var sources = new List<IHolon>();
            var staged = new List<Holon>();
            var rootIds = new List<Guid>();
            var edges = new List<Dictionary<string, object>>();
            var references = new HashSet<IHolon>(ReferenceEqualityComparer.Instance);
            var ids = new HashSet<Guid>();
            Guid Visit(IHolon source, int depth, Guid? parent)
            {
                ArgumentNullException.ThrowIfNull(source);
                if (!references.Add(source)) throw new ArgumentException("Neo4j selected graph contains a repeated node or cycle.");
                var record = OasisJson.Deserialize<Holon>(OasisJson.Serialize(source));
                if (record == null) throw new InvalidDataException("Neo4j holon snapshot is null.");
                if (record.Id == Guid.Empty) record.Id = Guid.NewGuid();
                if (!ids.Add(record.Id)) throw new ArgumentException("Neo4j holon batch contains duplicate identities.");
                if (parent.HasValue)
                {
                    if (record.ParentHolonId != Guid.Empty && record.ParentHolonId != parent.Value)
                        throw new ArgumentException("Neo4j selected child has a conflicting parent identity.");
                    record.ParentHolonId = parent.Value;
                    edges.Add(new Dictionary<string, object> { ["parent"] = parent.Value.ToString(), ["child"] = record.Id.ToString() });
                }
                record.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = record.Id.ToString();
                record.IsNewHolon = false;
                sources.Add(source);
                staged.Add(record);
                if (children && (maximumDepth == 0 || depth < maximumDepth) && (recursive || depth == initialDepth))
                    foreach (var child in source.Children ?? Enumerable.Empty<IHolon>()) Visit(child, depth + 1, record.Id);
                return record.Id;
            }
            foreach (var root in roots) rootIds.Add(Visit(root, initialDepth, null));
            if (staged.Count == 0) { result.Result = Array.Empty<IHolon>(); return result; }
            var rows = staged.Select(record => new Dictionary<string, object> {
                ["id"] = record.Id.ToString(), ["name"] = record.Name, ["description"] = record.Description,
                ["type"] = record.HolonType.ToString(), ["snapshot"] = OasisJson.Serialize(record),
                ["metadataTokens"] = MetadataLookupTokens(record)
            }).ToList();
            Dictionary<Guid, Holon> acknowledged;
            using (var session = _driver.AsyncSession())
            {
                using var transaction = await session.BeginTransactionAsync();
                const string query = "UNWIND $rows AS row MERGE (h:Holon {Id:row.id}) "
                    + "SET h.ProviderKey=row.id, h.Name=row.name, h.Description=row.description, h.HolonType=row.type, h.SnapshotJson=row.snapshot, h.MetaLookupTokens=row.metadataTokens RETURN h";
                dispatched = true;
                var records = await (await transaction.RunAsync(query, new { rows })).ToListAsync();
                acknowledged = records.Select(row => ReadHolonNode(row["h"].As<Neo4j.Driver.INode>())).ToDictionary(record => record.Id);
                if (acknowledged.Count != staged.Count || ids.Any(id => !acknowledged.ContainsKey(id)))
                    throw new InvalidDataException("Neo4j holon batch acknowledgement differs from submitted identities.");
                if (edges.Count != 0)
                {
                    var linked = await (await transaction.RunAsync("UNWIND $edges AS edge MATCH (p:Holon {Id:edge.parent}), (c:Holon {Id:edge.child}) "
                        + "MERGE (p)-[:HAS_CHILD]->(c) RETURN count(*) AS linked", new { edges })).SingleAsync();
                    if (linked["linked"].As<long>() != edges.Count) throw new InvalidDataException("Neo4j graph edge acknowledgement is incomplete.");
                }
                await transaction.CommitAsync();
            }
            for (var index = 0; index < sources.Count; index++)
            {
                sources[index].Id = staged[index].Id;
                sources[index].ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = staged[index].Id.ToString();
                sources[index].IsNewHolon = false;
                sources[index].ParentHolonId = staged[index].ParentHolonId;
            }
            result.Result = rootIds.Select(id => (IHolon)acknowledged[id]).ToList();
            foreach (var record in acknowledged.Values)
                record.Children = edges.Where(edge => (string)edge["parent"] == record.Id.ToString())
                    .Select(edge => (IHolon)acknowledged[Guid.Parse((string)edge["child"])]).ToList();
            result.IsSaved = true;
        }
        catch (Exception cause)
        {
            result.IsError = true;
            result.Exception = dispatched ? new Neo4jMutationReconciliationException(cause) : cause;
            if (!dispatched && (cause is ArgumentException || cause is InvalidDataException)) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
            result.Message = result.Exception.Message;
        }
        return result;
    }
}
