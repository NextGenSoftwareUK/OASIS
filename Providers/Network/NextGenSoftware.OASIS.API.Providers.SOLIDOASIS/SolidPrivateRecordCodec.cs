using System;
using System.IO;
using System.Linq;
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
    public SolidPrivateRecordCodec(Uri podRoot,byte[] storageKey)
    {
        _root=NormalizePodRoot(podRoot);
        _encryption=new EncryptedRecordCodec(storageKey);
    }
    internal static Uri NormalizePodRoot(Uri podRoot)
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
        var resource=Resource<T>(record.Id);
        // The existing canonical codec owns interface types and relationship-by-ID semantics.
        var ciphertext=_encryption.Encrypt(AssociatedData<T>(resource),StoredRecordJson.Serialize(record));
        using var graph=new Graph();
        var subject=graph.CreateUriNode(resource);
        graph.Assert(new Triple(subject,graph.CreateUriNode(new Uri(RdfType)),graph.CreateUriNode(new Uri(Namespace+"EncryptedRecordV1"))));
        graph.Assert(new Triple(subject,graph.CreateUriNode(new Uri(Namespace+"payload")),graph.CreateLiteralNode(ciphertext)));
        using var output=new System.IO.StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        new CompressingTurtleWriter().Save(graph,output);
        return output.ToString();
    }

    public T Decode<T>(string turtle,Guid expectedId) where T:class,IHolonBase
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
        var record=StoredRecordJson.DeserializeStored<T>(_encryption.Decrypt(AssociatedData<T>(resource),literal.Value));
        if(record==null||record.Id!=expectedId)throw new InvalidDataException("Decrypted record identity does not match its resource.");
        return record;
    }
    public void Dispose()=>_encryption.Dispose();
}
