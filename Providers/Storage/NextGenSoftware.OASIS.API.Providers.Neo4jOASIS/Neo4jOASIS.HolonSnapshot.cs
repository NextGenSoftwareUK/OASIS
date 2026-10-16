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

    private async Task<OASISResult<IHolon>> SaveOneHolonSnapshotAsync(IHolon holon)
    {
        var batch = await SaveHolonSnapshotsAsync(new[] { holon });
        return new OASISResult<IHolon> { IsError = batch.IsError, IsSaved = batch.IsSaved,
            Result = batch.IsError ? null : batch.Result.Single(), ErrorCode = batch.ErrorCode, Exception = batch.Exception, Message = batch.Message };
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonSnapshotsAsync(IEnumerable<IHolon> holons)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        var dispatched = false;
        try
        {
            ArgumentNullException.ThrowIfNull(holons);
            if (!IsProviderActivated || _driver == null) throw new InvalidOperationException("Neo4j provider is not activated.");
            var sources = holons.ToList();
            var staged = new List<Holon>();
            var ids = new HashSet<Guid>();
            foreach (var source in sources)
            {
                ArgumentNullException.ThrowIfNull(source);
                var record = OasisJson.Deserialize<Holon>(OasisJson.Serialize(source));
                if (record == null) throw new InvalidDataException("Neo4j holon snapshot is null.");
                if (record.Id == Guid.Empty) record.Id = Guid.NewGuid();
                if (!ids.Add(record.Id)) throw new ArgumentException("Neo4j holon batch contains duplicate identities.");
                record.ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = record.Id.ToString();
                record.IsNewHolon = false;
                staged.Add(record);
            }
            if (staged.Count == 0) { result.Result = Array.Empty<IHolon>(); return result; }
            var rows = staged.Select(record => new Dictionary<string, object> {
                ["id"] = record.Id.ToString(), ["name"] = record.Name, ["description"] = record.Description,
                ["type"] = record.HolonType.ToString(), ["snapshot"] = OasisJson.Serialize(record)
            }).ToList();
            Dictionary<Guid, Holon> acknowledged;
            using (var session = _driver.AsyncSession())
            {
                const string query = "UNWIND $rows AS row MERGE (h:Holon {Id:row.id}) "
                    + "SET h.ProviderKey=row.id, h.Name=row.name, h.Description=row.description, h.HolonType=row.type, h.SnapshotJson=row.snapshot RETURN h";
                dispatched = true;
                var records = await (await session.RunAsync(query, new { rows })).ToListAsync();
                acknowledged = records.Select(row => ReadHolonNode(row["h"].As<Neo4j.Driver.INode>())).ToDictionary(record => record.Id);
                if (acknowledged.Count != staged.Count || ids.Any(id => !acknowledged.ContainsKey(id)))
                    throw new InvalidDataException("Neo4j holon batch acknowledgement differs from submitted identities.");
            }
            for (var index = 0; index < sources.Count; index++)
            {
                sources[index].Id = staged[index].Id;
                sources[index].ProviderUniqueStorageKey[Core.Enums.ProviderType.Neo4jOASIS] = staged[index].Id.ToString();
                sources[index].IsNewHolon = false;
            }
            result.Result = staged.Select(record => (IHolon)acknowledged[record.Id]).ToList();
            result.IsSaved = true;
        }
        catch (Exception cause)
        {
            result.IsError = true;
            result.Exception = dispatched ? new Neo4jMutationReconciliationException(cause) : cause;
            result.Message = result.Exception.Message;
        }
        return result;
    }
}
