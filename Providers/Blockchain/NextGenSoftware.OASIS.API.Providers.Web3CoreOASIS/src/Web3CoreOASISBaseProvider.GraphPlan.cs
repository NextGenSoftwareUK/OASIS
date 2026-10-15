using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
public partial class Web3CoreOASISBaseProvider
{
    private sealed record PlannedWeb3Holon(IHolon Source,Holon Snapshot,Guid OriginalId,Guid OriginalParent);

    private static (List<IHolon> Roots,List<PlannedWeb3Holon> Nodes) PlanWeb3Graph(IEnumerable<IHolon> input,bool children,bool recursive,int depth,int initialDepth)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(depth));
        var roots=input.ToList();
        if(roots.Count==0) throw new ArgumentException("A nonempty graph is required.");
        var references=new HashSet<IHolon>(ReferenceEqualityComparer.Instance);
        var identities=new HashSet<Guid>();
        var nodes=new List<PlannedWeb3Holon>();
        void Visit(IHolon source,int current,Guid? parent)
        {
            if(source==null || !references.Add(source)) throw new ArgumentException("Selected Web3Core graph has null/repeated nodes or a cycle.");
            var id=source.Id==Guid.Empty ? Guid.NewGuid():source.Id;
            if(!identities.Add(id)) throw new ArgumentException("Selected Web3Core graph has duplicate identities.");
            if(parent.HasValue && source.ParentHolonId!=Guid.Empty && source.ParentHolonId!=parent.Value)
                throw new ArgumentException("Selected Web3Core child has a conflicting parent identity.");
            var snapshot=OasisJson.Deserialize<Holon>(OasisJson.Serialize(source));
            snapshot.Id=id;snapshot.ParentHolonId=parent ?? source.ParentHolonId;
            nodes.Add(new PlannedWeb3Holon(source,snapshot,source.Id,source.ParentHolonId));
            if(children && (depth==0 || current<depth) && (recursive || current==initialDepth))
                foreach(var child in source.Children ?? Enumerable.Empty<IHolon>()) Visit(child,current+1,id);
        }
        foreach(var root in roots) Visit(root,initialDepth,null);
        return (roots,nodes);
    }

}
