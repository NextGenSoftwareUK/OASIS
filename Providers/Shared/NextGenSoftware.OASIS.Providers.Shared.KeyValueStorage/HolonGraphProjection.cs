using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public static class HolonGraphProjection
{
    public static Func<IHolon,IHolon> Create(IEnumerable<IHolon> records,bool loadChildren,bool recursive,
        int maxChildDepth,int initialDepth,bool childrenFromProvider,Func<IHolon,IHolon> clone)
    {
        if(maxChildDepth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
        var children=records.ToLookup(row=>row.ParentHolonId);
        IHolon Project(IHolon source,int depth,HashSet<Guid> path)
        {
            if(!path.Add(source.Id)) throw new InvalidDataException("Persisted holon graph contains a cycle.");
            var copy=clone(source);
            copy.Children=new List<IHolon>();
            if(loadChildren && (maxChildDepth==0 || depth<maxChildDepth) && (recursive || depth==initialDepth))
            {
                var selected=childrenFromProvider ? children[source.Id] : source.Children ?? Enumerable.Empty<IHolon>();
                foreach(var child in selected)
                {
                    if(child == null) throw new InvalidDataException("Persisted holon graph contains a null child.");
                    copy.Children.Add(Project(child,depth+1,path));
                }
            }
            path.Remove(source.Id);
            return copy;
        }
        return root=>Project(root,initialDepth,new HashSet<Guid>());
    }
}
