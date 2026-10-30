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

namespace NextGenSoftware.OASIS.API.Providers.QdrantOASIS;

public partial class QdrantOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.QdrantOASIS;
    public CosmicQueryCapabilities QueryCapabilities => QdrantCqlCompiler.Capabilities;

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
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Qdrant target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = QdrantCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Qdrant history is not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Qdrant indices before querying.");

            var rows = new Dictionary<Guid, IHolon>();
            if (selected.Result.Count != 0)
            {
                // Native retrieval over admitted keys, without vector similarity or scroll.
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(15));
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_host}/collections/oasis_holons/points?timeout=15")
                {
                    Content = new StringContent(JsonSerializer.Serialize(new {
                        ids = selected.Result.Select(id => id.ToString("D")).ToArray(), with_payload = true, with_vector = false
                    }), Encoding.UTF8, "application/json")
                };
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                response.EnsureSuccessStatusCode();
                using var document = await ReadNativeResponseAsync(response, 4 * 1024 * 1024, deadline.Token);
                if (!HasOkStatus(document.RootElement)) throw new InvalidDataException("Native Qdrant read failed.");
                var records = document.RootElement.GetProperty("result");
                if (records.ValueKind != JsonValueKind.Array || records.GetArrayLength() > selected.Result.Count)
                    throw new InvalidDataException("Native retrieval exceeds the selected catalogue.");
                var seen = new HashSet<Guid>();
                foreach (var record in records.EnumerateArray())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var key = record.GetProperty("id").GetString();
                    if (!Guid.TryParseExact(key, "D", out var id) || key != id.ToString("D") ||
                        !selected.Result.Contains(id) || !seen.Add(id))
                        throw new InvalidDataException("Native identity differs from admitted catalogue.");
                    var source = record.GetProperty("payload");
                    var row = Des<Holon>(source.GetProperty("data_json").GetString());
                    if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native holon differs from read authority.");
                    if (source.GetProperty("is_deleted").GetBoolean() || row.IsDeleted) continue;
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
        { return Error("CQL_QUERY_CANCELLED", "Qdrant query cancelled; no partial rows published.", cause); }
        catch (NativePayloadLimitException cause)
        { return Error("CQL_QUERY_LIMIT", "Complete native response exceeds 4 MiB.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Qdrant query failed; no partial rows published.", cause); }
    }
}
