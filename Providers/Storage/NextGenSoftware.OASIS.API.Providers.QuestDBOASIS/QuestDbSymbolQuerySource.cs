using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS;

/// <summary>Read-only CQL projection of an explicitly enrolled QuestDB symbol table.</summary>
public sealed class QuestDbSymbolQuerySource : ICosmicQueryStorageProvider
{
    public const string SymbolMetadata = "QuestDbSymbol";
    private readonly QuestDbSymbolRestClient client;
    private readonly HashSet<string> catalogue;
    private readonly Guid owner;
    private readonly string sourceName;

    public QuestDbSymbolQuerySource(HttpClient http, Uri endpoint, Guid owner, IEnumerable<string> symbols)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(symbols);
        if (!endpoint.IsAbsoluteUri || endpoint.AbsolutePath != "/api/v1/sql/execute" ||
            (endpoint.Scheme != Uri.UriSchemeHttps && !(endpoint.Scheme == Uri.UriSchemeHttp && endpoint.IsLoopback)) ||
            endpoint.UserInfo.Length != 0 || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 || owner == Guid.Empty)
            throw new ArgumentException("An explicit QuestDB SQL endpoint and owner are required.");
        var enrolled = symbols.Take(9).ToArray();
        QuestDbSymbolRestClient.ValidateKeys(enrolled);
        catalogue = new(enrolled, StringComparer.Ordinal);
        this.owner = owner;
        sourceName = $"questdb-symbols:{endpoint.AbsoluteUri}:cql_demo_symbols";
        client = new(http, endpoint);
    }

    public ProviderType QueryProviderType => ProviderType.QuestDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; } =
        CosmicQueryCapabilities.ForStringMetadataKeyLookup(SymbolMetadata, 33, 8);

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
            return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit QuestDB symbol source is required.");
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
            return Failure("CQL_INVALID_SOURCE_KEY", "Select symbols from the enrolled QuestDB catalogue.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var symbols = await client.ReadSymbolsAsync(keys, cancellationToken);
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
            return Failure("CQL_QUERY_CANCELLED", "QuestDB symbol query cancelled.", error);
        }
        catch (Exception error)
        {
            return Failure("CQL_QUERY_FAILED", "QuestDB symbol query failed.", error);
        }
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

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception? error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
}
