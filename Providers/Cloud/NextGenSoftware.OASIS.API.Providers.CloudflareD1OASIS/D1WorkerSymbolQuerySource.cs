using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CloudflareD1OASIS;

/// <summary>Read-only CQL projection from an explicitly configured D1 Worker.</summary>
public sealed class D1WorkerSymbolQuerySource : ICosmicQueryStorageProvider
{
    public const string SymbolMetadata = "D1Symbol";
    private readonly HttpClient http;
    private readonly Uri endpoint;
    private readonly Guid owner;
    private readonly string sourceName;
    private readonly HashSet<string> catalogue;

    public D1WorkerSymbolQuerySource(HttpClient http, Uri endpoint, Guid owner, IEnumerable<string> symbols)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(symbols);
        if (!endpoint.IsAbsoluteUri || endpoint.AbsolutePath != "/symbols" ||
            (endpoint.Scheme != Uri.UriSchemeHttps && !(endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback)) ||
            endpoint.UserInfo.Length != 0 || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 || owner == Guid.Empty)
            throw new ArgumentException("An explicit D1 Worker endpoint and owner are required.");
        var enrolled = symbols.Take(9).ToArray();
        catalogue = new(enrolled, StringComparer.Ordinal);
        if (enrolled.Length is < 1 or > 8 || catalogue.Count != enrolled.Length ||
            enrolled.Any(key => string.IsNullOrWhiteSpace(key) || key.Length > 33))
            throw new ArgumentException("One to eight distinct bounded symbols are required.", nameof(symbols));
        this.http = http;
        this.endpoint = endpoint;
        this.owner = owner;
        sourceName = $"d1-worker-symbols:{endpoint.AbsoluteUri}";
    }

    public ProviderType QueryProviderType => ProviderType.CloudflareD1OASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; } =
        CosmicQueryCapabilities.ForStringMetadataKeyLookup(SymbolMetadata, 33, 8);

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
            return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit D1 symbol source is required.");
        if (maximumResults is < 1 or > 10000)
            return Failure("CQL_QUERY_LIMIT", "A bounded result budget is required.");
        var admitted = QueryCapabilities.AdmitPlan(plan);
        if (admitted.IsError)
            return Failure(admitted.ErrorCode, admitted.Message);

        IReadOnlyList<string> keys;
        if (QueryCapabilities.StringKeySetLookup.IsKeySetQuery(plan.Query))
        {
            var selected = QueryCapabilities.StringKeySetLookup.SelectKeys(plan.Query);
            if (selected.IsError) return Failure(selected.ErrorCode, selected.Message);
            keys = selected.Result;
        }
        else
        {
            var selected = QueryCapabilities.StringPointLookup.SelectKey(plan.Query);
            if (selected.IsError) return Failure(selected.ErrorCode, selected.Message);
            keys = new[] { selected.Result };
        }
        if (keys.Any(key => !catalogue.Contains(key)))
            return Failure("CQL_INVALID_SOURCE_KEY", "Select symbols from the enrolled D1 catalogue.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var symbols = await ReadSymbolsAsync(keys, cancellationToken);
            var rows = symbols.Select(MapSymbol)
                .Where(row => plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? symbols.Count).ToArray();
            cancellationToken.ThrowIfCancellationRequested();
            return rows.Length > maximumResults
                ? Failure("CQL_QUERY_LIMIT", "The complete source page exceeds the result budget.")
                : new(Array.AsReadOnly(rows)) { IsLoaded = true, LoadedCount = rows.Length };
        }
        catch (OperationCanceledException error)
        {
            return Failure("CQL_QUERY_CANCELLED", "D1 symbol query cancelled.", error);
        }
        catch (Exception error)
        {
            return Failure("CQL_QUERY_FAILED", "D1 symbol query failed.", error);
        }
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string? code, string message, Exception? error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
    private async Task<IReadOnlyList<string>> ReadSymbolsAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var token = deadline.Token;
        var uri = new UriBuilder(endpoint) { Query = string.Join("&", keys.Select(key => "symbol=" + Uri.EscapeDataString(key))) }.Uri;
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        var buffer = new byte[4097];
        var size = await stream.ReadAtLeastAsync(buffer, buffer.Length, false, token);
        if (size == buffer.Length) throw new InvalidDataException("D1 response exceeded the symbol query budget.");
        using var json = JsonDocument.Parse(buffer.AsMemory(0, size));
        var properties = json.RootElement.EnumerateObject().ToArray();
        if (properties.Length != 1 || properties[0].Name != "symbols")
            throw new InvalidDataException("D1 returned an unexpected symbol envelope.");
        var symbols = properties[0].Value.EnumerateArray().Select(row => row.GetString()).ToArray();
        if (!symbols.SequenceEqual(keys.OrderBy(key => key, StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidDataException("D1 returned unexpected or duplicate symbols.");
        return keys.ToArray();
    }

    private IHolon MapSymbol(string symbol)
    {
        var fields = new Dictionary<string, object> { ["name"] = symbol, [SymbolMetadata] = symbol };
        var mapped = NativeRecordHolonMapper.CreateHolon(owner, QueryProviderType,
            new NativeRecord(sourceName, symbol, fields), NativeDataMapping.ForSource(sourceName));
        if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
        var row = mapped.Result;
        row.CreatedByAvatarId = owner;
        row.IsPublic = true;
        row.IsActive = true;
        row.Children = Array.Empty<IHolon>();
        row.ProviderUniqueStorageKey[QueryProviderType] = symbol;
        return row;
    }
}
