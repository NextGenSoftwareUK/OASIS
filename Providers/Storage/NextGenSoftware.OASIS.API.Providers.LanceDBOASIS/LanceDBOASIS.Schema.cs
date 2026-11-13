using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.LanceDBOASIS;

public partial class LanceDBOASIS
{
    private async Task EnsureTableAsync(string table)
    {
        // Read-only admission: an unavailable table is never a request to create or replace it.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/table/{Uri.EscapeDataString(table)}/describe/")
        { Content = Json(new { version = (ulong?)null }) };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        if (response.StatusCode != HttpStatusCode.OK || response.Content.Headers.ContentType?.MediaType != "application/json")
            throw new HttpRequestException("LanceDB table description was not acknowledged as native JSON.");
        using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, deadline.Token)) != 0)
        {
            if (body.Length + count > 262144) throw new InvalidDataException("LanceDB table schema exceeds256KiB.");
            body.Write(buffer, 0, count);
        }
        using var document = JsonDocument.Parse(body.ToArray());
        var root = document.RootElement;
        if (root.GetProperty("version").GetUInt64() == 0)
            throw new InvalidDataException("LanceDB requires a positive native table version.");
        var fields = root.GetProperty("schema").GetProperty("fields");
        if (fields.ValueKind != JsonValueKind.Array || fields.GetArrayLength() != 6)
            throw new InvalidDataException("LanceDB document schema requires exactly six fields.");
        var names = new[] { "id", "doc", "username", "email", "parent_holon_id", "vector" };
        for (var index = 0; index < 6; index++)
        {
            var field = fields[index];
            if (field.GetProperty("name").GetString() != names[index] || field.GetProperty("nullable").GetBoolean())
                throw new InvalidDataException("LanceDB field order/nullability differs from the storage contract.");
            var type = field.GetProperty("type");
            if (index < 5)
                Primitive(type, "string");
            else
            {
                if (type.GetProperty("type").GetString() != "fixed_size_list" || type.GetProperty("length").GetInt32() != 1)
                    throw new InvalidDataException("LanceDB vector must be a fixed-size list of one Float32 coordinate.");
                var children = type.GetProperty("fields");
                if (children.ValueKind != JsonValueKind.Array || children.GetArrayLength() != 1 ||
                    children[0].GetProperty("name").GetString() != "item" || !children[0].GetProperty("nullable").GetBoolean())
                    throw new InvalidDataException("LanceDB vector child field differs from the Arrow storage schema.");
                Primitive(children[0].GetProperty("type"), "float");
            }
        }
    }

    private static void Primitive(JsonElement type, string expected)
    {
        if (type.GetProperty("type").GetString() != expected ||
            type.TryGetProperty("fields", out var children) && children.ValueKind != JsonValueKind.Null ||
            type.TryGetProperty("length", out var length) && length.ValueKind != JsonValueKind.Null)
            throw new InvalidDataException("LanceDB primitive field differs from the storage contract.");
    }
}
