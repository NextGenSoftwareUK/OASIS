using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

public sealed class CommittedHolonReplicaPlan : IAtomicHolonWritePlan
{
    public IReadOnlyList<PlannedHolonWrite> Nodes {get;}
    public IReadOnlyList<Guid> RootIds {get;}
    public IReadOnlyDictionary<Guid,IReadOnlyList<Guid>> ChildIds {get;}
    public string Fingerprint {get;}
    public Guid OperationId {get;}
    public ProviderType Authority {get;}
    public ConditionalHolonOperation Operation {get;}
    public string SourceId {get;}
    public ProviderType SourceAuthority {get;}
    private readonly IReadOnlyDictionary<Guid,string> _content;

    private CommittedHolonReplicaPlan(CommittedHolonReplicaRequest request,ProviderType authority,Action<IHolon> validateSource=null)
    {
        ArgumentNullException.ThrowIfNull(request);CosmicConcurrency.ValidateAuthority(authority);
        OperationId=request.OperationId;Authority=authority;Operation=request.Operation;
        SourceId=request.SourceId;SourceAuthority=request.SourceAuthority;
        var graph=HolonGraphWritePlan.Create(request.Roots,request.SaveChildren,request.Recursive,request.MaxChildDepth,0,validateSource);
        var content=new Dictionary<Guid,string>();var edges=new Dictionary<Guid,IReadOnlyList<Guid>>();
        foreach(var node in graph.Nodes)
        {
            var row=node.Snapshot;
            if(node.Source.Id==Guid.Empty || row.Version<1 || row.VersionId==Guid.Empty
                || (row.Version==1 ? row.PreviousVersionId!=Guid.Empty : row.PreviousVersionId==Guid.Empty || row.PreviousVersionId==row.VersionId))
                throw new ArgumentException("Replica content requires committed identities and coherent logical lineage.");
            if(Operation!=ConditionalHolonOperation.Save && (!row.IsDeleted || row.IsActive || row.Version<2))
                throw new ArgumentException("Replica deletion requires a committed versioned tombstone.");
            var sourceToken=CosmicConcurrency.GetProviderRevisionId(row,SourceAuthority);
            if(!sourceToken.HasValue || sourceToken.Value==Guid.Empty || sourceToken.Value==row.VersionId)
                throw new ArgumentException("Replica content requires its committed source provider revision.");
            content.Add(row.Id,CanonicalContent(row,authority));
            edges.Add(row.Id,node.SelectedChildIds);
        }
        Nodes=Array.AsReadOnly(graph.Nodes.ToArray());
        var rootSources=new HashSet<IHolon>(graph.Roots,ReferenceEqualityComparer.Instance);
        RootIds=Array.AsReadOnly(Nodes.Where(node=>rootSources.Contains(node.Source)).Select(node=>node.Snapshot.Id).ToArray());
        ChildIds=new ReadOnlyDictionary<Guid,IReadOnlyList<Guid>>(edges);_content=new ReadOnlyDictionary<Guid,string>(content);
        var intent=new JObject{["format"]=1,["kind"]="committed-holon-replica",["source"]=SourceId,
            ["sourceAuthority"]=SourceAuthority.ToString(),["targetAuthority"]=Authority.ToString(),["actor"]=request.ActorId,
            ["operation"]=Operation.ToString(),["roots"]=new JArray(RootIds),["saveChildren"]=request.SaveChildren,
            ["recursive"]=request.Recursive,["depth"]=request.MaxChildDepth,
            ["nodes"]=new JArray(Nodes.Select(node=>new JObject{["record"]=JObject.Parse(content[node.Snapshot.Id]),
                ["children"]=new JArray(edges[node.Snapshot.Id])}))};
        Fingerprint=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(intent).ToString(Formatting.None))));
    }
    public static CommittedHolonReplicaPlan Create(CommittedHolonReplicaRequest request,ProviderType authority)=>new(request,authority);
    public static CommittedHolonReplicaPlan Create(CommittedHolonReplicaRequest request,ProviderType authority,Action<IHolon> validateSource)=>new(request,authority,validateSource);
    public void ValidateAcknowledgedRecord(IHolon record)
    {
        if(!_content.TryGetValue(record.Id,out var expected)
            || PortableValues(JObject.Parse(CanonicalContent(record,Authority))).ToString(Formatting.None)
                !=PortableValues(JObject.Parse(expected)).ToString(Formatting.None))
            throw new InvalidDataException("Replica acknowledgement changed committed logical content, lineage or audit.");
        var token=CosmicConcurrency.GetProviderRevisionId(record,Authority);
        if(!token.HasValue || token.Value==Guid.Empty || token.Value==record.VersionId)
            throw new InvalidDataException("Replica acknowledgement has no valid target-owned provider revision.");
    }
    // Native keys/revisions and the target's metadata namespace are local observations, not portable logical content.
    private static string CanonicalContent(IHolon record,ProviderType target)
    {
        var values=JObject.Parse(StoredRecordJson.Serialize<IHolon>(record));
        foreach(var field in new[]{"ProviderUniqueStorageKey","PreviousVersionProviderUniqueStorageKey","InstanceSavedOnProviderType"})values.Remove(field);
        if(values["ProviderMetaData"] is JObject providers)
        {
            providers.Property(target.ToString())?.Remove();
            foreach(var provider in providers.Properties().ToArray())
                if(provider.Value is JObject fields)
                {fields.Remove(CosmicConcurrency.ProviderRevisionMetadataKey);if(!fields.Properties().Any())provider.Remove();}
            if(!providers.Properties().Any())values.Remove("ProviderMetaData");
        }
        return Canonical(values).ToString(Formatting.None);
    }
    // Acknowledged values cross CLR/JSON/BSON implementations. Collection implementation
    // type tags are serialization detail; element/custom-object types and values remain bound.
    // Keep the original intent fingerprint bytes unchanged for exact receipt replay.
    private static JToken PortableValues(JToken token)
    {
        if(token is JArray array)return new JArray(array.Select(PortableValues));
        if(token is not JObject obj)return token.DeepClone();
        var type=obj.Value<string>("$type");
        var arrayImplementation=type!=null && IsArrayImplementation(type);
        var dictionary=type!=null && !arrayImplementation && (type.StartsWith("System.Collections.Generic.Dictionary`2[[System.String,",StringComparison.Ordinal)
            || type.StartsWith("System.Collections.ObjectModel.ReadOnlyDictionary`2[[System.String,",StringComparison.Ordinal));
        var sequence=type!=null && (type.StartsWith("System.Collections.Generic.List`1[",StringComparison.Ordinal)
            || type.StartsWith("System.Collections.ObjectModel.ObservableCollection`1[",StringComparison.Ordinal)
            || type.StartsWith("System.Collections.ObjectModel.Collection`1[",StringComparison.Ordinal)
            || type.StartsWith("System.Collections.ObjectModel.ReadOnlyCollection`1[",StringComparison.Ordinal)
            || arrayImplementation);
        if(!dictionary && sequence && obj.Count==2 && obj["$values"] is JArray values)
            return PortableValues(values);
        return new JObject(obj.Properties().Where(property=>!dictionary || property.Name!="$type")
            .OrderBy(property=>property.Name,StringComparer.Ordinal)
            .Select(property=>new JProperty(property.Name,PortableValues(property.Value))));
    }

    private static bool IsArrayImplementation(string type)
    {
        var depth=0;
        for(var index=0;index<type.Length;index++)
        {
            if(type[index]=='[')depth++;
            else if(type[index]==']')depth--;
            else if(type[index]==',' && depth==0)return type.Substring(0,index).EndsWith("[]",StringComparison.Ordinal);
        }
        return type.EndsWith("[]",StringComparison.Ordinal);
    }

    private static JToken Canonical(JToken token)=>token is JObject obj
        ?new JObject(obj.Properties().OrderBy(property=>property.Name,StringComparer.Ordinal).Select(property=>new JProperty(property.Name,Canonical(property.Value))))
        :token is JArray array ? new JArray(array.Select(Canonical)):token.DeepClone();
}
