using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS;

public partial class OpenSearchOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.OpenSearchOASIS;
    public CosmicQueryCapabilities QueryCapabilities => OpenSearchCqlCompiler.Capabilities;

    private static async Task<JsonDocument> ReadNativeResponseAsync(HttpResponseMessage response,
        int maximumBytes, CancellationToken cancellation)
    {
        using var stream = await response.Content.ReadAsStreamAsync(cancellation);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, cancellation)) != 0)
        {
            if (body.Length + count > maximumBytes)
                throw new NativePayloadLimitException();
            body.Write(buffer, 0, count);
        }
        return JsonDocument.Parse(body.ToArray());
    }

    private sealed class NativePayloadLimitException : IOException { }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit OpenSearch target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = OpenSearchCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Version-control mode needs a verified revision query policy.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured OpenSearch indices before querying.");

            var rows = new Dictionary<Guid, IHolon>();
            if (selected.Result.Count != 0)
            {
                // One real-time native multi-get over admitted IDs; never a search/index scan.
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/oasis_holons/_mget")
                {
                    Content = new StringContent(JsonSerializer.Serialize(new {
                        ids = selected.Result.Select(id => id.ToString("D")).ToArray()
                    }), Encoding.UTF8, "application/json")
                };
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                using var document = await ReadNativeResponseAsync(response, 4 * 1024 * 1024, cancellationToken);
                var records = document.RootElement.GetProperty("docs");
                if (records.ValueKind != JsonValueKind.Array || records.GetArrayLength() != selected.Result.Count)
                    throw new InvalidDataException("Native multi-get must account for every selected identity.");
                var seen = new HashSet<Guid>();
                foreach (var record in records.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var key = record.GetProperty("_id").GetString();
                    if (!Guid.TryParseExact(key, "D", out var id) || key != id.ToString("D") ||
                        !selected.Result.Contains(id) || !seen.Add(id) ||
                        record.GetProperty("_index").GetString() != "oasis_holons" || record.TryGetProperty("error", out _))
                        throw new InvalidDataException("Native identity/index differs from admitted catalogue or has an item error.");
                    if (!record.GetProperty("found").GetBoolean()) continue;
                    if (record.GetProperty("_version").GetInt64() < 1)
                        throw new InvalidDataException("Native document revision missing.");
                    var source = record.GetProperty("_source");
                    var row = JsonSerializer.Deserialize<Holon>(source.GetRawText(), _jsonOpts);
                    if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native holon differs from read authority.");
                    if (source.GetProperty("isDeleted").GetBoolean() || row.IsDeleted) continue;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(id, row);
                }
            }
            var page = selected.Result.Where(rows.ContainsKey).Select(id => rows[id])
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "OpenSearch query cancelled; no partial rows published.", cause); }
        catch (NativePayloadLimitException cause)
        { return Error("CQL_QUERY_LIMIT", "Complete native response exceeds 4 MiB.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native OpenSearch query failed; no partial rows published.", cause); }
    }
}
