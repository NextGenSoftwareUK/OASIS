using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS
{
    private sealed record PlannedEthereumHolon(PlannedHolonWrite Write,StoredRecordPublicationSnapshot<IHolon> Publication)
    {
        public IHolon Source=>Write.Source;
        public Holon Record=>Write.Snapshot;
    }

    private static (List<IHolon> Roots,List<PlannedEthereumHolon> Nodes) PlanStoredGraph(
        IEnumerable<IHolon> roots,bool saveChildren,bool recursive,int maxDepth,int initialDepth)
    {
        var publications=new Dictionary<IHolon,StoredRecordPublicationSnapshot<IHolon>>(ReferenceEqualityComparer.Instance);
        var plan=HolonGraphWritePlan.Create(roots,saveChildren,recursive,maxDepth,initialDepth,
            source=>publications.Add(source,StoredRecordPublicationSnapshot<IHolon>.Capture(source)));
        var nativeKeys=new HashSet<int>();
        var records=plan.Nodes.ToDictionary(node=>node.Snapshot.Id,node=>node.Snapshot);
        foreach(var node in plan.Nodes)
            if(!nativeKeys.Add(Core.Utilities.HashUtility.GetNumericHash(node.Snapshot.Id.ToString())))
                throw new ArgumentException("Selected Ethereum graph contains colliding numeric storage keys.");
        // Preserve Ethereum's embedded selected-child snapshots without recursing over caller collections.
        foreach(var node in plan.Nodes.AsEnumerable().Reverse()){
            if(node.SelectsChildren)node.Snapshot.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(node,HolonChildRoleWriteKind.SourceSave,false,null);
            node.Snapshot.Children=node.SelectedChildIds.Select(id=>(IHolon)records[id]).ToList();
            if(node.SelectsChildren){
                node.Snapshot.ChildrenIds=node.SelectedChildIds.ToList();
                node.Snapshot.ChildIdListCache=string.Join(",",node.SelectedChildIds);
                node.Snapshot.AllChildIdListCache=string.Join(",",PlannedDescendantIds(node.Snapshot));
            }
            _=SerializePrivateStorage("holon",node.Snapshot);
        }
        var bySource=plan.Nodes.ToDictionary(node=>node.Source,node=>node.Snapshot,ReferenceEqualityComparer.Instance);
        return(plan.Roots.Select(root=>(IHolon)bySource[root]).ToList(),
            plan.Nodes.Select(node=>new PlannedEthereumHolon(node,publications[node.Source])).ToList());
    }

    private static IEnumerable<Guid> PlannedDescendantIds(IHolon root)
    {
        var pending=new Stack<IHolon>(root.Children.AsEnumerable().Reverse());
        while(pending.Count>0){
            var child=pending.Pop();yield return child.Id;
            foreach(var descendant in child.Children.AsEnumerable().Reverse())pending.Push(descendant);
        }
    }
}
