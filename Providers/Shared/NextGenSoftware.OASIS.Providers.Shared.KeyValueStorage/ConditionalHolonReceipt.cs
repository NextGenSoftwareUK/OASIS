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
    private static string Write(IEnumerable<IHolon> acknowledgedRoots,Guid operation,string intent)
    {
        var graph=HolonGraphWritePlan.Create(acknowledgedRoots,true,true,0,0);
        if(graph.Nodes.Any(n=>n.Source.Id==Guid.Empty))throw new InvalidDataException("A receipt cannot invent acknowledged identities.");
        var nodes=new JArray(graph.Nodes.Select(n=>new JObject{
            ["id"]=n.Source.Id,["record"]=StoredRecordJson.Serialize(n.Snapshot),
            ["children"]=new JArray((n.Source.Children??Enumerable.Empty<IHolon>()).Select(c=>c.Id))}));
        var document=new JObject{["format"]=3,["operation"]=operation,["intent"]=intent,["roots"]=new JArray(graph.Roots.Select(r=>r.Id)),["nodes"]=nodes};
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
        var document=JObject.Parse(json);
        if(document.Count!=6||document["format"]?.Type!=JTokenType.Integer||document.Value<int>("format")!=3||!Guid.TryParse(document.Value<string>("operation"),out _)||document["intent"]?.Type!=JTokenType.String||document["nodes"] is not JArray nodes||document["roots"] is not JArray roots||document["digest"]?.Type!=JTokenType.String)
            throw new InvalidDataException("Invalid conditional receipt envelope.");
        if(!string.Equals(document.Value<string>("digest"),Digest(document),StringComparison.Ordinal))throw new InvalidDataException("Conditional receipt integrity check failed.");
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
    {
        var roots=ReadBoundGraph(json,plan);
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
        var roots=ReadBoundGraph(json,plan);
        foreach(var node in HolonGraphWritePlan.Create(roots,true,true,0,0).Nodes)replica.ValidateAcknowledgedRecord(node.Source);
        return roots;
    }
    private static IReadOnlyList<IHolon> ReadBoundGraph(string json,IAtomicHolonWritePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);var roots=Read(json);
        var context=JObject.Parse(json);
        if(context.Value<string>("operation")!=plan.OperationId.ToString()||context.Value<string>("intent")!=plan.Fingerprint)throw new InvalidDataException("Receipt operation or intent differs from original request.");
        if(!roots.Select(r=>r.Id).SequenceEqual(plan.RootIds))throw new InvalidDataException("Receipt roots do not match original request.");
        var graph=HolonGraphWritePlan.Create(roots,true,true,0,0);
        if(!graph.Nodes.Select(n=>n.Source.Id).ToHashSet().SetEquals(plan.Nodes.Select(n=>n.Snapshot.Id)))throw new InvalidDataException("Receipt selected identities differ from original request.");
        foreach(var node in graph.Nodes)if(!(node.Source.Children??Enumerable.Empty<IHolon>()).Select(c=>c.Id).SequenceEqual(plan.ChildIds[node.Source.Id]))throw new InvalidDataException("Receipt topology differs from original request.");
        return roots;
    }
    // Integrity checksum, NOT a keyed signature or protection against an authorized database attacker.
    private static string Digest(JObject document)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(new JObject{["format"]=document["format"]?.DeepClone(),["operation"]=document["operation"]?.DeepClone(),["intent"]=document["intent"]?.DeepClone(),["roots"]=document["roots"]?.DeepClone(),["nodes"]=document["nodes"]?.DeepClone()}.ToString(Formatting.None))));
    private static Guid Identity(JToken value)=>Guid.TryParse(value?.Value<string>(),out var id)&&id!=Guid.Empty?id:throw new InvalidDataException("Receipt identity must be a nonempty GUID.");
}
