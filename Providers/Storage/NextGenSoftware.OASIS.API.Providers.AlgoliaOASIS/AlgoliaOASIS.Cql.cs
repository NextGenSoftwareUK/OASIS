using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AlgoliaOASIS;

public partial class AlgoliaOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.AlgoliaOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    private sealed class QueryPayloadLimitException : IOException { }

    private static async Task<byte[]> ReadQueryBodyAsync(HttpResponseMessage response, int maximumBytes, CancellationToken token)
    {
        if (response.Content.Headers.ContentLength is long size && size > maximumBytes)
            throw new QueryPayloadLimitException();
        using var input = await response.Content.ReadAsStreamAsync(token);
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await input.ReadAsync(buffer, token)) != 0)
        {
            if (output.Length + read > maximumBytes) throw new QueryPayloadLimitException();
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    private readonly Guid? _queryProbeId;

    public AlgoliaOASIS(string appId, string apiKey, Guid queryProbeId, HttpClient? httpClient = null)
        : this(appId, apiKey, httpClient)
    {
        if (queryProbeId == Guid.Empty) throw new ArgumentException("An explicit existing teaching record is required.", nameof(queryProbeId));
        _queryProbeId = queryProbeId;
    }

    private static void RejectDuplicateQueryProperties(JsonElement element)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
            if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate native Algolia property.");
    }

    private async Task<IReadOnlyList<Holon>> ReadQueryRecordsAsync(IReadOnlyList<Guid> ids, CancellationToken token)
    {
        if (ids.Count == 0) return Array.Empty<Holon>();
        // Native read-only getObjects: requests explicitly select this index and
        // object ID. No search, browse, index creation or settings mutation.
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/1/indexes/*/objects");
        request.Content = new StringContent(JsonSerializer.Serialize(new {
            requests = ids.Select(id => new { indexName = IdxHolons, objectID = id.ToString("D") })
        }), System.Text.Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        var raw = await ReadQueryBodyAsync(response, 4 * 1024 * 1024, token);
        using var document = JsonDocument.Parse(raw);
        var envelope = document.RootElement;
        RejectDuplicateQueryProperties(envelope);
        // Algolia documents an optional native status message, including a
        // missing index. Never reinterpret it as an empty successful query.
        if (envelope.TryGetProperty("message", out _) || envelope.TryGetProperty("error", out _) || envelope.TryGetProperty("status", out _))
            throw new InvalidDataException("Algolia returned an unresolved native source status.");
        var results = envelope.GetProperty("results");
        if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() != ids.Count)
            throw new InvalidDataException("Algolia result count differs from requested identities.");
        var rows = new List<Holon>();
        for (var index = 0; index < ids.Count; index++)
        {
            var record = results[index];
            if (record.ValueKind == JsonValueKind.Null) continue;
            if (record.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Algolia returned an invalid stored record.");
            if (System.Text.Encoding.UTF8.GetByteCount(record.GetRawText()) > 1024 * 1024)
                throw new QueryPayloadLimitException();
            RejectDuplicateQueryProperties(record);
            var id = ids[index].ToString("D");
            if (record.GetProperty("objectID").GetString() != id || record.GetProperty("id").GetString() != id)
                throw new InvalidDataException("Algolia positional object ID or canonical Holon identity differs.");
            // This query contract excludes child graphs. Apply that projection
            // before materialization encounters the IHolon child interface;
            // the byte budget above still covers the complete native record.
            using var projected = new MemoryStream();
            using (var writer = new Utf8JsonWriter(projected))
            {
                writer.WriteStartObject();
                foreach (var property in record.EnumerateObject())
                    if (!property.NameEquals("children")) property.WriteTo(writer);
                writer.WriteEndObject();
            }
            var row = JsonSerializer.Deserialize<Holon>(projected.ToArray(), _jsonOpts);
            if (row == null || row.Id != ids[index]) throw new InvalidDataException("Invalid canonical Algolia Holon.");
            row.Children = Array.Empty<IHolon>();
            rows.Add(row);
        }
        return rows.AsReadOnly();
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            if (_queryProbeId is not Guid id) throw new InvalidOperationException("An explicit existing Algolia teaching record is required.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var observed = await ReadQueryRecordsAsync(new[] { id }, deadline.Token);
            if (observed.Count != 1 || observed[0].IsDeleted)
                throw new InvalidDataException("The existing live Algolia teaching record was not verified.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return new() { IsError = true, ErrorCount = 1,
                ErrorCode = error is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "Algolia query activation did not verify the existing teaching record.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? error = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Algolia target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Algolia historical CQL reads are not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Error("CQL_PROVIDER_INACTIVE", "Activate the existing Algolia query collection first.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>();
            var ids = selected.Result.Where(id => plan.Query.ReadAccess.CanRead(id, false, Guid.Empty)).ToArray();
            var observed = await ReadQueryRecordsAsync(ids, deadline.Token);
            foreach (var row in observed)
            {
                if (!plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Native Algolia holon differs from read authority.");
                if (row.IsDeleted) continue;
                rows.Add(row);
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete Algolia page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Algolia query cancelled; no partial rows published.", error); }
        catch (QueryPayloadLimitException error)
        { return Error("CQL_QUERY_LIMIT", "Algolia native payload exceeds its byte budget.", error); }
        catch (Exception error)
        { return Error("CQL_QUERY_FAILED", "Native Algolia query failed; no partial rows published.", error); }
    }
}
