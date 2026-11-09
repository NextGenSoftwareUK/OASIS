using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS;

public partial class TypesenseOASIS
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

    private async Task EnsureCollectionAsync(string name)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_host}/collections/{name}");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // id is a built-in string primary key; it is not a searchable schema field.
            using var creation = new HttpRequestMessage(HttpMethod.Post, _host+"/collections") {
                Content = Json(new { name, fields = new[] { new { name = ".*", type = "auto" } } }) };
            using var created = await _http.SendAsync(creation, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (created.StatusCode != HttpStatusCode.Created)
                throw new InvalidDataException("Native collection creation was not acknowledged.");
            using var schema = await ReadNativeResponseAsync(created, 65536, deadline.Token);
            ValidateCollection(schema.RootElement, name);
            return;
        }
        response.EnsureSuccessStatusCode();
        using var existing = await ReadNativeResponseAsync(response, 65536, deadline.Token);
        ValidateCollection(existing.RootElement, name);
    }

    private static void ValidateCollection(JsonElement schema, string name)
    {
        if (schema.GetProperty("name").GetString() != name ||
            !schema.GetProperty("fields").EnumerateArray().Any(field =>
                field.GetProperty("name").GetString() == ".*" && field.GetProperty("type").GetString() == "auto"))
            throw new InvalidDataException("Native collection does not match the configured storage schema.");
    }

    private async Task UpsertDocAsync(string collection, object document)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var payload = JsonDocument.Parse(Ser(document));
        var id = payload.RootElement.GetProperty("id").GetString();
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_host}/collections/{collection}/documents?action=upsert&dirty_values=reject") {Content=Json(document)};
        // One native upsert only; do not replay an unknown write outcome.
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
            throw new InvalidDataException("Native upsert did not acknowledge the document.");
        using var acknowledged = await ReadNativeResponseAsync(response, 4*1024*1024, deadline.Token);
        if (acknowledged.RootElement.GetProperty("id").GetString() != id)
            throw new InvalidDataException("Native acknowledgement differs from submitted document identity.");
    }
}
