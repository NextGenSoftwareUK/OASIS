using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Detached exact acknowledgements for a provider's atomic operation receipt.
/// The provider must atomically bind this payload to its request fingerprint and native writes.</summary>
public static class ConditionalHolonReceipt
{
    public static string Write(IEnumerable<IHolon> acknowledgedRoots)
        =>Write(acknowledgedRoots,Guid.Empty,string.Empty);
    public static string Write(IEnumerable<IHolon> acknowledgedRoots,ConditionalHolonPlan plan)
        =>Write(acknowledgedRoots,(IAtomicHolonWritePlan)plan);
    public static string Write(IEnumerable<IHolon> acknowledgedRoots,IAtomicHolonWritePlan plan)
        =>Write(acknowledgedRoots,(plan??throw new ArgumentNullException(nameof(plan))).OperationId,plan.Fingerprint);
    public static string Write(IEnumerable<IHolon> acknowledgedRoots,ConditionalHolonPlan plan,HolonCommittedDelta delta)
    {
        ArgumentNullException.ThrowIfNull(plan);ArgumentNullException.ThrowIfNull(delta);
        var json=Write(acknowledgedRoots,plan.OperationId,plan.Fingerprint,delta);
        _=Read(json,plan,out _);return json;
    }
    private static string Write(IEnumerable<IHolon> acknowledgedRoots,Guid operation,string intent,HolonCommittedDelta delta=null)
    {
        var graph=HolonGraphWritePlan.Create(acknowledgedRoots,true,true,0,0);
        if(graph.Nodes.Any(n=>n.Source.Id==Guid.Empty))throw new InvalidDataException("A receipt cannot invent acknowledged identities.");
        var nodes=new JArray(graph.Nodes.Select(n=>new JObject{
            ["id"]=n.Source.Id,["record"]=StoredRecordJson.Serialize(n.Snapshot),
            ["children"]=new JArray((n.Source.Children??Enumerable.Empty<IHolon>()).Select(c=>c.Id))}));
        var document=new JObject{["format"]=delta==null?3:4,["operation"]=operation,["intent"]=intent,["roots"]=new JArray(graph.Roots.Select(r=>r.Id)),["nodes"]=nodes};
        if(delta!=null)document["delta"]=delta.StoredJson;
        document["digest"]=Digest(document);return document.ToString(Formatting.None);
    }
    public static IReadOnlyList<IHolon> Read(string json)
    {
        try{return ReadDocument(json);}
        catch(Exception cause)when(cause is JsonException or ArgumentException or InvalidCastException
            or FormatException or OverflowException or InvalidOperationException){
            throw new InvalidDataException("Stored conditional receipt is invalid.",cause);
        }
    }
    private static IReadOnlyList<IHolon> ReadDocument(string json)
    {
        var document=ParseEnvelope(json);
        var format=document["format"]?.Type==JTokenType.Integer?document.Value<int>("format"):0;
        if(format is not(3 or 4)||document.Count!=(format==4?7:6)||!Guid.TryParse(document.Value<string>("operation"),out var operation)||document["intent"]?.Type!=JTokenType.String||document["nodes"] is not JArray nodes||document["roots"] is not JArray roots||document["digest"]?.Type!=JTokenType.String||format==4&&document["delta"]?.Type!=JTokenType.String)
            throw new InvalidDataException("Invalid conditional receipt envelope.");
        if(!string.Equals(document.Value<string>("digest"),Digest(document),StringComparison.Ordinal))throw new InvalidDataException("Conditional receipt integrity check failed.");
        if(format==4&&HolonCommittedDelta.ReadStored(document.Value<string>("delta")).OperationId!=operation)
            throw new InvalidDataException("Committed delta belongs to another receipt operation.");
        var records=new Dictionary<Guid,IHolon>();var links=new Dictionary<Guid,Guid[]>();
        foreach(var node in nodes){
            var id=Identity(node["id"]);
            if(node["record"]?.Type!=JTokenType.String||node["children"] is not JArray children)throw new InvalidDataException("Invalid receipt node.");
            var record=StoredRecordJson.DeserializeStoredHolon(node.Value<string>("record"));
            if(record==null||record.Id!=id||!records.TryAdd(id,record))throw new InvalidDataException("Receipt node identity is invalid or duplicated.");
            links.Add(id,children.Select(Identity).ToArray());
        }
        IHolon Lookup(Guid id)=>records.TryGetValue(id,out var record)?record:throw new InvalidDataException("Receipt links an absent record.");
        foreach(var pair in links)records[pair.Key].Children=pair.Value.Select(Lookup).ToList();
        var selected=roots.Select(Identity).Select(Lookup).ToList();
        var validated=HolonGraphWritePlan.Create(selected,true,true,0,0);
        if(validated.Nodes.Count!=records.Count)throw new InvalidDataException("Receipt contains unreachable records.");
        return selected.AsReadOnly();
    }
    public static IReadOnlyList<IHolon> Read(string json,ConditionalHolonPlan plan)
        =>Read(json,plan,out _);
    public static IReadOnlyList<IHolon> Read(string json,ConditionalHolonPlan plan,out HolonCommittedDelta delta)
    {
        var roots=ReadBoundGraph(json,plan,out delta);
        var graph=HolonGraphWritePlan.Create(roots,true,true,0,0);
        foreach(var node in graph.Nodes){
            var row=node.Source;var revision=CosmicConcurrency.GetProviderRevisionId(row,plan.Authority);var expected=plan.ExpectedRevisions[row.Id];
            if(row.Version<1||row.VersionId==Guid.Empty||!revision.HasValue||revision.Value==Guid.Empty||revision.Value==row.VersionId||expected.HasValue&&revision.Value==expected.Value)
                throw new InvalidDataException("Receipt has an invalid acknowledged logical/provider revision.");
            if(!expected.HasValue&&(row.Version!=1||row.PreviousVersionId!=Guid.Empty))throw new InvalidDataException("Create-only receipt is not an initial acknowledgement.");
            if(plan.Operation!=ConditionalHolonOperation.Save&&!row.IsDeleted)throw new InvalidDataException("Delete receipt has no acknowledged tombstone.");
        }
        return roots;
    }
    public static IReadOnlyList<IHolon> Read(string json,IAtomicHolonWritePlan plan)
    {
        if(plan is ConditionalHolonPlan conditional)return Read(json,conditional);
        if(plan is not CommittedHolonReplicaPlan replica)throw new ArgumentException("Unknown atomic holon intent.",nameof(plan));
        var roots=ReadBoundGraph(json,plan,out _);
        foreach(var node in HolonGraphWritePlan.Create(roots,true,true,0,0).Nodes)replica.ValidateAcknowledgedRecord(node.Source);
        return roots;
    }
    private static IReadOnlyList<IHolon> ReadBoundGraph(string json,IAtomicHolonWritePlan plan,out HolonCommittedDelta delta)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var context=ParseEnvelope(json);
        if(context["operation"]?.Type!=JTokenType.String||context["intent"]?.Type!=JTokenType.String
            ||context.Value<string>("operation")!=plan.OperationId.ToString()||context.Value<string>("intent")!=plan.Fingerprint)throw new InvalidDataException("Receipt operation or intent differs from original request.");
        delta=null;
        if(context["format"]?.Type==JTokenType.Integer&&context["format"].ToString()=="4")
        {
            if(context["delta"]?.Type!=JTokenType.String)throw new InvalidDataException("Missing committed delta.");
            delta=HolonCommittedDelta.ReadStored(context.Value<string>("delta"));
            if(plan is not ConditionalHolonPlan primary || delta.ActorId!=primary.ActorId || delta.Authority!=primary.Authority || delta.Operation!=primary.Operation || delta.OperationId!=primary.OperationId)
                throw new InvalidDataException("Committed delta belongs to another write context.");
        }
        var roots=Read(json);
        if(!roots.Select(r=>r.Id).SequenceEqual(plan.RootIds))throw new InvalidDataException("Receipt roots do not match original request.");
        var graph=HolonGraphWritePlan.Create(roots,true,true,0,0);
        if(!graph.Nodes.Select(n=>n.Source.Id).ToHashSet().SetEquals(plan.Nodes.Select(n=>n.Snapshot.Id)))throw new InvalidDataException("Receipt selected identities differ from original request.");
        foreach(var node in graph.Nodes)if(!(node.Source.Children??Enumerable.Empty<IHolon>()).Select(c=>c.Id).SequenceEqual(plan.ChildIds[node.Source.Id]))throw new InvalidDataException("Receipt topology differs from original request.");
        if(delta!=null&&plan is ConditionalHolonPlan edit)
            delta.ValidateAcknowledgement(edit.OperationId,edit.ActorId,edit.Authority,edit.Operation,graph.Nodes.Select(node=>node.Source),
                edit.ExpectedRevisions.Select(pair=>new HolonWriteCondition(pair.Key,pair.Value)));
        return roots;
    }
    private static JObject ParseEnvelope(string json)
    {
        try
        {
            using var input=new StringReader(json??throw new ArgumentNullException(nameof(json)));
            using var reader=new JsonTextReader(input){DateParseHandling=DateParseHandling.None,MaxDepth=32};
            var value=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            if(reader.Read())throw new InvalidDataException("Unexpected data after conditional receipt.");
            return value;
        }
        catch(Exception cause)when(cause is JsonException or ArgumentException)
        {throw new InvalidDataException("Invalid conditional receipt envelope.",cause);}
    }
    // Integrity checksum, NOT a keyed signature or protection against an authorized database attacker.
    private static string Digest(JObject document)
    {
        var body=new JObject{["format"]=document["format"]?.DeepClone(),["operation"]=document["operation"]?.DeepClone(),["intent"]=document["intent"]?.DeepClone(),["roots"]=document["roots"]?.DeepClone(),["nodes"]=document["nodes"]?.DeepClone()};
        if(document.Value<int>("format")==4)body["delta"]=document["delta"]?.DeepClone();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.ToString(Formatting.None))));
    }
    private static Guid Identity(JToken value)=>Guid.TryParse(value?.Value<string>(),out var id)&&id!=Guid.Empty?id:throw new InvalidDataException("Receipt identity must be a nonempty GUID.");
}
