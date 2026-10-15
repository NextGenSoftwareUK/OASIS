using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private static Func<IHolon,IHolon> CreateHolonProjector(IEnumerable<IHolon> records,bool loadChildren,
            bool recursive,int maxChildDepth,int initialDepth,bool childrenFromProvider)
        {
            var children=records.ToLookup(row=>row.ParentHolonId);
            IHolon Project(IHolon source,int depth,HashSet<Guid> path)
            {
                if (!path.Add(source.Id)) throw new InvalidDataException("Ethereum persisted holon graph contains a cycle.");
                // Independent graphs per returned root: projecting one cannot trim another.
                var copy=JsonConvert.DeserializeObject<Core.Holons.Holon>(JsonConvert.SerializeObject(source),new StoredHolonConverter());
                copy.Children=new List<IHolon>();
                if (loadChildren && (maxChildDepth == 0 || depth < maxChildDepth) && (recursive || depth == initialDepth))
                {
                    var selected=childrenFromProvider ? children[source.Id] : source.Children ?? Enumerable.Empty<IHolon>();
                    foreach(var child in selected)
                    {
                        if(child == null) throw new InvalidDataException("Ethereum persisted holon graph contains a null child.");
                        copy.Children.Add(Project(child,depth+1,path));
                    }
                }
                path.Remove(source.Id);
                return copy;
            }
            return root=>Project(root,initialDepth,new HashSet<Guid>());
        }
    }
}
