using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

public partial class UpstashOASIS : ICosmicQueryStorageProvider
{
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
            // One bounded native MGET uses exactly the same holon:{GUID-D}
            // keys as Save/Load. No keyspace scan or managed predicate execution.
            var command = new object[] { "MGET" }.Concat(selection.Result.Select(id => (object)("holon:" + id.ToString("D")))).ToArray();
            var response = await ExecAsync(cancellationToken, command).ConfigureAwait(false);
            if (!response.TryGetProperty("result", out var values) || values.ValueKind != System.Text.Json.JsonValueKind.Array
                || values.GetArrayLength() != selection.Result.Count)
                throw new InvalidDataException("Upstash MGET must return one positional value per requested identity.");
            for (var index = 0; index < selection.Result.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var id = selection.Result[index];
                var value = values[index];
                if (value.ValueKind == System.Text.Json.JsonValueKind.Null) continue;
                if (value.ValueKind != System.Text.Json.JsonValueKind.String)
                    throw new InvalidDataException("Upstash MGET record must be a string or null.");
                var json = value.GetString();
                payloadBytes = checked(payloadBytes + Encoding.UTF8.GetByteCount(json));
                if (payloadBytes > 4 * 1024 * 1024)
                    return Error("CQL_QUERY_LIMIT", "The complete canonical response exceeds 4 MiB.");
                var row = Des<Holon>(json);
                if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Native Upstash record differs from the authorized identity.");
                if (row.IsDeleted) continue;
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
