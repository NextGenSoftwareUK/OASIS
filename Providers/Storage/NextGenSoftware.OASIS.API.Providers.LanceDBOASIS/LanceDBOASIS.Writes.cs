using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;

namespace NextGenSoftware.OASIS.API.Providers.LanceDBOASIS;

public partial class LanceDBOASIS
{
    private async Task UpsertDocAsync<T>(string table, string id, T obj)
    {
        var row = ToRow(obj, id);
        if (Encoding.UTF8.GetByteCount((string)row["doc"]) > 1024 * 1024)
            throw new InvalidDataException("LanceDB document exceeds the1MiB adapter budget.");
        var vectorType = new FixedSizeListType(FloatType.Default, 1);
        var schema = new Schema.Builder()
            .Field(new Field("id", StringType.Default, false))
            .Field(new Field("doc", StringType.Default, false))
            .Field(new Field("username", StringType.Default, false))
            .Field(new Field("email", StringType.Default, false))
            .Field(new Field("parent_holon_id", StringType.Default, false))
            .Field(new Field("vector", vectorType, false)).Build();
        StringArray Column(string name) => new StringArray.Builder().Append((string)row[name]).Build();
        using var batch = new RecordBatch(schema, new IArrowArray[]
        {
            Column("id"), Column("doc"), Column("username"), Column("email"), Column("parent_holon_id"),
            new FixedSizeListArray(vectorType, 1, new FloatArray.Builder().Append(1.0f).Build(), ArrowBuffer.Empty, 0, 0)
        }, 1);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var bytes = new MemoryStream();
        using (var writer = new ArrowStreamWriter(bytes, schema, leaveOpen: true))
        {
            await writer.WriteRecordBatchAsync(batch, deadline.Token);
            await writer.WriteEndAsync(deadline.Token);
        }
        var content = new ByteArrayContent(bytes.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.apache.arrow.stream");
        // Merge controls are query parameters; the body is exclusively Arrow IPC.
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{_baseUrl}/v1/table/{Uri.EscapeDataString(table)}/merge_insert/?on=id&when_matched_update_all=true&when_not_matched_insert_all=true&when_not_matched_by_source_delete=false")
        { Content = content };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentType?.MediaType != "application/json")
            throw new HttpRequestException("LanceDB merge was not acknowledged as native JSON.");
        using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var acknowledgement = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, deadline.Token)) != 0)
        {
            if (acknowledgement.Length + count > 65536) throw new InvalidDataException("LanceDB acknowledgement exceeds64KiB.");
            acknowledgement.Write(buffer, 0, count);
        }
        using var document = JsonDocument.Parse(acknowledgement.ToArray());
        var root = document.RootElement;
        var inserted = root.GetProperty("num_inserted_rows").GetUInt64();
        var updated = root.GetProperty("num_updated_rows").GetUInt64();
        if (root.GetProperty("version").GetUInt64() == 0 || inserted > 1 || updated > 1 || inserted + updated != 1 ||
            root.GetProperty("num_deleted_rows").GetUInt64() != 0)
            throw new InvalidDataException("LanceDB must acknowledge exactly one merged row and a positive commit version.");
        // Commit version is a native receipt, not a conditional-write/CAS guarantee.
    }
}
