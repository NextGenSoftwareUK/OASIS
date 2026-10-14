using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public partial class HolonRepository
    {
        private List<HolonModel> PrepareGraph(IEnumerable<IHolon> roots, bool saveChildren, bool recursive, int maxDepth, int initialDepth = 0)
        {
            if (maxDepth < 0 || initialDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            var models = new List<HolonModel>();
            var seen = new HashSet<Guid>();
            void Visit(IHolon holon, int depth, Guid? parent)
            {
                if (holon == null) throw new ArgumentException("Graph contains a null holon.");
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                if (!seen.Add(holon.Id)) throw new ArgumentException("Graph contains a cycle or repeated holon identity.");
                if (parent.HasValue)
                {
                    if (holon.ParentHolonId != Guid.Empty && holon.ParentHolonId != parent.Value)
                        throw new ArgumentException("Graph child has a conflicting parent identity.");
                    holon.ParentHolonId = parent.Value;
                }
                models.Add(CreateHolonModel(holon));
                if (!saveChildren || (maxDepth != 0 && depth >= maxDepth) || (!recursive && depth > initialDepth)) return;
                foreach (var child in holon.Children ?? Enumerable.Empty<IHolon>()) Visit(child, depth + 1, holon.Id);
            }
            foreach (var root in roots) Visit(root, initialDepth, null);
            return models;
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
