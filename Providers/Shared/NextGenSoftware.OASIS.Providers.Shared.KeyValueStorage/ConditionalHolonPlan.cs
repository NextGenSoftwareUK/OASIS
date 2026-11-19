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
    public Guid ActorId {get;}
    public ProviderType Authority {get;}
    public ConditionalHolonOperation Operation {get;}
    public IReadOnlyDictionary<Guid,Guid?> ExpectedRevisions {get;}
    public HolonReplicationEnrollment ReplicationEnrollment {get;}
    public bool SaveChildren {get;}
    public bool Recursive {get;}
    public int MaxChildDepth {get;}
    public ConditionalHolonPropertyWriteRequest PropertyWrite {get;}
    private ConditionalHolonPlan(ConditionalHolonWriteRequest request,ProviderType authority,HolonReplicationEnrollment enrollment=null,
        ConditionalHolonPropertyWriteRequest propertyWrite=null)
    {
        ArgumentNullException.ThrowIfNull(request);
        if(propertyWrite!=null && !ReferenceEquals(propertyWrite.WriteRequest,request))
            throw new ArgumentException("Selected fields belong to another conditional request.");
        PropertyWrite=propertyWrite;
        if(request.Write.ReplicationEnrollment!=null && enrollment==null)
            throw new ArgumentException("This conditional transaction owner does not support atomic replication enrollment.");
        if(enrollment!=null && request.Write.ReplicationEnrollment is { } embedded
            && (embedded.SourceId!=enrollment.SourceId || !embedded.TargetIds.SequenceEqual(enrollment.TargetIds)
                || embedded.SourceStorageIdentity?.BindingFingerprint!=enrollment.SourceStorageIdentity?.BindingFingerprint
                || embedded.NativeRoutes.Count!=enrollment.NativeRoutes.Count))
            throw new ArgumentException("Explicit and embedded replication enrollment differ.");
        ReplicationEnrollment=enrollment;SaveChildren=request.SaveChildren;Recursive=request.Recursive;MaxChildDepth=request.MaxChildDepth;
        OperationId=request.Write.OperationId;
        ActorId=request.Write.ActorId;
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
        // Submitted roles bind the logical graph; transport Children may be flattened.
        // Validate the bound intent, not a recaptured transport-only role assignment.
        foreach(var node in graph.Nodes.Where(node=>node.SelectsChildren))
            HolonChangeTracking.ValidateChildSelectionForConditionalWrite(node.Source,node.RelationshipManifest);
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
            if(propertyWrite?.Selections.TryGetValue(node.Snapshot.Id,out var selectedPaths)==true)
            {
                var selectedValues=new JArray();
                foreach(var path in selectedPaths)
                {
                    var exists=SelectedReceiptValue(values,path,out var value);
                    var field=new JObject { ["path"]=path, ["exists"]=exists };
                    if(exists)field["value"]=value.DeepClone();
                    selectedValues.Add(field);
                }
                // An unselected authoritative sibling is not part of this write's intent.
                // Identity, parent edge and the conditional/graph envelope remain bound.
                values=new JObject { ["id"]=node.Snapshot.Id, ["parent"]=intent.ParentHolonId,
                    ["selectedValues"]=selectedValues };
            }
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
        if(enrollment?.SourceStorageIdentity!=null)
        {
            content["replication"]["format"]=2;
            content["replication"]["sourceStorageBinding"]=enrollment.SourceStorageIdentity.BindingFingerprint;
        }
        if(enrollment?.NativeRoutes.Count > 0)
        {
            content["replication"]["format"]=3;
            content["replication"]["routes"]=JObject.Parse(HolonReplicationRouteDescriptors.Capture(enrollment.NativeRoutes));
        }
        if(propertyWrite!=null)
        {
            content["format"]=8; // Selected-value domain; old full-record bindings must not silently replay.
            content["propertyWrites"]=JObject.Parse(propertyWrite.CanonicalSelectionJson);
        }
        Fingerprint=ComputeFingerprint(content);
    }
    /// <summary>Apply an explicit scalar selection to the native before-image after revision/permission admission.
    /// Records not selected by this request keep their explicit full-save semantics.
    /// Parent-edge changes require a graph write rather than an implicit scalar selection.</summary>
    public Holon ApplySelectedValues(Holon authoritative,Holon submitted)
        =>ApplyPropertyWriteSelection(PropertyWrite,authoritative,submitted);
    public static Holon ApplyPropertyWriteSelection(ConditionalHolonPropertyWriteRequest selection,Holon authoritative,Holon submitted)
    {
        ArgumentNullException.ThrowIfNull(submitted);
        if(selection==null || !selection.Selections.ContainsKey(submitted.Id))return submitted;
        if(authoritative==null || authoritative.ParentHolonId!=submitted.ParentHolonId)
            throw new ArgumentException("Selected scalar writes require an existing record with the same parent edge.");
        var merged=selection.ApplyToAuthoritative(authoritative,submitted);
        if(merged.IsError)throw new System.IO.InvalidDataException(merged.Message,merged.Exception);
        return (Holon)merged.Result;
    }
    /// <summary>Validate selected after-image values, including legitimate absence.
    /// Unlike write admission, two missing leaves are equal receipt evidence, not a new invalid write.
    /// This comparison does not replace the receipt's identity/revision/graph/delta integrity checks.</summary>
    public static void ValidateSelectedAcknowledgement(ConditionalHolonPropertyWriteRequest selection,Holon submitted,Holon acknowledged)
    {
        ArgumentNullException.ThrowIfNull(selection);ArgumentNullException.ThrowIfNull(submitted);ArgumentNullException.ThrowIfNull(acknowledged);
        if(submitted.Id!=acknowledged.Id || !selection.Selections.TryGetValue(submitted.Id,out var paths))
            throw new System.IO.InvalidDataException("Acknowledged selection belongs to another record.");
        var expected=(JObject)StoredRecordJson.CaptureRuntimeToken(submitted);
        var actual=(JObject)StoredRecordJson.CaptureRuntimeToken(acknowledged);
        foreach(var path in paths)
        {
            var before=SelectedReceiptValue(expected,path,out var left);
            var after=SelectedReceiptValue(actual,path,out var right);
            if(before!=after || before && !StoredRecordValueEquality.Equivalent(left,right))
                throw new System.IO.InvalidDataException("Acknowledged selected values differ from their submitted intent.");
        }
    }
    private static bool SelectedReceiptValue(JObject root,string path,out JToken value)
    {
        JToken current=root;
        foreach(var encoded in path.Substring(1).Split('/'))
        {
            if(current is not JObject owner)
                throw new System.IO.InvalidDataException("Acknowledged selected-property ancestry is invalid.");
            var segment=encoded.Replace("~1","/").Replace("~0","~");
            var property=owner.Property(segment,StringComparison.Ordinal);
            if(property==null){value=null;return false;}
            current=property.Value;
        }
        value=current;return true;
    }
    /// <summary>Exact canonical value binding for an already captured logical intent. The caller supplies its domain/version.</summary>
    public static string ComputeFingerprint(JToken content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(content).ToString(Formatting.None))));
    }
    public static ConditionalHolonPlan Create(ConditionalHolonWriteRequest request,ProviderType authority)=>new(request,authority);
    /// <summary>Bind explicit scalar selection to the same immutable graph, actor, authority, revisions and enrollment.
    /// Native owners must apply it to their authoritative before-images; normal Create never infers selection.</summary>
    public static ConditionalHolonPlan CreatePropertyWrite(ConditionalHolonPropertyWriteRequest request,ProviderType authority)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(request.WriteRequest,authority,request.WriteRequest.Write.ReplicationEnrollment,request);
    }
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
