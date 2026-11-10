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
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;

internal sealed class SupabaseMutationReconciliationException(Guid proposedId, Exception cause)
    : Exception("Supabase write outcome requires reconciliation before replay.", cause), INativeMutationReconciliationRequired
{
    public Guid ProposedId { get; } = proposedId;
    public IReadOnlyList<Guid> ProposedIds { get; init; } = new[] { proposedId };
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

    // PostgREST can
    // return fewer rows than requested; HTTP success alone is not a complete read.
    private Task<List<HolonRow>> ReadCompleteHolonRows(string filter, CancellationToken token) =>
        ReadCompleteRows($"oasis_holons?{filter}", ReadNativeRows, row => ParseHolon(row).Id, token);

    private async Task<List<TRow>> ReadCompleteRows<TRow>(
        string query, Func<string, List<TRow>> readPage, Func<TRow, Guid> getId, CancellationToken token, bool singleIdentity = false)
    {
        var rows = new List<TRow>();
        var seen = new HashSet<Guid>();
        long? expectedTotal = null;
        do
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_baseUrl}/{query}&order=id.asc&limit={(singleIdentity ? 1 : 100)}&offset={rows.Count}");
            request.Headers.Add("Prefer", "count=exact");
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var page = readPage(await ReadBoundedBody(response, token).ConfigureAwait(false));
            if (!response.Content.Headers.TryGetValues("Content-Range", out var headers))
                throw new InvalidDataException("Supabase omitted exact native result range.");

            var match = Regex.Match(headers.Single(), @"^(?:(\d+)-(\d+)|\*)/(\d+)$", RegexOptions.CultureInvariant);
            if (!match.Success || !long.TryParse(match.Groups[3].Value, out var total)
                || (expectedTotal.HasValue && total != expectedTotal.Value)
                || (page.Count == 0 && (total != 0 || match.Groups[1].Success))
                || (page.Count != 0 && (!long.TryParse(match.Groups[1].Value, out var from) || from != rows.Count
                    || !long.TryParse(match.Groups[2].Value, out var to) || to != (long)rows.Count + page.Count - 1 || to >= total)))
                throw new InvalidDataException("Supabase returned an incomplete or inconsistent native range.");

            if (singleIdentity && total > 1)
                throw new StorageIdentityAmbiguousException();

            expectedTotal = total;
            foreach (var row in page)
            {
                if (!seen.Add(getId(row)))
                    throw new InvalidDataException("Supabase returned duplicate canonical identities.");
                rows.Add(row);
            }
        } while (rows.Count < expectedTotal);
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
