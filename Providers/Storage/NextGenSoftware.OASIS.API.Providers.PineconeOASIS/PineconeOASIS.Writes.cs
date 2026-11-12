using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.PineconeOASIS;

public partial class PineconeOASIS
{
    private async Task UpsertDocAsync<T>(string ns, string id, T obj)
    {
        var metadata = ToMetadata(obj, id);
        if (JsonSerializer.SerializeToUtf8Bytes(metadata).Length > 40960)
            throw new InvalidDataException("Pinecone metadata exceeds the40KiB record budget.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_host}/vectors/upsert")
        {
            Content = Json(new { vectors = new[] { new { id, values = new[] { 1.0f }, metadata } }, @namespace = ns })
        };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException("Pinecone did not acknowledge the vector upsert.");
        using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, deadline.Token)) != 0)
        {
            if (body.Length + count > 65536) throw new InvalidDataException("Pinecone acknowledgement exceeds64KiB.");
            body.Write(buffer, 0, count);
        }
        using var document = JsonDocument.Parse(body.ToArray());
        if (document.RootElement.GetProperty("upsertedCount").GetInt32() != 1)
            throw new InvalidDataException("Pinecone must acknowledge exactly one vector.");
        // This native acknowledgement accepts the write; eventual read visibility is separate.
    }
}
