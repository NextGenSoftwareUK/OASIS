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
            List<NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite> Nodes,HashSet<string> RootIds)
        {
            public void Publish()
            {
                foreach(var node in Nodes) {node.Source.Id=node.Snapshot.Id;node.Source.ParentHolonId=node.Snapshot.ParentHolonId;}
            }
        }

        private PreparedGraph PrepareGraph(IEnumerable<IHolon> roots,bool saveChildren,bool recursive,int maxDepth,int initialDepth=0)
        {
            var plan=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphWritePlan.Create(roots,saveChildren,recursive,maxDepth,initialDepth);
            var models=plan.Nodes.Select(node=> {
                // Preserve SQLite's own full mapping and UTC validation; use shared detached planning for identity/edges.
                var model=CreateHolonModel(node.Source,node.Snapshot.Id);
                model.Id=node.Snapshot.Id.ToString();model.ParentHolonId=node.Snapshot.ParentHolonId.ToString();
                return model;
            }).ToList();
            var rootIds=plan.Roots.Select(source=>plan.Nodes.Single(node=>ReferenceEquals(node.Source,source)).Snapshot.Id.ToString()).ToHashSet();
            return new PreparedGraph(models,plan.Nodes,rootIds);
        }

        private IHolon AttachLoadedChildren(IHolon root, IEnumerable<HolonModel> rows, bool recursive, int maxDepth, int initialDepth = 0)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            var children = rows.Where(row => row.DeletedDate == DateTime.MinValue).ToLookup(row => row.ParentHolonId);
            var path = new HashSet<Guid> { root.Id };
            void Attach(IHolon parent, int depth)
            {
                if ((maxDepth != 0 && depth >= maxDepth) || (!recursive && depth > initialDepth)) return;
                foreach (var row in children[parent.Id.ToString()])
                {
                    var child = GetHolonFromEntity(row);
                    if (!path.Add(child.Id)) throw new InvalidOperationException("Persisted graph contains a cycle.");
                    parent.Children.Add(child);
                    Attach(child, depth + 1);
                    path.Remove(child.Id);
                }
            }
            Attach(root, initialDepth);
            return root;
        }
    }
}
