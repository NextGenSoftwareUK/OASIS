using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Integrity-bound physical approval record. Its original policy is provenance,not a current authorization grant.</summary>
public static class HolonDestinationBindingRecord
{
    public static string Capture(HolonReplicationDestinationBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        return new JObject { ["Format"]=1,["EstablishedOperationId"]=binding.EstablishedOperationId.ToString("D"),
            ["RevisionId"]=binding.RevisionId.ToString("D"),
            ["Descriptor"]=JObject.Parse(HolonReplicationRouteDescriptors.Capture(new[]{binding.Route})) }.ToString(Formatting.None);
    }
    public static string Digest(string json)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("cosmic-destination-binding-v1:"+json)));
    public static HolonReplicationDestinationBinding Read(string json,string digest,CosmicStorageIdentity source,ProviderType target)
    {
        ArgumentNullException.ThrowIfNull(source);
        if(string.IsNullOrWhiteSpace(json) || Digest(json)!=digest)
            throw new InvalidDataException("The recorded destination binding checksum is invalid.");
        try
        {
            using var reader=new JsonTextReader(new StringReader(json)){DateParseHandling=DateParseHandling.None};
            var value=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            var names=new[]{"Format","EstablishedOperationId","RevisionId","Descriptor"};
            if(reader.Read() || value.Properties().Count()!=names.Length || names.Any(name=>value.Property(name,StringComparison.Ordinal)==null)
                || value["Format"]?.Type!=JTokenType.Integer || value["Format"].ToString(Formatting.None)!="1"
                || value["Descriptor"] is not JObject descriptor)
                throw new InvalidDataException("Unsupported or malformed destination binding record.");
            var routes=HolonReplicationRouteDescriptors.Read(descriptor.ToString(Formatting.None));
            if(routes.Count!=1 || routes[0].SourceStorage.BindingFingerprint!=source.BindingFingerprint || routes[0].TargetStorage.Authority!=target)
                throw new InvalidDataException("The recorded destination belongs to another native source or target authority.");
            Guid Id(string name)
            {
                var token=value[name];
                if(token?.Type!=JTokenType.String || !Guid.TryParseExact(token.Value<string>(),"D",out var id)
                    || id==Guid.Empty || id.ToString("D")!=token.Value<string>())
                    throw new InvalidDataException("Canonical nonempty binding operation/revision identities are required.");
                return id;
            }
            return new(routes[0],Id("EstablishedOperationId"),Id("RevisionId"));
        }
        catch(Exception cause)when(cause is JsonException || cause is ArgumentException)
        {throw new InvalidDataException("Invalid destination binding record.",cause);}
    }
}

public sealed class HolonDestinationRebindRequiredException : InvalidOperationException
{
    public const string Code="COSMIC_REPLICATION_DESTINATION_REBIND_REQUIRED";
    public HolonDestinationRebindRequiredException():base("The native source already records a different physical destination. Explicit verified rebinding is required.") { }
}
