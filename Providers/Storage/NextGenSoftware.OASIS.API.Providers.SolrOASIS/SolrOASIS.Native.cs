using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SolrOASIS;

public partial class SolrOASIS
{
    private sealed class NativePayloadLimitException : IOException { }

    private static async Task<JsonDocument> ReadNativeResponseAsync(HttpResponseMessage response,
        int maximumBytes, CancellationToken cancellation)
    {
        using var source = await response.Content.ReadAsStreamAsync(cancellation);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await source.ReadAsync(buffer, cancellation)) != 0)
        {
            if (body.Length + count > maximumBytes) throw new NativePayloadLimitException();
            body.Write(buffer, 0, count);
        }
        return JsonDocument.Parse(body.ToArray());
    }

    private static void ValidateNativeStatus(JsonElement root)
    {
        var header = root.GetProperty("responseHeader");
        if (header.GetProperty("status").GetInt32() != 0 || root.TryGetProperty("error", out _) ||
            (header.TryGetProperty("partialResults", out var partial) && partial.ValueKind != JsonValueKind.False) ||
            (header.TryGetProperty("segmentTerminatedEarly", out var terminated) && terminated.ValueKind != JsonValueKind.False))
            throw new InvalidDataException("Native Solr operation failed or returned incomplete results.");
    }

    private async Task EnsureCollectionAsync(string collection)
    {
        // Provisioning is explicit: SolrCloud collection creation and standalone
        // core creation are different admin operations. Activation must never guess.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_baseUrl}/solr/{collection}/schema?wt=json&showDefaults=true&omitHeader=false");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        response.EnsureSuccessStatusCode();
        using var document = await ReadNativeResponseAsync(response, 256*1024, deadline.Token);
        ValidateNativeStatus(document.RootElement);
        var schema = document.RootElement.GetProperty("schema");
        if (schema.GetProperty("uniqueKey").GetString() != "id")
            throw new InvalidDataException("Solr unique key must be id.");
        var fields = schema.GetProperty("fields").EnumerateArray().ToDictionary(field => field.GetProperty("name").GetString()!);
        void Field(string name, string type, bool indexed, bool stored, bool docValues)
        {
            if (!fields.TryGetValue(name, out var field) || field.GetProperty("type").GetString() != type ||
                field.GetProperty("indexed").GetBoolean() != indexed || field.GetProperty("stored").GetBoolean() != stored ||
                field.GetProperty("docValues").GetBoolean() != docValues || field.GetProperty("multiValued").GetBoolean())
                throw new InvalidDataException("Solr field differs from canonical storage contract: " + name);
        }
        foreach (var name in new[]{"id","name","username","email","parentHolonId"}) Field(name,"string",true,true,false);
        Field("data_json","string",false,true,false);
        Field("holonType","pint",true,true,false);
        Field("isDeleted","boolean",true,true,false);
        Field("_version_","plong",false,false,true);
        var types = schema.GetProperty("fieldTypes").EnumerateArray().ToDictionary(type => type.GetProperty("name").GetString()!);
        foreach (var pair in new[]{("string","solr.StrField"),("pint","solr.IntPointField"),("plong","solr.LongPointField"),("boolean","solr.BoolField")})
            if (!types.TryGetValue(pair.Item1,out var type) || type.GetProperty("class").GetString() != pair.Item2)
                throw new InvalidDataException("Solr field type differs from canonical contract: " + pair.Item1);
    }

    private static Dictionary<string, object> ToSolrDoc(object value, string id)
    {
        var json = Ser(value);
        using var payload = JsonDocument.Parse(json);
        if (payload.RootElement.GetProperty("id").GetString() != id)
            throw new InvalidDataException("Payload identity differs from native identity.");
        var document = new Dictionary<string,object> { ["id"] = id, ["data_json"] = json };
        foreach (var name in new[]{"name","username","email","parentHolonId"})
            if (payload.RootElement.TryGetProperty(name,out var field) && field.ValueKind == JsonValueKind.String)
                document[name] = field.GetString()!;
        if (payload.RootElement.TryGetProperty("holonType",out var type)) document["holonType"] = type.GetInt32();
        if (payload.RootElement.TryGetProperty("isDeleted",out var deleted)) document["isDeleted"] = deleted.GetBoolean();
        return document;
    }

    private static T? DecodeDocument<T>(JsonElement document)
    {
        var id = document.GetProperty("id").GetString();
        var json = document.GetProperty("data_json").GetString()!;
        using var payload = JsonDocument.Parse(json);
        if (payload.RootElement.GetProperty("id").GetString() != id)
            throw new InvalidDataException("Native ID differs from canonical stored payload.");
        return JsonSerializer.Deserialize<T>(json,_jsonOpts);
    }

    private async Task UpsertDocAsync(string collection, object value)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var payload = JsonDocument.Parse(Ser(value));
        var id = payload.RootElement.GetProperty("id").GetString();
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_baseUrl}/solr/{collection}/update/json/docs?commit=true&waitSearcher=true&versions=true&wt=json&omitHeader=false")
            {Content=Json(new[]{value})};
        // One native full replacement with hard commit, no follow-up write/replay.
        using var response = await _http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token);
        response.EnsureSuccessStatusCode();
        using var acknowledgement = await ReadNativeResponseAsync(response,65536,deadline.Token);
        ValidateNativeStatus(acknowledgement.RootElement);
        var adds = acknowledgement.RootElement.GetProperty("adds");
        if (adds.ValueKind != JsonValueKind.Array || adds.GetArrayLength() != 2 || adds[0].GetString() != id || adds[1].GetInt64() <= 0)
            throw new InvalidDataException("Native Solr did not acknowledge this document and its exact version.");
    }
}
