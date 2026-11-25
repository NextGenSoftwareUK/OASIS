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
    private async Task ValidateIndexAsync()
    {
        // A data-plane host is explicit; never create or silently replace an index.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_host}/describe_index_stats")
        { Content = Json(new { }) };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException("Pinecone index statistics were not acknowledged.");
        using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, deadline.Token)) != 0)
        {
            if (body.Length + count > 262144) throw new InvalidDataException("Pinecone index description exceeds256KiB.");
            body.Write(buffer, 0, count);
        }
        using var document = JsonDocument.Parse(body.ToArray());
        var index = document.RootElement;
        // Storage needs exactly one property of the index: it accepts one-dimensional dense vectors. A dimension of 1
        // proves that (sparse indexes have no dimension). vectorType and metric are not part of every API version's
        // statistics response; when reported they must agree, but storage never depends on the metric.
        if (!index.TryGetProperty("dimension", out var dimension) || dimension.GetInt32() != 1)
            throw new InvalidDataException("Pinecone requires a one-dimensional dense document-storage index.");
        if (index.TryGetProperty("vectorType", out var vectorType) && vectorType.GetString() != "dense")
            throw new InvalidDataException("Pinecone requires a one-dimensional dense document-storage index.");
        if (index.TryGetProperty("metric", out var metricValue) &&
            metricValue.GetString() is not ("cosine" or "dotproduct" or "euclidean"))
            throw new InvalidDataException("Unsupported Pinecone index metric.");
        if (index.GetProperty("totalVectorCount").GetInt64() < 0 ||
            !index.GetProperty("indexFullness").TryGetDouble(out var fullness) || !double.IsFinite(fullness) || fullness < 0 || fullness > 1)
            throw new InvalidDataException("Invalid Pinecone native index statistics.");
        var namespaces = index.GetProperty("namespaces");
        if (namespaces.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Pinecone namespace map is missing.");
        foreach (var ns in namespaces.EnumerateObject())
            if (ns.Value.GetProperty("vectorCount").GetInt64() < 0)
                throw new InvalidDataException("Pinecone namespace count is invalid.");
    }
}
