using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS;

public partial class MilvusOASIS
{
    private Task UpsertDocAsync<T>(string collection, string id, T value)
        => UpsertRowsAsync(collection, new[] { ToRow(value, id) });

    private async Task UpsertRowsAsync(string collection, IReadOnlyList<Dictionary<string, object>> rows)
    {
        var expectedIds = rows.Select(row => (string)row["id"]).ToHashSet(StringComparer.Ordinal);
        if (rows.Count == 0 || expectedIds.Count != rows.Count)
            throw new InvalidDataException("Milvus upsert requires nonempty, unique document identities.");
        var maximumAcknowledgementBytes = 65536L + 64L * rows.Count;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Post,$"{_baseUrl}/v2/vectordb/entities/upsert") {
            Content = Json(new {collectionName=collection,data=rows}) };
        request.Headers.Add("Request-Timeout","15");
        // One native submission; unknown outcomes do not authorize a retry.
        using var response = await _http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer,deadline.Token)) != 0)
        {
            if (body.Length + count > maximumAcknowledgementBytes) throw new InvalidDataException("Native Milvus acknowledgement exceeds the request-specific limit.");
            body.Write(buffer,0,count);
        }
        using var acknowledgement = JsonDocument.Parse(body.ToArray());
        var root = acknowledgement.RootElement;
        if (root.GetProperty("code").GetInt32() != 0)
            throw new InvalidDataException("Milvus rejected the native upsert.");
        var data = root.GetProperty("data");
        var ids = data.GetProperty("upsertIds");
        if (data.GetProperty("upsertCount").GetInt32() != rows.Count || ids.ValueKind != JsonValueKind.Array ||
            ids.GetArrayLength() != rows.Count || !expectedIds.SetEquals(ids.EnumerateArray().Select(id => id.GetString())))
            throw new InvalidDataException("Native Milvus acknowledgement differs from submitted documents.");
    }
}
