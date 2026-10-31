using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;

internal sealed class SupabaseMutationReconciliationException(Guid proposedId, Exception cause)
    : Exception("Supabase write outcome requires reconciliation before replay.", cause), INativeMutationReconciliationRequired
{
    public Guid ProposedId { get; } = proposedId;
    public string OriginalTransactionId => string.Empty;
}

public partial class SupabaseOASIS
{
    private static async Task<string> ReadBoundedBody(HttpResponseMessage response, CancellationToken token)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var buffer = new MemoryStream();
        var block = new byte[16384];
        for (var read = await stream.ReadAsync(block, token); read != 0; read = await stream.ReadAsync(block, token))
        {
            if (buffer.Length + read > 4 * 1024 * 1024)
                throw new InvalidDataException("Supabase native response exceeds the 4 MiB boundary.");
            buffer.Write(block, 0, read);
        }
        return new UTF8Encoding(false, true).GetString(buffer.ToArray());
    }

    private static List<HolonRow> ReadNativeRows(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Supabase requires a canonical native row array.");
        foreach (var row in document.RootElement.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object || !row.TryGetProperty("id", out var id)
                || id.ValueKind != JsonValueKind.String || !Guid.TryParseExact(id.GetString(), "D", out var guid) || guid == Guid.Empty
                || !row.TryGetProperty("data_json", out var data) || data.ValueKind != JsonValueKind.String
                || !row.TryGetProperty("is_deleted", out var deleted) || deleted.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                || !row.TryGetProperty("holon_type", out var type) || !type.TryGetInt32(out _))
                throw new InvalidDataException("Supabase canonical projection is incomplete or invalid.");
        }
        return JsonSerializer.Deserialize<List<HolonRow>>(json, _jsonOpts)!;
    }

    // One owner for point, parent, all-record and CQL completeness. PostgREST can
    // return fewer rows than requested; HTTP success alone is not a complete read.
    private async Task<List<HolonRow>> ReadCompleteHolonRows(string filter, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/oasis_holons?{filter}");
        request.Headers.Add("Prefer", "count=exact");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        var rows = ReadNativeRows(await ReadBoundedBody(response, token));
        if (!response.Content.Headers.TryGetValues("Content-Range", out var headers))
            throw new InvalidDataException("Supabase omitted exact native result range.");
        var header = headers.Single();
        var match = Regex.Match(header, @"^(?:(\d+)-(\d+)|\*)/(\d+)$", RegexOptions.CultureInvariant);
        if (!match.Success || !long.TryParse(match.Groups[3].Value, out var total) || total != rows.Count
            || rows.Count == 0 && match.Groups[1].Success
            || rows.Count != 0 && (!long.TryParse(match.Groups[1].Value, out var from) || from != 0
                || !long.TryParse(match.Groups[2].Value, out var to) || to != rows.Count - 1))
            throw new InvalidDataException("Supabase returned an incomplete or inconsistent native range.");
        var seen = new HashSet<Guid>();
        foreach (var row in rows)
        {
            var holon = ParseHolon(row);
            if (!seen.Add(holon.Id)) throw new InvalidDataException("Supabase returned duplicate canonical identities.");
        }
        return rows;
    }

    private static Holon ParseHolon(HolonRow row)
    {
        var holon = Deserialize<Holon>(row.data_json);
        if (holon == null || holon.Id == Guid.Empty || row.id != holon.Id.ToString("D") || row.is_deleted || holon.IsDeleted)
            throw new InvalidDataException("Supabase row identity/deletion disagrees with its canonical payload.");
        return holon;
    }
}
