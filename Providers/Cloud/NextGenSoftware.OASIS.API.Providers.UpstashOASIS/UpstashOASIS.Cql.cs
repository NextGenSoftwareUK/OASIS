using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.UpstashOASIS;

public partial class UpstashOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly Guid? _queryProbe;

    /// <summary>Explicit public query source with a caller-owned, bounded HTTP client.</summary>
    public UpstashOASIS(string restUrl, string token, HttpClient client, Guid queryProbe)
        : this(restUrl, token, client)
    {
        if (queryProbe == Guid.Empty) throw new ArgumentException("An existing public teaching probe is required.", nameof(queryProbe));
        _queryProbe = queryProbe;
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_queryProbe is not Guid id || QueryProviderType != Core.Enums.ProviderType.UpstashOASIS)
                throw new InvalidOperationException("An explicit source authority and teaching probe are required.");
            var values = await ReadQueryValuesAsync(new[] { id }, cancellationToken).ConfigureAwait(false);
            var payload = ReadQueryPayload(values[0]);
            var row = payload == null ? null : ReadQueryRecord(payload, id);
            if (row == null || row.IsDeleted || !row.IsPublic)
                throw new InvalidDataException("The teaching probe must be an existing public record.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception cause)
        {
            return new()
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = cause is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "The native Upstash query source did not verify its teaching probe.",
                Exception = cause
            };
        }
    }

    private async Task<JsonElement> ReadQueryValuesAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        // Exact canonical Holon keys. The native result is positional.
        var command = new object[] { "MGET" }.Concat(ids.Select(id => (object)("holon:" + id.ToString("D")))).ToArray();
        var response = await ExecAsync(cancellationToken, command).ConfigureAwait(false);
        if (!response.TryGetProperty("result", out var values) || values.ValueKind != JsonValueKind.Array ||
            values.GetArrayLength() != ids.Count)
            throw new InvalidDataException("Upstash MGET must return one positional value per requested identity.");
        return values;
    }

    private static string? ReadQueryPayload(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString(),
        _ => throw new InvalidDataException("Upstash MGET record must be a string or null.")
    };

    private static Holon ReadQueryRecord(string payload, Guid id)
    {
        var row = Des<Holon>(payload);
        if (row == null || row.Id != id)
            throw new InvalidDataException("Native Upstash record differs from the requested identity.");
        return row;
    }

    public ProviderType QueryProviderType => ProviderType.Value;
    private static readonly CosmicQueryCapabilities UnsupportedAliasCapabilities = new(CosmicQueryFeatures.None,
        Array.Empty<CosmicQueryFieldCapability>());
    public CosmicQueryCapabilities QueryCapabilities => QueryProviderType == Core.Enums.ProviderType.UpstashOASIS
        ? UpstashCqlCompiler.Capabilities : UnsupportedAliasCapabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Upstash target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = UpstashCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Upstash provider before querying.");

            var rows = new List<IHolon>();
            var payloadBytes = 0;
            if (selection.Result.Count == 0 || plan.Query.Limit == 0)
                return new(Array.AsReadOnly(Array.Empty<IHolon>())) { IsLoaded = true, LoadedCount = 0 };
            var values = await ReadQueryValuesAsync(selection.Result, cancellationToken).ConfigureAwait(false);
            for (var index = 0; index < selection.Result.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var id = selection.Result[index];
                var json = ReadQueryPayload(values[index]);
                if (json == null) continue;
                payloadBytes = checked(payloadBytes + Encoding.UTF8.GetByteCount(json));
                if (payloadBytes > 4 * 1024 * 1024)
                    return Error("CQL_QUERY_LIMIT", "The complete canonical response exceeds 4 MiB.");
                var row = ReadQueryRecord(json, id);
                if (!plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Native Upstash record differs from the authorized identity.");
                if (row.IsDeleted) continue;
                if (_queryProbe.HasValue && !row.IsPublic)
                    throw new InvalidDataException("Native teaching record is no longer public.");
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[QueryProviderType] = id.ToString("D");
                rows.Add(row);
            }

            // Paging follows the bounded live result, after missing/deleted records
            // are removed. Federation still observes separate provider moments.
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Upstash query cancelled; no partial result published.", new OperationCanceledException(cancellationToken)); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Upstash native-key query failed; no partial result published.", cause); }
    }
}
