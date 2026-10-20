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
public sealed class ConditionalHolonPlan : IAtomicHolonWritePlan
{
    public IReadOnlyList<PlannedHolonWrite> Nodes {get;}
    public IReadOnlyList<Guid> RootIds {get;}
    public IReadOnlyDictionary<Guid,IReadOnlyList<Guid>> ChildIds {get;}
    public string Fingerprint {get;}
    public Guid OperationId {get;}
    public ProviderType Authority {get;}
    public ConditionalHolonOperation Operation {get;}
    public IReadOnlyDictionary<Guid,Guid?> ExpectedRevisions {get;}
    public HolonReplicationEnrollment ReplicationEnrollment {get;}
    public bool SaveChildren {get;}
    public bool Recursive {get;}
    public int MaxChildDepth {get;}
    private ConditionalHolonPlan(ConditionalHolonWriteRequest request,ProviderType authority,HolonReplicationEnrollment enrollment=null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if(request.Write.ReplicationEnrollment!=null && enrollment==null)
            throw new ArgumentException("This conditional transaction owner does not support atomic replication enrollment.");
        if(enrollment!=null && request.Write.ReplicationEnrollment is { } embedded
            && (embedded.SourceId!=enrollment.SourceId || !embedded.TargetIds.SequenceEqual(enrollment.TargetIds)))
            throw new ArgumentException("Explicit and embedded replication enrollment differ.");
        ReplicationEnrollment=enrollment;SaveChildren=request.SaveChildren;Recursive=request.Recursive;MaxChildDepth=request.MaxChildDepth;
        OperationId=request.Write.OperationId;
        Authority=request.Write.Authority;Operation=request.Operation;
        ExpectedRevisions=new System.Collections.ObjectModel.ReadOnlyDictionary<Guid,Guid?>(request.Write.Conditions.ToDictionary(c=>c.HolonId,c=>c.ExpectedRevisionId));
        if(request.Write.Authority!=authority)throw new ArgumentException("Conditional request belongs to another authority.");
        if(request.Operation!=ConditionalHolonOperation.Save&&request.Write.Conditions.Any(c=>!c.ExpectedRevisionId.HasValue))
            throw new ArgumentException("Conditional deletion requires existing revisions.");
        var graph=HolonGraphWritePlan.Create(request.Roots,request.SaveChildren,request.Recursive,request.MaxChildDepth,0);
        if(!graph.Nodes.Select(n=>n.Snapshot.Id).ToHashSet().SetEquals(request.Write.Conditions.Select(c=>c.HolonId)))
            throw new ArgumentException("Exactly one condition is required for each selected record.");
        if(request.SubmittedRelationships.Count>0)
        {
            var selected=graph.Nodes.Where(node=>node.SelectsChildren).Select(node=>node.Snapshot.Id).ToHashSet();
            if(!selected.SetEquals(request.SubmittedRelationships.Keys))
                throw new ArgumentException("Submitted roles differ from the selected relationship parents.");
            for(var index=0;index<graph.Nodes.Count;index++)
            {
                var node=graph.Nodes[index];
                if(!node.SelectsChildren)continue;
                var manifest=request.SubmittedRelationships[node.Snapshot.Id];
                if(manifest.ParentId!=node.Snapshot.Id || !manifest.Roles.SelectMany(role=>role.ChildIds).ToHashSet().SetEquals(node.SelectedChildIds))
                    throw new ArgumentException("Submitted roles differ from the captured relationship graph.");
                graph.Nodes[index]=node with { RelationshipManifest=manifest };
            }
        }
        Nodes=Array.AsReadOnly(graph.Nodes.ToArray());
        var rootSources=new HashSet<IHolon>(graph.Roots,ReferenceEqualityComparer.Instance);
        RootIds=Array.AsReadOnly(Nodes.Where(node=>rootSources.Contains(node.Source)).Select(node=>node.Snapshot.Id).ToArray());
        var edges=Nodes.ToDictionary(node=>node.Snapshot.Id,node=>node.SelectedChildIds);
        ChildIds=new System.Collections.ObjectModel.ReadOnlyDictionary<Guid,IReadOnlyList<Guid>>(edges);
        var records=new JArray();
        foreach(var node in Nodes){
            var intent=StoredRecordJson.Project<Holon>(node.Snapshot);
            intent.ProviderUniqueStorageKey?.Remove(authority); // Only this authority's current key is assigned by this write owner.
            if(intent.ProviderMetaData!=null&&intent.ProviderMetaData.TryGetValue(authority,out var own)){
                own?.Remove(CosmicConcurrency.ProviderRevisionMetadataKey);
                if(own==null||own.Count==0)intent.ProviderMetaData.Remove(authority);
            }
            var values=(JObject)StoredRecordJson.CaptureRuntimeToken(intent);
            values.Remove(nameof(Holon.ChildRoleManifestJson)); // Explicit submitted manifests own relationship intent.
            foreach(var field in new[]{"Version","VersionId","PreviousVersionId","CreatedDate","ModifiedDate","CreatedByAvatarId","ModifiedByAvatarId","CreatedProviderType","InstanceSavedOnProviderType"})values.Remove(field);
            if(intent.ProviderUniqueStorageKey==null||intent.ProviderUniqueStorageKey.Count==0)values.Remove("ProviderUniqueStorageKey");
            records.Add(values);
        }
        // New exact-number domain: old rounded receipts must reconcile, never silently replay.
        var content=new JObject{["format"]=5,["actor"]=request.Write.ActorId,["authority"]=authority.ToString(),["operation"]=request.Operation.ToString(),["children"]=request.SaveChildren,["recursive"]=request.Recursive,["depth"]=request.MaxChildDepth,["roots"]=new JArray(RootIds),["nodes"]=records,["conditions"]=JArray.FromObject(request.Write.Conditions.OrderBy(c=>c.HolonId))};
        if(request.SubmittedRelationships.Count>0)
        {
            content["format"]=6;
            content["relationships"]=new JArray(request.SubmittedRelationships.OrderBy(pair=>pair.Key)
                .Select(pair=>JObject.Parse(HolonChildRoleManifestJson.Serialize(pair.Value))));
        }
        if(enrollment!=null)content["replication"]=new JObject{["format"]=1,["source"]=enrollment.SourceId,["targets"]=new JArray(enrollment.TargetIds)};
        Fingerprint=ComputeFingerprint(content);
    }
    /// <summary>Exact canonical value binding for an already captured logical intent. The caller supplies its domain/version.</summary>
    public static string ComputeFingerprint(JToken content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(content).ToString(Formatting.None))));
    }
    public static ConditionalHolonPlan Create(ConditionalHolonWriteRequest request,ProviderType authority)=>new(request,authority);
    public static ConditionalHolonPlan CreateReplicating(ConditionalHolonWriteRequest request,ProviderType authority,HolonReplicationEnrollment enrollment)
        =>new(request,authority,enrollment ?? throw new ArgumentNullException(nameof(enrollment)));
    private static JToken Canonical(JToken value)
    {
        if(value is JObject obj)return new JObject(obj.Properties().OrderBy(p=>p.Name,StringComparer.Ordinal).Select(p=>new JProperty(p.Name,Canonical(p.Value))));
        if(value is JArray array)return new JArray(array.Select(Canonical));
        if(StoredRecordPayloadEquality.IsNumber(value))return new JRaw(StoredRecordPayloadEquality.CanonicalNumberText(value));
        return value.DeepClone();
    }
}
