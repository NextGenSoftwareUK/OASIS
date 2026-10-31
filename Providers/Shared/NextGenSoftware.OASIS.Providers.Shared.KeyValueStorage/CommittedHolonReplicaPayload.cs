using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Versioned transport for an already committed, selected graph. Navigation is carried as explicit edges.</summary>
public static class CommittedHolonReplicaPayload
{
    private static readonly JsonSerializerSettings EnvelopeSettings = new()
    { TypeNameHandling = TypeNameHandling.None, MissingMemberHandling = MissingMemberHandling.Error };

    public static string Capture(CommittedHolonReplicaRequest request)
    {
        var plan = CommittedHolonReplicaPlan.Create(request, request.SourceAuthority);
        return JsonConvert.SerializeObject(new Envelope {
            Format = 1, OperationId = request.OperationId, SourceId = request.SourceId,
            SourceAuthority = request.SourceAuthority, ActorId = request.ActorId, Operation = request.Operation,
            SaveChildren = request.SaveChildren, Recursive = request.Recursive, MaxChildDepth = request.MaxChildDepth,
            Roots = plan.RootIds.ToArray(), Nodes = plan.Nodes.Select(node => new Node {
                RecordJson = node.CapturedIntentJson, Children = node.SelectedChildIds.ToArray()
            }).ToArray()
        }, EnvelopeSettings);
    }

    public static CommittedHolonReplicaRequest Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("A committed graph payload is required.");
        try
        {
            using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
            var document = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (reader.Read()) throw new InvalidDataException("Trailing content after committed graph envelope.");
            ValidateWireShape(document);
            var envelope = document.ToObject<Envelope>(JsonSerializer.Create(EnvelopeSettings));
            if (envelope == null || envelope.Format != 1 || envelope.Nodes == null || envelope.Roots == null)
                throw new InvalidDataException("Unsupported or incomplete committed graph envelope.");
            var records = new Dictionary<Guid, IHolon>();
            var edges = new Dictionary<Guid, Guid[]>();
            foreach (var node in envelope.Nodes)
            {
                if (node == null || node.Children == null || string.IsNullOrWhiteSpace(node.RecordJson))
                    throw new InvalidDataException("Incomplete committed graph node.");
                var record = StoredRecordJson.Deserialize<IHolon>(node.RecordJson);
                if (record == null || record.Id == Guid.Empty || !records.TryAdd(record.Id, record))
                    throw new InvalidDataException("Committed graph identities must be nonempty and unique.");
                edges.Add(record.Id, node.Children);
            }
            foreach (var edge in edges)
                records[edge.Key].Children = edge.Value.Select(id => records[id]).ToList();
            var request = new CommittedHolonReplicaRequest(envelope.OperationId, envelope.SourceId,
                envelope.SourceAuthority, envelope.ActorId, envelope.Operation, envelope.Roots.Select(id => records[id]),
                envelope.SaveChildren, envelope.Recursive, envelope.MaxChildDepth);
            var plan = CommittedHolonReplicaPlan.Create(request, request.SourceAuthority);
            if (plan.Nodes.Count != records.Count || plan.Nodes.Any(node =>
                !node.SelectedChildIds.SequenceEqual(edges[node.Source.Id])))
                throw new InvalidDataException("Payload contains unselected nodes or edges.");
            return request;
        }
        catch (Exception error) when (error is JsonException || error is ArgumentException || error is KeyNotFoundException)
        { throw new InvalidDataException("Invalid committed graph payload.", error); }
    }

    // Typed conversion accepts strings for booleans/enums/numbers and rounds floating-point
    // integers. Validate the producer's versioned token contract before any such conversion.
    private static void ValidateWireShape(JObject document)
    {
        Fields(document,"Format","OperationId","SourceId","SourceAuthority","ActorId","Operation",
            "SaveChildren","Recursive","MaxChildDepth","Roots","Nodes");
        foreach(var name in new[]{"Format","SourceAuthority","Operation","MaxChildDepth"})
            Token(document[name],JTokenType.Integer);
        Token(document["SourceId"],JTokenType.String);
        Token(document["SaveChildren"],JTokenType.Boolean);Token(document["Recursive"],JTokenType.Boolean);
        Identity(document["OperationId"]);Identity(document["ActorId"],allowEmpty:true);
        Identities(document["Roots"]);
        Token(document["Nodes"],JTokenType.Array);
        foreach(var token in (JArray)document["Nodes"])
        {
            if(token is not JObject node)throw new InvalidDataException("Committed graph nodes must be objects.");
            Fields(node,"RecordJson","Children");Token(node["RecordJson"],JTokenType.String);
            Identities(node["Children"]);
        }
    }
    private static void Fields(JObject item,params string[] names)
    {
        if(item.Properties().Count()!=names.Length || names.Any(name=>item.Property(name,StringComparison.Ordinal)==null))
            throw new InvalidDataException("Missing, unknown or incorrectly cased committed graph fields.");
    }
    private static void Token(JToken value,JTokenType type)
    {
        if(value?.Type!=type)throw new InvalidDataException("Committed graph JSON token type does not match the versioned contract.");
    }
    private static void Identity(JToken value,bool allowEmpty=false)
    {
        Token(value,JTokenType.String);
        if(!Guid.TryParseExact(value.Value<string>(),"D",out var id) || !allowEmpty && id==Guid.Empty)
            throw new InvalidDataException("Committed graph identities must use D-format GUID strings and satisfy their empty-value contract.");
    }
    private static void Identities(JToken value)
    {
        Token(value,JTokenType.Array);
        foreach(var id in (JArray)value)Identity(id);
    }

    private sealed class Envelope
    {
        [JsonProperty(Required = Required.Always)]
        public int Format { get; set; }
        [JsonProperty(Required = Required.Always)]
        public Guid OperationId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public string SourceId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public ProviderType SourceAuthority { get; set; }
        [JsonProperty(Required = Required.Always)]
        public Guid ActorId { get; set; }
        [JsonProperty(Required = Required.Always)]
        public ConditionalHolonOperation Operation { get; set; }
        [JsonProperty(Required = Required.Always)]
        public bool SaveChildren { get; set; }
        [JsonProperty(Required = Required.Always)]
        public bool Recursive { get; set; }
        [JsonProperty(Required = Required.Always)]
        public int MaxChildDepth { get; set; }
        [JsonProperty(Required = Required.Always)]
        public Guid[] Roots { get; set; }
        [JsonProperty(Required = Required.Always)]
        public Node[] Nodes { get; set; }
    }
    private sealed class Node
    {
        [JsonProperty(Required = Required.Always)]
        public string RecordJson { get; set; }
        [JsonProperty(Required = Required.Always)]
        public Guid[] Children { get; set; }
    }
}
