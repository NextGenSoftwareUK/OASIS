using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS;

/// <summary>Reads the bounded symbol projection from a configured QuestDB fixture table.</summary>
internal sealed class QuestDbSymbolRestClient(HttpClient http, Uri executeUri)
{
    private const int MaximumBodyBytes = 16 * 1024;
    private static readonly Regex Symbol = new(@"\A[A-Z0-9]{1,16}-[A-Z0-9]{1,16}\z", RegexOptions.CultureInvariant);

    internal static void ValidateKeys(IReadOnlyList<string> keys)
    {
        if (keys.Count is < 1 or > 8 || keys.Any(key => key == null || !Symbol.IsMatch(key)) ||
            keys.Distinct(StringComparer.Ordinal).Count() != keys.Count)
            throw new ArgumentException("One to eight distinct canonical QuestDB symbols are required.", nameof(keys));

    }

    public async Task<IReadOnlyList<string>> ReadSymbolsAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        ValidateKeys(keys);
        var literals = string.Join(", ", keys.Select(key => $"'{key}'"));
        var sql = $"select symbol from cql_demo_symbols where symbol in ({literals}) order by symbol";
        var uri = new UriBuilder(executeUri) { Query = "query=" + Uri.EscapeDataString(sql) }.Uri;
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[MaximumBodyBytes + 1];
        var size = await stream.ReadAtLeastAsync(buffer, buffer.Length, false, cancellationToken);
        if (size > MaximumBodyBytes)
            throw new InvalidDataException("QuestDB response exceeded the symbol query budget.");

        using var json = JsonDocument.Parse(buffer.AsMemory(0, size), new JsonDocumentOptions { MaxDepth = 6 });
        var result = json.RootElement;
        var columns = result.GetProperty("columns");
        var rows = result.GetProperty("dataset");
        if (result.TryGetProperty("error", out _) || columns.GetArrayLength() != 1 ||
            columns[0].GetProperty("name").GetString() != "symbol" ||
            columns[0].GetProperty("type").GetString() != "SYMBOL" ||
            rows.GetArrayLength() != keys.Count || result.GetProperty("count").GetInt32() != keys.Count)
            throw new InvalidDataException("QuestDB returned an unexpected symbol result schema or count.");

        var symbols = rows.EnumerateArray().Select(row => row.GetArrayLength() == 1
            ? row[0].GetString() : null).ToArray();
        if (!symbols.SequenceEqual(keys.OrderBy(key => key, StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidDataException("QuestDB returned unexpected, duplicate or unordered symbols.");
        return keys.ToArray();
    }
}
