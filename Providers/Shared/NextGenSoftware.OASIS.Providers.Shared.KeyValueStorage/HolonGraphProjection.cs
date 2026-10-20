using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

public static class HolonGraphProjection
{
    public static Func<IHolon,IHolon> Create(IEnumerable<IHolon> records,bool loadChildren,bool recursive,
        int maxChildDepth,int initialDepth,bool childrenFromProvider,Func<IHolon,IHolon> clone,bool rootsAreChildren=false)
    {
        if(maxChildDepth<0 || initialDepth<0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
        // Parent lookups have already traversed one edge to produce their result roots.
        // Nonrecursive parent lookup returns those immediate children without grandchildren.
        var startDepth=(long)initialDepth+(rootsAreChildren ? 1:0);
        if(rootsAreChildren)loadChildren=loadChildren && recursive;
        var children=records.ToLookup(row=>row.ParentHolonId);
        IHolon Project(IHolon root)
        {
            var path=new HashSet<Guid>();IHolon result=null;
            var pending=new Stack<(IHolon Source,long Depth,IHolon Parent,Guid? Exit)>();
            pending.Push((root,startDepth,null,null));
            while(pending.Count>0)
            {
                var (source,depth,parent,exit)=pending.Pop();
                if(exit.HasValue){path.Remove(exit.Value);continue;}
                if(source==null)throw new InvalidDataException("Persisted holon graph contains a null child.");
                var id=source.Id;
                if(!path.Add(id))throw new InvalidDataException("Persisted holon graph contains a cycle.");
                var copy=clone(source);copy.Children=new List<IHolon>();
                if(parent==null)result=copy;else parent.Children.Add(copy);
                pending.Push((null,0,null,id));
                if(loadChildren && (maxChildDepth==0 || depth<maxChildDepth) && (recursive || depth==startDepth))
                {
                    var selected=(childrenFromProvider ? children[id] : source.Children ?? Enumerable.Empty<IHolon>()).ToArray();
                    for(var index=selected.Length-1;index>=0;index--)pending.Push((selected[index],depth+1,copy,null));
                }
            }
            return result;
        }
        return Project;
    }
}
