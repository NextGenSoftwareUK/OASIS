using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public sealed record PlannedHolonWrite(IHolon Source,Holon Snapshot)
{
    public bool SelectsChildren { get; init; }
    public string CapturedIntentJson { get; init; }
    public IReadOnlyList<Guid> SelectedChildIds { get; init; } = Array.Empty<Guid>();
    public HolonChildRoleManifest RelationshipManifest { get; init; }
}

/// <summary>Validates and snapshots selected nodes without publishing identities or performing I/O.</summary>
public static class HolonGraphWritePlan
{
    public static (List<IHolon> Roots,List<PlannedHolonWrite> Nodes) Create(IEnumerable<IHolon> input,bool children,bool recursive,int depth,int initialDepth)
        =>Create(input,children,recursive,depth,initialDepth,null);
    public static (List<IHolon> Roots,List<PlannedHolonWrite> Nodes) Create(IEnumerable<IHolon> input,bool children,bool recursive,int depth,int initialDepth,Action<IHolon> validateSource)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(depth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(depth));
        var roots=input.ToList();
        if(roots.Count==0) throw new ArgumentException("A nonempty graph is required.");
        var references=new HashSet<IHolon>(ReferenceEqualityComparer.Instance);
        var identities=new HashSet<Guid>();var nodes=new List<PlannedHolonWrite>();
        var memberships=new Dictionary<Guid,HolonChildCollectionState>();
        var pending=new Stack<(IHolon Source,long Depth,Guid? Parent,List<Guid> Siblings)>();
        for(var index=roots.Count-1;index>=0;index--)pending.Push((roots[index],initialDepth,null,null));
        while(pending.Count>0)
        {
            var (source,current,parent,siblings)=pending.Pop();
            if(source==null || !references.Add(source)) throw new ArgumentException("Selected graph has null/repeated nodes or a cycle.");
            var id=source.Id==Guid.Empty ? Guid.NewGuid():source.Id;
            if(!identities.Add(id)) throw new ArgumentException("Selected graph has duplicate identities.");
            siblings?.Add(id);
            if(parent.HasValue && source.ParentHolonId!=Guid.Empty && source.ParentHolonId!=parent.Value)
                throw new ArgumentException("Selected child has a conflicting parent identity.");
            validateSource?.Invoke(source);
            var document=StoredRecordJson.CaptureInputToken<IHolon>(source) as JObject
                ??throw new ArgumentException("Selected graph input must capture a record object.");
            var captured=document.ToString(Newtonsoft.Json.Formatting.None);document.Remove("$type");
            // This JSON was just captured from input, not read from storage.
            var snapshot=StoredRecordJson.Project<Holon>(document);
            snapshot.Id=id;snapshot.ParentHolonId=parent ?? source.ParentHolonId;
            var selectsChildren = children && (depth==0 || current<depth) && (recursive || current==initialDepth);
            var childIds=new List<Guid>();
            nodes.Add(new PlannedHolonWrite(source,snapshot) { SelectsChildren = selectsChildren, CapturedIntentJson = captured,
                SelectedChildIds = childIds.AsReadOnly() });
            if(selectsChildren)
            {
                var membership=HolonChildCollectionState.Capture(source);
                memberships.Add(id,membership);
                var selected=membership.Children;
                for(var index=selected.Count-1;index>=0;index--)pending.Push((selected[index],current+1,id,childIds));
            }
        }
        var plannedIds=new Dictionary<IHolon,Guid>(ReferenceEqualityComparer.Instance);
        foreach(var node in nodes)plannedIds.Add(node.Source,node.Snapshot.Id);
        for(var index=0;index<nodes.Count;index++)
        {
            var node=nodes[index];
            if(memberships.TryGetValue(node.Snapshot.Id,out var membership))
                nodes[index]=node with { RelationshipManifest=membership.CreateManifest(node.Snapshot.Id,child=>plannedIds[child]) };
            OasisJson.Serialize(node.Snapshot);
        }
        return (roots,nodes);
    }
}
