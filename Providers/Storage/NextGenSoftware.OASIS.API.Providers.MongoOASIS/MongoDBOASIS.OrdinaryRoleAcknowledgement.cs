using System;
using System.Collections.Generic;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    private static void PublishOrdinaryChildOwnership(IReadOnlyList<PlannedHolonWrite> nodes,
        IReadOnlyDictionary<Guid,IHolon> committed)
    {
        if(committed==null)throw new InvalidDataException("An acknowledged ordinary graph requires its committed records.");
        var prepared=new HashSet<IHolon>(ReferenceEqualityComparer.Instance);
        foreach(var node in nodes)
        {
            if(!committed.ContainsKey(node.Snapshot.Id))
                throw new InvalidDataException("An acknowledged ordinary graph omitted a selected record.");
            if(HolonChangeTracking.TryGetPreparedChildSelection(node.Source,out _))prepared.Add(node.Source);
        }
        // Publish all parent edges before any role acknowledgement validates them.
        foreach(var node in nodes)
        {
            node.Source.Id=node.Snapshot.Id;
            node.Source.ParentHolonId=node.Snapshot.ParentHolonId;
            if(!prepared.Contains(node.Source) && node.Source is Holon owner)
                owner.ChildRoleManifestJson=((Holon)committed[node.Snapshot.Id]).ChildRoleManifestJson;
        }
        // The manager owns acknowledgement of its flattened transport records.
        foreach(var node in nodes)
            if(!prepared.Contains(node.Source) && node.Source is Holon)
                HolonChildRolePersistence.PublishAcknowledgedSelection(node);
    }
}
