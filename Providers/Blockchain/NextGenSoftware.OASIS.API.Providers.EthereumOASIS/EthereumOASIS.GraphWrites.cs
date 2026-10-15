using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private sealed class StoredNodeResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member,MemberSerialization serialization)
            {
                var property=base.CreateProperty(member,serialization);
                if(member.Name is "Children" or "AllChildren" or "ParentHolon" or "ParentHolon2" or "ParentHolon3") property.Ignored=true;
                return property;
            }
        }

        private static (List<IHolon> Roots,List<(IHolon Record,string Payload)> Nodes) PlanStoredGraph(
            IEnumerable<IHolon> roots,bool saveChildren,bool recursive,int maxDepth,int initialDepth)
        {
            ArgumentNullException.ThrowIfNull(roots);
            if(maxDepth < 0 || initialDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            var sourceRoots=roots.ToList();
            var seenIds=new HashSet<Guid>();
            var seenNativeKeys=new HashSet<int>();
            var seenReferences=new HashSet<IHolon>(ReferenceEqualityComparer.Instance);
            var nodes=new List<IHolon>();
            var settings=new JsonSerializerSettings { ContractResolver=new StoredNodeResolver() };
            IHolon Visit(IHolon source,int depth,Guid? parent)
            {
                if(source == null) throw new ArgumentException("Selected Ethereum graph contains a null holon.");
                if(!seenReferences.Add(source)) throw new ArgumentException("Selected Ethereum graph repeats a reference or contains a cycle.");
                if(source.Id == Guid.Empty) source.Id=Guid.NewGuid();
                if(!seenIds.Add(source.Id)) throw new ArgumentException("Selected Ethereum graph contains duplicate GUIDs.");
                if(!seenNativeKeys.Add(Core.Utilities.HashUtility.GetNumericHash(source.Id.ToString())))
                    throw new ArgumentException("Selected Ethereum graph contains colliding numeric storage keys.");
                if(parent.HasValue && source.ParentHolonId != Guid.Empty && source.ParentHolonId != parent.Value)
                    throw new ArgumentException("Selected Ethereum child has a conflicting parent GUID.");
                if(parent.HasValue) source.ParentHolonId=parent.Value;
                var copy=JsonConvert.DeserializeObject<Core.Holons.Holon>(JsonConvert.SerializeObject(source,settings),new StoredHolonConverter());
                copy.Children=new List<IHolon>();
                nodes.Add(copy);
                if(saveChildren && (maxDepth == 0 || depth < maxDepth) && (recursive || depth == initialDepth))
                    foreach(var child in source.Children ?? Enumerable.Empty<IHolon>()) copy.Children.Add(Visit(child,depth+1,source.Id));
                return copy;
            }
            var selectedRoots=sourceRoots.Select(root=>Visit(root,initialDepth,null)).ToList();
            // Materialize every encrypted payload before the first native transaction.
            var payloads=nodes.AsEnumerable().Reverse().ToDictionary(node=>node.Id,node=>SerializePrivateStorage("holon",node));
            var encoded=nodes.Select(node=>(Record:node,Payload:payloads[node.Id])).ToList();
            return (selectedRoots,encoded);
        }
    }
}
