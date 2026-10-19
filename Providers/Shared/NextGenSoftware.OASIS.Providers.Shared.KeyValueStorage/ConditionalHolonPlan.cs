using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Pre-I/O graph admission and logical intent binding. Does not provide a transaction or receipt store.</summary>
public sealed class ConditionalHolonPlan
{
    public IReadOnlyList<PlannedHolonWrite> Nodes {get;}
    public IReadOnlyList<Guid> RootIds {get;}
    public IReadOnlyDictionary<Guid,IReadOnlyList<Guid>> ChildIds {get;}
    public string Fingerprint {get;}
    public Guid OperationId {get;}
    public ProviderType Authority {get;}
    public ConditionalHolonOperation Operation {get;}
    public IReadOnlyDictionary<Guid,Guid?> ExpectedRevisions {get;}
    private ConditionalHolonPlan(ConditionalHolonWriteRequest request,ProviderType authority)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationId=request.Write.OperationId;
        Authority=request.Write.Authority;Operation=request.Operation;
        ExpectedRevisions=new System.Collections.ObjectModel.ReadOnlyDictionary<Guid,Guid?>(request.Write.Conditions.ToDictionary(c=>c.HolonId,c=>c.ExpectedRevisionId));
        if(request.Write.Authority!=authority)throw new ArgumentException("Conditional request belongs to another authority.");
        if(request.Operation!=ConditionalHolonOperation.Save&&request.Write.Conditions.Any(c=>!c.ExpectedRevisionId.HasValue))
            throw new ArgumentException("Conditional deletion requires existing revisions.");
        var graph=HolonGraphWritePlan.Create(request.Roots,request.SaveChildren,request.Recursive,request.MaxChildDepth,0);
        if(!graph.Nodes.Select(n=>n.Snapshot.Id).ToHashSet().SetEquals(request.Write.Conditions.Select(c=>c.HolonId)))
            throw new ArgumentException("Exactly one condition is required for each selected record.");
        Nodes=Array.AsReadOnly(graph.Nodes.ToArray());RootIds=Array.AsReadOnly(graph.Roots.Select(r=>r.Id).ToArray());
        var edges=new Dictionary<Guid,IReadOnlyList<Guid>>();
        void CaptureEdges(IHolon source,int depth){
            var selected=request.SaveChildren&&(request.MaxChildDepth==0||depth<request.MaxChildDepth)&&(request.Recursive||depth==0)
                ?(source.Children??Enumerable.Empty<IHolon>()).ToArray():Array.Empty<IHolon>();
            edges.Add(source.Id,Array.AsReadOnly(selected.Select(c=>c.Id).ToArray()));foreach(var child in selected)CaptureEdges(child,depth+1);
        }
        foreach(var root in graph.Roots)CaptureEdges(root,0);
        ChildIds=new System.Collections.ObjectModel.ReadOnlyDictionary<Guid,IReadOnlyList<Guid>>(edges);
        var records=new JArray();
        foreach(var node in Nodes){
            var intent=StoredRecordJson.Project<Holon>(node.Snapshot);
            intent.ProviderUniqueStorageKey?.Remove(authority); // Only this authority's current key is assigned by this write owner.
            if(intent.ProviderMetaData!=null&&intent.ProviderMetaData.TryGetValue(authority,out var own)){
                own?.Remove(CosmicConcurrency.ProviderRevisionMetadataKey);
                if(own==null||own.Count==0)intent.ProviderMetaData.Remove(authority);
            }
            var values=JObject.Parse(StoredRecordJson.Serialize(intent));
            foreach(var field in new[]{"Version","VersionId","PreviousVersionId","CreatedDate","ModifiedDate","CreatedByAvatarId","ModifiedByAvatarId","CreatedProviderType","InstanceSavedOnProviderType"})values.Remove(field);
            if(intent.ProviderUniqueStorageKey==null||intent.ProviderUniqueStorageKey.Count==0)values.Remove("ProviderUniqueStorageKey");
            records.Add(values);
        }
        var content=new JObject{["format"]=3,["actor"]=request.Write.ActorId,["authority"]=authority.ToString(),["operation"]=request.Operation.ToString(),["children"]=request.SaveChildren,["recursive"]=request.Recursive,["depth"]=request.MaxChildDepth,["roots"]=new JArray(RootIds),["nodes"]=records,["conditions"]=JArray.FromObject(request.Write.Conditions.OrderBy(c=>c.HolonId))};
        Fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(content).ToString(Formatting.None))));
    }
    public static ConditionalHolonPlan Create(ConditionalHolonWriteRequest request,ProviderType authority)=>new(request,authority);
    private static JToken Canonical(JToken value)=>value is JObject obj
        ?new JObject(obj.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,Canonical(p.Value))))
        :value is JArray array?new JArray(array.Select(Canonical)):value.DeepClone();
}
