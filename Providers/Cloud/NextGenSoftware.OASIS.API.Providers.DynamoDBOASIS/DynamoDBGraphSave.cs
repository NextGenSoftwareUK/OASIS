using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS
{
    private sealed record PlannedNode(IHolon Source,Holon Snapshot);

    private static (List<IHolon> Roots,List<PlannedNode> Nodes) PlanGraph(IEnumerable<IHolon> input,bool children,bool recursive,int depth,int initialDepth)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(depth));
        var roots=input.ToList();
        if(roots.Count==0) throw new ArgumentException("A nonempty graph is required.");
        var references=new HashSet<IHolon>(ReferenceEqualityComparer.Instance);
        var identities=new HashSet<Guid>();var nodes=new List<PlannedNode>();
        void Visit(IHolon source,int current,Guid? parent)
        {
            if(source==null || !references.Add(source)) throw new ArgumentException("Selected graph has null/repeated nodes or a cycle.");
            var id=source.Id==Guid.Empty ? Guid.NewGuid():source.Id;
            if(!identities.Add(id)) throw new ArgumentException("Selected graph has duplicate identities.");
            if(parent.HasValue && source.ParentHolonId!=Guid.Empty && source.ParentHolonId!=parent.Value)
                throw new ArgumentException("Selected child has a conflicting parent identity.");
            var snapshot=Des<Holon>(Ser(source))!;
            snapshot.Id=id;snapshot.ParentHolonId=parent ?? source.ParentHolonId;
            nodes.Add(new PlannedNode(source,snapshot));
            if(children && (depth==0 || current<depth) && (recursive || current==initialDepth))
                foreach(var child in source.Children ?? Enumerable.Empty<IHolon>()) Visit(child,current+1,id);
        }
        foreach(var root in roots) Visit(root,initialDepth,null);
        foreach(var node in nodes) Ser(node.Snapshot);
        return (roots,nodes);
    }
}
