using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;

namespace NextGenSoftware.OASIS.API.Providers.LanceDBOASIS;

public partial class LanceDBOASIS
{
    private sealed record NativeRow(Guid Id, string Json);
    private sealed class NativePayloadLimitException : IOException { }

    private async Task<IReadOnlyList<NativeRow>> QueryArrowRowsAsync(
        string table, string? predicate, int maximumRows, CancellationToken cancellationToken)
    {
        if (maximumRows < 1 || maximumRows > 1000) throw new NativePayloadLimitException();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var body = new Dictionary<string, object>
        {
            ["k"] = maximumRows + 1, ["offset"] = 0,
            ["columns"] = new[] { "id", "doc" }, ["prefilter"] = true
        };
        if (predicate != null) body["filter"] = predicate;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/table/{Uri.EscapeDataString(table)}/query/")
        { Content = Json(body) };
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.apache.arrow.file"));
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentType?.MediaType != "application/vnd.apache.arrow.file")
            throw new HttpRequestException("LanceDB query did not return the negotiated Arrow IPC file.");
        using var input = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var bytes = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(buffer, deadline.Token)) != 0)
        {
            if (bytes.Length + count > 4 * 1024 * 1024) throw new NativePayloadLimitException();
            bytes.Write(buffer, 0, count);
        }
        var encoded = bytes.ToArray();
        var magic = Encoding.ASCII.GetBytes("ARROW1");
        if (encoded.Length < 10 || !encoded.Take(6).SequenceEqual(magic) || !encoded.TakeLast(6).SequenceEqual(magic))
            throw new InvalidDataException("LanceDB Arrow file is incomplete or has invalid framing.");
        bytes.Position = 0;
        using var reader = new ArrowFileReader(bytes);
        var rows = new List<NativeRow>();
        var seen = new HashSet<Guid>();
        RecordBatch? batch;
        while ((batch = await reader.ReadNextRecordBatchAsync(deadline.Token)) != null)
        {
            using (batch)
            {
                if (rows.Count + (long)batch.Length > maximumRows) throw new NativePayloadLimitException();
                var fields = batch.Schema.FieldsList;
                if (fields.Count != 2 || fields[0].Name != "id" || fields[1].Name != "doc" ||
                    fields.Any(f => f.DataType is not StringType) ||
                    batch.Column(0) is not StringArray ids || batch.Column(1) is not StringArray documents ||
                    ids.NullCount != 0 || documents.NullCount != 0 || ids.Length != batch.Length || documents.Length != batch.Length)
                    throw new InvalidDataException("LanceDB query projection differs from id/doc UTF8 contract.");
                for (var index = 0; index < batch.Length; index++)
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    var key = ids.GetString(index);
                    if (!Guid.TryParseExact(key, "D", out var id) || id == Guid.Empty || key != id.ToString("D") || !seen.Add(id))
                        throw new InvalidDataException("LanceDB native identity is invalid or duplicated.");
                    var json = documents.GetString(index);
                    if (json == null) throw new InvalidDataException("LanceDB native document is missing.");
                    if (Encoding.UTF8.GetByteCount(json) > 1024 * 1024) throw new NativePayloadLimitException();
                    rows.Add(new NativeRow(id, json));
                }
            }
        }
        // An empty file must still carry the negotiated projection schema.
        if (reader.Schema.FieldsList.Count != 2 || reader.Schema.FieldsList[0].Name != "id" ||
            reader.Schema.FieldsList[1].Name != "doc" || reader.Schema.FieldsList.Any(f => f.DataType is not StringType))
            throw new InvalidDataException("LanceDB empty projection differs from id/doc UTF8 contract.");
        return rows;
    }
}
