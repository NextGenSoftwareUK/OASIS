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
    private async Task<OASISResult<IHolon>> ReadHolonRootAsync(string field, string value, bool children, bool recursive, int maximumDepth, bool continueOnError,int version)
    {
        var result = new OASISResult<IHolon>();
        var failures = new List<string>();
        try
        {
            if (field != "Id" && field != "ProviderKey") throw new ArgumentException("Unknown holon selector.");
            if (maximumDepth < 0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
            if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
            if (!IsProviderActivated || _driver == null) throw new InvalidOperationException("Neo4j provider is not activated.");
            using (var session = _driver.AsyncSession())
            {
                var rows = await (await session.RunAsync("MATCH (h:Holon {" + field + ":$value}) RETURN h", new { value })).ToListAsync();
                if (rows.Count == 0) return new OASISResult<IHolon> { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Holon not found in Neo4j." };
                if (rows.Count != 1) throw new Core.Helpers.StorageLifecycleConflictException("Neo4j holon identity is not unique.");
                var root = ReadHolonNode(rows[0]["h"].As<Neo4j.Driver.INode>());
                root=await ReadStoredVersionAsync(session,"Holon",root,version,json=>OasisJson.DeserializeStored<Holon>(json));
                if(root==null)return new OASISResult<IHolon>{IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Requested Neo4j holon version was not found."};
                if (children) await HydrateNativeChildrenAsync(session, root, recursive, maximumDepth, 0, new HashSet<Guid> { root.Id }, continueOnError, failures);
                result.Result = root;
            }
            result.IsLoaded = true;
        }
        catch (Exception cause)
        {
            result.IsError = true; result.Exception = cause; result.Message = cause.Message;
            if (cause is InvalidDataException || cause is Core.Helpers.StorageLifecycleConflictException)
                result.ErrorCode = Core.Helpers.StorageLifecycleConflictException.ErrorCode;
            else if (cause is ArgumentException) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
        }
        return MarkNativePartialRead(result, failures);
    }

    private static OASISResult<T> MarkNativePartialRead<T>(OASISResult<T> result, List<string> failures)
    {
        if (result.IsError || failures.Count == 0) return result;
        result.IsWarning = true;
        result.Message = $"Returned partial holon data; {failures.Count} descendant reads failed.";
        result.MetaData ??= new Dictionary<string, string>();
        result.MetaData["partialHolonReads"] = "true";
        result.MetaData["failedHolonReadCount"] = failures.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        result.MetaData["failedHolonReads"] = OasisJson.Serialize(failures.ToArray());
        return result;
    }

    private static async Task HydrateNativeChildrenAsync(IAsyncSession session, IHolon parent,
        bool recursive, int maximumDepth, int depth, HashSet<Guid> visited,
        bool continueOnError = false, List<string> failures = null)
    {
        List<IRecord> rows;
        try
        {
            rows = await (await session.RunAsync("MATCH (p:Holon {Id:$id})-[:HAS_CHILD]->(h:Holon) RETURN h",
                new { id = parent.Id.ToString() })).ToListAsync();
        }
        catch (Exception cause) when (continueOnError && failures != null && cause is not InvalidDataException
            && (cause is IOException || cause is TimeoutException || cause is ServiceUnavailableException || cause is SessionExpiredException))
        {
            failures.Add(parent.Id + " (" + cause.GetType().Name + ")");
            return;
        }
        var selected = new List<IHolon>();
        foreach (var row in rows)
        {
            var child = ReadHolonNode(row["h"].As<Neo4j.Driver.INode>());
            if (!visited.Add(child.Id)) throw new Core.Helpers.StorageLifecycleConflictException("Neo4j selected graph has a repeated identity or cycle.");
            if (child.ParentHolonId != parent.Id) throw new Core.Helpers.StorageLifecycleConflictException("Neo4j child snapshot conflicts with its native parent edge.");
            if (recursive && (maximumDepth == 0 || depth + 1 < maximumDepth))
                await HydrateNativeChildrenAsync(session, child, true, maximumDepth, depth + 1, visited, continueOnError, failures);
            selected.Add(child);
        }
        parent.Children = selected;
    }

    private static Holon ReadHolonNode(Neo4j.Driver.INode node)
    {
        if (!node.Properties.TryGetValue("SnapshotJson", out var json) || json is not string snapshot || string.IsNullOrWhiteSpace(snapshot))
            throw new InvalidDataException("Neo4j holon canonical snapshot is missing.");
        var holon = OasisJson.DeserializeStored<Holon>(snapshot);
        if (holon == null || holon.Id != ReadNativeIdentity(node))
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
        var archiveVersions = IsVersionControlEnabled;
        try
        {
            ArgumentNullException.ThrowIfNull(holons);
            if (!IsProviderActivated || _driver == null) throw new InvalidOperationException("Neo4j provider is not activated.");
            if (maximumDepth < 0 || initialDepth < 0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
            var roots = holons.ToList();
            if (roots.Count == 0) { result.Result = Array.Empty<IHolon>(); return result; }
            var plan = HolonGraphWritePlan.Create(roots, children, recursive, maximumDepth, initialDepth);
            var rootSources = new HashSet<IHolon>(plan.Roots, ReferenceEqualityComparer.Instance);
            var sources = plan.Nodes.Select(node => node.Source).ToList();
            var staged = plan.Nodes.Select(node => node.Snapshot).ToList();
            var ids = new HashSet<Guid>(staged.Select(record => record.Id));
            var rootIds = plan.Nodes.Where(node => rootSources.Contains(node.Source)).Select(node => node.Snapshot.Id).ToList();
            var edges = plan.Nodes.Where(node => !rootSources.Contains(node.Source)).Select(node =>
                new Dictionary<string, object> { ["parent"] = node.Snapshot.ParentHolonId.ToString(), ["child"] = node.Snapshot.Id.ToString() }).ToList();
            foreach (var record in staged)
            {
                record.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = record.Id.ToString();
                record.IsNewHolon = false;
            }
            var rows = staged.Select(record => new Dictionary<string, object> {
                ["id"] = record.Id.ToString(), ["name"] = record.Name, ["description"] = record.Description,
                ["type"] = record.HolonType.ToString(), ["snapshot"] = OasisJson.Serialize(record),
                ["metadataTokens"] = MetadataLookupTokens(record)
            }).ToList();
            Dictionary<Guid, Holon> acknowledged;
            using (var session = _driver.AsyncSession())
            {
                using var transaction = await session.BeginTransactionAsync();
                dispatched = true;
                if(archiveVersions)
                {
                    var byId=rows.ToDictionary(row=>(string)row["id"]);
                    foreach(var record in staged.OrderBy(record=>record.Id))
                    {
                        var row=byId[record.Id.ToString()];
                        row["expected"]=await StampRecordHistoryAsync(transaction,"Holon",record,ReadHolonNode);
                        row["writeId"]=Guid.NewGuid().ToString("N");
                        row["snapshot"]=OasisJson.Serialize(record);
                    }
                }
                var query="UNWIND $rows AS row MERGE (h:Holon {Id:row.id}) ";
                if(archiveVersions)query+="ON CREATE SET h.__oasisCreatedBy=row.writeId "
                    + "SET h.__oasisHolonLock=true REMOVE h.__oasisHolonLock WITH h,row "
                    + "WHERE (row.expected IS NULL AND h.__oasisCreatedBy=row.writeId) OR h.SnapshotJson=row.expected REMOVE h.__oasisCreatedBy ";
                query+="SET h.ProviderKey=row.id, h.Name=row.name, h.Description=row.description, h.HolonType=row.type, h.SnapshotJson=row.snapshot, h.MetaLookupTokens=row.metadataTokens RETURN h";
                var records = await (await transaction.RunAsync(query, new { rows })).ToListAsync();
                acknowledged = records.Select(row => ReadHolonNode(row["h"].As<Neo4j.Driver.INode>())).ToDictionary(record => record.Id);
                if (acknowledged.Count != staged.Count || ids.Any(id => !acknowledged.ContainsKey(id)))
                    throw new InvalidDataException("Neo4j holon batch acknowledgement differs from submitted identities.");
                if (edges.Count != 0)
                {
                    // Selected child parent identity and native incoming edge move in the same transaction.
                    await (await transaction.RunAsync("UNWIND $edges AS edge MATCH (c:Holon {Id:edge.child}) "
                        + "MATCH (old:Holon)-[r:HAS_CHILD]->(c) WHERE old.Id <> edge.parent DELETE r", new { edges })).ConsumeAsync();
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
                if(archiveVersions){sources[index].Version=staged[index].Version;sources[index].VersionId=staged[index].VersionId;sources[index].PreviousVersionId=staged[index].PreviousVersionId;}
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
