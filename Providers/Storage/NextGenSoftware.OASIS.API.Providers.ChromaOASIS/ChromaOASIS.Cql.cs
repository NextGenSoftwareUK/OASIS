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

namespace NextGenSoftware.OASIS.API.Providers.ChromaOASIS;

public partial class ChromaOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.ChromaOASIS;
    public CosmicQueryCapabilities QueryCapabilities => ChromaCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Chroma target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");

            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = ChromaCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled)
                return Error("CQL_CAPABILITY_UNSUPPORTED", "Chroma history is not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Chroma collections before querying.");

            var rows = new Dictionary<Guid, IHolon>();
            if (selected.Result.Count != 0)
            {
                // Querying may never create collections or resolve a guessed storage authority.
                if (!_collectionIds.TryGetValue(CollHolons, out var collection) ||
                    !Guid.TryParseExact(collection, "D", out var collectionId) || collection != collectionId.ToString("D"))
                    return Error("CQL_PROVIDER_CONFIGURATION_REQUIRED", "Activation must resolve a canonical Chroma collection UUID.");

                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(15));
                var ids = selected.Result.Select(id => id.ToString("D")).ToArray();
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{CollectionsUrl}/{collection}/get")
                {
                    Content = Json(new { ids, include = new[] { "documents" }, limit = ids.Length + 1, offset = 0 })
                };
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                response.EnsureSuccessStatusCode();
                using var document = await ReadQueryResponseAsync(response, deadline.Token);
                var nativeIds = document.RootElement.GetProperty("ids");
                var documents = document.RootElement.GetProperty("documents");
                if (nativeIds.ValueKind != JsonValueKind.Array || documents.ValueKind != JsonValueKind.Array ||
                    nativeIds.GetArrayLength() != documents.GetArrayLength() || nativeIds.GetArrayLength() > ids.Length)
                    throw new InvalidDataException("Native IDs/documents must form a complete bounded parallel result.");

                var seen = new HashSet<Guid>();
                for (var index = 0; index < nativeIds.GetArrayLength(); index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var key = nativeIds[index].GetString();
                    if (!Guid.TryParseExact(key, "D", out var id) || key != id.ToString("D") ||
                        !selected.Result.Contains(id) || !seen.Add(id))
                        throw new InvalidDataException("Native identity differs from admitted catalogue.");
                    var json = documents[index].GetString();
                    if (json == null) throw new InvalidDataException("Native document is missing.");
                    if (Encoding.UTF8.GetByteCount(json) > 1024 * 1024) throw new NativePayloadLimitException();
                    var row = JsonSerializer.Deserialize<Holon>(json);
                    if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native holon differs from read authority.");
                    if (row.IsDeleted) continue;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(id, row);
                }
            }

            // Stable admitted-key order owns pagination; native storage ordering is unspecified.
            var page = selected.Result.Where(rows.ContainsKey).Select(id => rows[id])
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Chroma query cancelled; no partial rows published.", cause); }
        catch (NativePayloadLimitException cause)
        { return Error("CQL_QUERY_LIMIT", "Native response or stored payload exceeds the admitted byte budget.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Chroma query failed; no partial rows published.", cause); }
    }

    private sealed class NativePayloadLimitException : IOException { }

    private static async Task<JsonDocument> ReadQueryResponseAsync(HttpResponseMessage response, CancellationToken token)
    {
        using var stream = await response.Content.ReadAsStreamAsync(token);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, token)) != 0)
        {
            if (body.Length + count > 4 * 1024 * 1024) throw new NativePayloadLimitException();
            body.Write(buffer, 0, count);
        }
        return JsonDocument.Parse(body.ToArray());
    }
}
