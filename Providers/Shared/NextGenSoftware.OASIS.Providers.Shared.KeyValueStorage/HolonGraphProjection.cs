using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;

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
        var pool=records.ToArray();
        var children=pool.ToLookup(row=>row.ParentHolonId);
        var identities=pool.ToLookup(row=>row.Id);
        IHolon[] SelectChildren(IHolon source)
        {
            if(!childrenFromProvider)return (source.Children ?? Enumerable.Empty<IHolon>()).ToArray();
            if(source is not Holon {ChildRoleManifestJson:not null} owner)return children[source.Id].ToArray();
            var manifest=HolonChildRoleManifestJson.Deserialize(owner.ChildRoleManifestJson);
            if(manifest.ParentId!=source.Id)throw new InvalidDataException("Persisted child-role manifest belongs to another owner.");
            return manifest.Roles.SelectMany(role=>role.ChildIds).Distinct().Select(id=>{
                var matches=identities[id].Take(2).ToArray();
                if(matches.Length!=1 || matches[0].ParentHolonId!=source.Id)
                    throw new InvalidDataException("Selected child-role member is missing, ambiguous or belongs to another parent.");
                return matches[0];
            }).ToArray();
        }
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
                    var selected=SelectChildren(source);
                    for(var index=selected.Length-1;index>=0;index--)pending.Push((selected[index],depth+1,copy,null));
                }
            }
            return result;
        }
        return Project;
    }
}
