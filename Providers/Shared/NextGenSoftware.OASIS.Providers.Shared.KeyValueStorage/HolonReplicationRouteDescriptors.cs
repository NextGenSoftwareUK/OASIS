using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Versioned immutable native route meaning. Contains fingerprints and native IDs, never connection secrets.</summary>
public static class HolonReplicationRouteDescriptors
{
    public static string Capture(IEnumerable<HolonReplicationRouteBinding> routes)
    {
        var input = routes?.ToArray() ?? throw new ArgumentNullException(nameof(routes));
        if (input.Length == 0 || input[0] == null)
            throw new ArgumentException("At least one native route is required.",nameof(routes));
        var frozen = new HolonReplicationEnrollment(input[0].SourceId,input.Select(route => route?.TargetId),
            input[0].SourceStorage,input).NativeRoutes;
        return new JObject { ["Format"]=1, ["Routes"]=new JArray(frozen.Select(route => new JObject
        {
            ["Source"]=Storage(route.SourceStorage), ["Target"]=Storage(route.TargetStorage), ["PolicyBinding"]=route.PolicyBinding
        })) }.ToString(Formatting.None);
    }

    public static IReadOnlyList<HolonReplicationRouteBinding> Read(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("Native route descriptors are required.");
        try
        {
            using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling=DateParseHandling.None };
            var document = JObject.Load(reader,new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
            if (reader.Read()) throw new InvalidDataException("Trailing content after native route descriptors.");
            Fields(document,"Format","Routes");
            if (document["Format"]?.Type != JTokenType.Integer || document["Format"].ToString(Formatting.None) != "1"
                || document["Routes"] is not JArray items || items.Count == 0 || items.Count > 256)
                throw new InvalidDataException("Unsupported or incomplete native route descriptors.");
            var routes = items.Select(item =>
            {
                if (item is not JObject route) throw new InvalidDataException("Each native route must be an object.");
                Fields(route,"Source","Target","PolicyBinding");
                return new HolonReplicationRouteBinding(ReadStorage(route["Source"]),ReadStorage(route["Target"]),Text(route["PolicyBinding"]));
            }).ToArray();
            return new HolonReplicationEnrollment(routes[0].SourceId,routes.Select(route => route.TargetId),routes[0].SourceStorage,routes).NativeRoutes;
        }
        catch (Exception cause) when (cause is JsonException || cause is ArgumentException)
        { throw new InvalidDataException("Invalid native route descriptors.",cause); }
    }

    private static JObject Storage(CosmicStorageIdentity value) => new()
    {
        ["Authority"]=value.Authority.ToString(), ["EndpointFingerprint"]=value.EndpointFingerprint, ["StoreId"]=value.StoreId
    };
    private static CosmicStorageIdentity ReadStorage(JToken token)
    {
        if (token is not JObject value) throw new InvalidDataException("Native storage identity must be an object.");
        Fields(value,"Authority","EndpointFingerprint","StoreId");
        var text = Text(value["Authority"]);
        if (!Enum.TryParse<ProviderType>(text,out var authority) || authority.ToString() != text)
            throw new InvalidDataException("Native authority must use its canonical provider name.");
        return new CosmicStorageIdentity(authority,Text(value["EndpointFingerprint"]),Text(value["StoreId"]));
    }
    private static string Text(JToken token) => token?.Type == JTokenType.String ? token.Value<string>()
        : throw new InvalidDataException("Native route identity fields must be strings.");
    private static void Fields(JObject value,params string[] names)
    {
        if (value.Properties().Count() != names.Length || names.Any(name => value.Property(name,StringComparison.Ordinal) == null))
            throw new InvalidDataException("Native route fields are missing,unknown or incorrectly cased.");
    }
}
