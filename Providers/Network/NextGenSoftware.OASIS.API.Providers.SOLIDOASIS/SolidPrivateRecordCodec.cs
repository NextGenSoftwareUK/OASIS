using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace NextGenSoftware.OASIS.API.Providers.SOLIDOASIS;

/// <summary>Encrypted canonical record in a closed RDF envelope. Caller owns durable key storage.
/// Does not implement pod HTTP/auth/indexing or authorize exposing resource identifiers.</summary>
public sealed class SolidPrivateRecordCodec : IDisposable
{
    private const string Namespace="https://oasisomniverse.one/ns/";
    private const string RdfType="http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
    private readonly Uri _root;
    private readonly EncryptedRecordCodec _encryption;
    private static readonly EmbeddedRecordHistory History=new();
    public bool HasHistory<T>(string turtle,Guid id) where T:class,IHolonBase
        =>DecodeDocument<T>(turtle,id)[EmbeddedRecordHistory.PropertyName]!=null;
    public bool PayloadEquals<T>(string expected,string actual,Guid id) where T:class,IHolonBase
        =>StoredRecordPayloadEquality.Equivalent(DecodeDocument<T>(expected,id),DecodeDocument<T>(actual,id));
    public SolidPrivateRecordCodec(Uri podRoot,byte[] storageKey)
    {
        _root=NormalizePodRoot(podRoot);
        _encryption=new EncryptedRecordCodec(storageKey);
    }
    public static Uri NormalizePodRoot(Uri podRoot)
    {
        ArgumentNullException.ThrowIfNull(podRoot);
        if(!podRoot.IsAbsoluteUri||(podRoot.Scheme!=Uri.UriSchemeHttps&&!(podRoot.Scheme==Uri.UriSchemeHttp&&podRoot.IsLoopback))
            ||!string.IsNullOrEmpty(podRoot.UserInfo)||!string.IsNullOrEmpty(podRoot.Query)||!string.IsNullOrEmpty(podRoot.Fragment))
            throw new ArgumentException("An absolute HTTPS pod root (or loopback HTTP) without credentials/query/fragment is required.",nameof(podRoot));
        return new Uri(podRoot.AbsoluteUri.TrimEnd('/')+"/");
    }

    private static string Kind<T>() where T:class,IHolonBase
    {
        if(typeof(IAvatar).IsAssignableFrom(typeof(T)))return "avatars";
        if(typeof(IAvatarDetail).IsAssignableFrom(typeof(T)))return "avatar-details";
        if(typeof(IHolon).IsAssignableFrom(typeof(T)))return "holons";
        throw new ArgumentException("A supported avatar, avatar-detail or holon contract is required.");
    }
    public Uri Resource<T>(Guid id) where T:class,IHolonBase
    {
        if(id==Guid.Empty)throw new ArgumentException("A nonempty record identity is required.",nameof(id));
        return new Uri(_root,$"oasis/{Kind<T>()}/{id:D}.ttl");
    }
    public Uri Container<T>() where T:class,IHolonBase=>new Uri(_root,$"oasis/{Kind<T>()}/");
    private static string AssociatedData<T>(Uri resource) where T:class,IHolonBase
        =>"oasis-solid-private-record-v1/"+Kind<T>()+"/"+resource.AbsoluteUri;

    public string Encode<T>(T record) where T:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(record);
        return EncodePayload<T>(record.Id,StoredRecordJson.Serialize(record));
    }

    public async Task<(T Record,string Turtle)> StageVersionAsync<T>(T proposed,string previousTurtle=null) where T:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(proposed);
        var previous=previousTurtle==null?null:DecodeDocument<T>(previousTurtle,proposed.Id);
        var document=await History.StageAsync(proposed,previous,StoredRecordJson.DeserializeStored<T>,record=>StoredRecordJson.Serialize(record));
        var record=await History.ReadAsync(proposed.Id,document,0,StoredRecordJson.DeserializeStored<T>);
        return (record,EncodePayload<T>(proposed.Id,document.ToString(Newtonsoft.Json.Formatting.None)));
    }

    private string EncodePayload<T>(Guid id,string json) where T:class,IHolonBase
    {
        var resource=Resource<T>(id);
        // The existing canonical codec owns interface types and relationship-by-ID semantics.
        var ciphertext=_encryption.Encrypt(AssociatedData<T>(resource),json);
        using var graph=new Graph();
        var subject=graph.CreateUriNode(resource);
        graph.Assert(new Triple(subject,graph.CreateUriNode(new Uri(RdfType)),graph.CreateUriNode(new Uri(Namespace+"EncryptedRecordV1"))));
        graph.Assert(new Triple(subject,graph.CreateUriNode(new Uri(Namespace+"payload")),graph.CreateLiteralNode(ciphertext)));
        using var output=new System.IO.StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        new CompressingTurtleWriter().Save(graph,output);
        return output.ToString();
    }

    public T Decode<T>(string turtle,Guid expectedId) where T:class,IHolonBase
        =>DecodeVersionAsync<T>(turtle,expectedId,0).GetAwaiter().GetResult();

    public Task<T> DecodeVersionAsync<T>(string turtle,Guid expectedId,int version) where T:class,IHolonBase
    {
        if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
        return History.ReadAsync(expectedId,DecodeDocument<T>(turtle,expectedId),version,StoredRecordJson.DeserializeStored<T>);
    }

    public System.Collections.Generic.IReadOnlyList<T> DecodeAllVersions<T>(string turtle,Guid expectedId) where T:class,IHolonBase
        =>History.ReadAll(expectedId,DecodeDocument<T>(turtle,expectedId),StoredRecordJson.DeserializeStored<T>);

    private JObject DecodeDocument<T>(string turtle,Guid expectedId) where T:class,IHolonBase
    {
        var resource=Resource<T>(expectedId);
        using var graph=new Graph{BaseUri=resource};
        using var input=new StringReader(turtle??throw new ArgumentNullException(nameof(turtle)));
        new TurtleParser().Load(graph,input);
        var subject=graph.CreateUriNode(resource);
        var type=graph.GetTriplesWithSubjectPredicate(subject,graph.CreateUriNode(new Uri(RdfType))).ToArray();
        var payload=graph.GetTriplesWithSubjectPredicate(subject,graph.CreateUriNode(new Uri(Namespace+"payload"))).ToArray();
        if(graph.Triples.Count!=2||type.Length!=1||type[0].Object is not IUriNode marker||marker.Uri.AbsoluteUri!=Namespace+"EncryptedRecordV1"
            ||payload.Length!=1||payload[0].Object is not ILiteralNode literal)
            throw new InvalidDataException("A single exact encrypted RDF record envelope is required; no plaintext downgrade is permitted.");
        var document=OasisJson.ParseStoredObject(_encryption.Decrypt(AssociatedData<T>(resource),literal.Value));
        var record=StoredRecordJson.DeserializeStored<T>(History.CanonicalPayload(document).ToString(Newtonsoft.Json.Formatting.None));
        if(record==null||record.Id!=expectedId)throw new InvalidDataException("Decrypted record identity does not match its resource.");
        if(document["id"]==null){
            if(document[EmbeddedRecordHistory.PropertyName]!=null)throw new InvalidDataException("Versioned encrypted record is missing its embedded identity.");
            document["id"]=expectedId.ToString();
        }
        return document;
    }
    public void Dispose()=>_encryption.Dispose();
}
