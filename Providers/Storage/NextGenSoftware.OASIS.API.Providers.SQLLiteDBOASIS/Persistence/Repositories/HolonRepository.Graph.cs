using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public partial class HolonRepository
    {
        private sealed record PreparedGraph(List<HolonModel> Models,
            List<NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite> Nodes,HashSet<string> RootIds,
            IReadOnlyDictionary<Guid,IReadOnlyList<Guid>> ChildIds)
        {
            public void Publish()
            {
                foreach(var node in Nodes) {node.Source.Id=node.Snapshot.Id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;}
            }
        }

        private PreparedGraph PrepareGraph(IEnumerable<IHolon> roots,bool saveChildren,bool recursive,int maxDepth,int initialDepth=0)
        {
            var plan=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphWritePlan.Create(roots,saveChildren,recursive,maxDepth,initialDepth,
                source=>Context.DataContext.ValidateDeletionUtc(source.DeletedDate));
            var models=plan.Nodes.Select(node=> {
                // Preserve SQLite's own full mapping and UTC validation; use shared detached planning for identity/edges.
                var model=CreateHolonModel(node.Snapshot,node.Snapshot.Id);
                model.Id=node.Snapshot.Id.ToString();model.ParentHolonId=node.Snapshot.ParentHolonId.ToString();
                return model;
            }).ToList();
            var rootIds=plan.Roots.Select(source=>plan.Nodes.Single(node=>ReferenceEquals(node.Source,source)).Snapshot.Id.ToString()).ToHashSet();
            var childIds=plan.Nodes.ToDictionary(node=>node.Snapshot.Id,node=>node.SelectedChildIds);
            return new PreparedGraph(models,plan.Nodes,rootIds,childIds);
        }

        // A graph commit acknowledges all selected records, not just its root DTO. Build the response
        // from the committed models, without an additional (potentially newer) database read.
        private List<IHolon> CommittedGraph(PreparedGraph graph)
        {
            var records = graph.Models.Select(GetHolonFromEntity).ToDictionary(record => record.Id);
            foreach (var pair in graph.ChildIds)
                records[pair.Key].Children=pair.Value.Select(id=>records[id]).ToList();
            return graph.Nodes.Where(node => graph.RootIds.Contains(node.Snapshot.Id.ToString()))
                .Select(node => records[node.Snapshot.Id]).ToList();
        }

        private IHolon AttachLoadedChildren(IHolon root, IEnumerable<HolonModel> rows, bool recursive, int maxDepth, long initialDepth = 0)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            var children = rows.Where(row => row.DeletedDate == DateTime.MinValue).ToLookup(row => row.ParentHolonId);
            var path = new HashSet<Guid> { root.Id };
            var pending=new Stack<(IHolon Parent,HolonModel Source,long Depth,Guid? Exit)>();
            pending.Push((root,null,initialDepth,null));
            while(pending.Count>0)
            {
                var (parent,source,depth,exit)=pending.Pop();
                if(exit.HasValue){path.Remove(exit.Value);continue;}
                var current=parent;
                if(source!=null)
                {
                    current=GetHolonFromEntity(source);
                    if(!path.Add(current.Id))throw new InvalidOperationException("Persisted graph contains a cycle.");
                    parent.Children.Add(current);
                }
                pending.Push((null,null,0,current.Id));
                if((maxDepth!=0 && depth>=maxDepth) || (!recursive && depth>initialDepth))continue;
                var selected=children[current.Id.ToString()].ToArray();
                for(var index=selected.Length-1;index>=0;index--)pending.Push((current,selected[index],depth+1,null));
            }
            return root;
        }

        private static void ValidateReadTraversal(int maximumDepth, int initialDepth = 0)
        {
            if (maximumDepth < 0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
            if (initialDepth < 0) throw new ArgumentOutOfRangeException(nameof(initialDepth));
        }
    }
}
