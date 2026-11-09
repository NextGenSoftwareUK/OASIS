using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS;

public partial class MilvusOASIS
{
    private async Task UpsertDocAsync<T>(string collection,string id,T value)
    {
        // ToRow stages and checks the stored UTF8 budget before any native I/O.
        var row = ToRow(value,id);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Post,$"{_baseUrl}/v2/vectordb/entities/upsert") {
            Content = Json(new {collectionName=collection,data=new[]{row}}) };
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
            if (body.Length+count > 65536) throw new InvalidDataException("Native Milvus acknowledgement exceeds 64KiB.");
            body.Write(buffer,0,count);
        }
        using var acknowledgement = JsonDocument.Parse(body.ToArray());
        var root = acknowledgement.RootElement;
        if (root.GetProperty("code").GetInt32() != 0)
            throw new InvalidDataException("Milvus rejected the native upsert.");
        var data = root.GetProperty("data");
        var ids = data.GetProperty("upsertIds");
        if (data.GetProperty("upsertCount").GetInt32() != 1 || ids.ValueKind != JsonValueKind.Array ||
            ids.GetArrayLength() != 1 || ids[0].GetString() != id)
            throw new InvalidDataException("Native Milvus acknowledgement differs from submitted document.");
    }
}
