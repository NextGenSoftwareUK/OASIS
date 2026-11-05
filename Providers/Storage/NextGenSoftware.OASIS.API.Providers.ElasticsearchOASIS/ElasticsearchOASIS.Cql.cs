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

namespace NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS;

public partial class ElasticsearchOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private const string QueryHolonIndex = "oasis-holons";
    public ProviderType QueryProviderType => Core.Enums.ProviderType.ElasticsearchOASIS;
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

    public ElasticsearchOASIS(string baseUrl, string? username, string? password, Guid queryProbeId, HttpClient? httpClient = null)
        : this(baseUrl, username, password, httpClient: httpClient)
    {
        if (queryProbeId == Guid.Empty) throw new ArgumentException("An explicit existing teaching record is required.", nameof(queryProbeId));
        _queryProbeId = queryProbeId;
    }

    private async Task<(Holon? Row, int Bytes)> ReadQueryRecordAsync(Guid id, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{_host}/{QueryHolonIndex}/_doc/{id:D}?realtime=true&_source=true");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode != HttpStatusCode.NotFound) response.EnsureSuccessStatusCode();
        var raw = await ReadQueryBodyAsync(response, 1024 * 1024, token);
        using var document = JsonDocument.Parse(raw);
        static void RejectDuplicates(JsonElement element)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate native Elasticsearch property.");
        }
        var native = document.RootElement;
        RejectDuplicates(native);
        if (native.TryGetProperty("error", out _)) throw new InvalidDataException("Elasticsearch returned a native source error.");
        if (native.GetProperty("_index").GetString() != QueryHolonIndex || native.GetProperty("_id").GetString() != id.ToString("D"))
            throw new InvalidDataException("Elasticsearch envelope index or identity differs from selected key.");
        var found = native.GetProperty("found").GetBoolean();
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            if (found || native.TryGetProperty("_source", out _))
                throw new InvalidDataException("Elasticsearch missing-document response is inconsistent.");
            return (null, raw.Length);
        }
        if (!found) throw new InvalidDataException("Elasticsearch returned an unsuccessful document state with HTTP success.");
        var source = native.GetProperty("_source");
        RejectDuplicates(source);
        var json = source.GetProperty("dataJson").GetString();
        if (string.IsNullOrEmpty(json)) throw new InvalidDataException("Elasticsearch canonical dataJson is absent.");
        using var canonical = JsonDocument.Parse(json);
        RejectDuplicates(canonical.RootElement);
        if (canonical.RootElement.GetProperty("id").GetString() != id.ToString("D"))
            throw new InvalidDataException("Elasticsearch stored Holon identity differs from selected key.");
        var row = JsonSerializer.Deserialize<Holon>(json, _jsonOpts);
        if (row == null || row.Id != id) throw new InvalidDataException("Elasticsearch stored Holon identity is invalid.");
        if (source.GetProperty("isDeleted").GetBoolean() != row.IsDeleted ||
            source.GetProperty("parentHolonId").GetString() != row.ParentHolonId.ToString("D") ||
            source.GetProperty("holonType").GetInt32() != (int)row.HolonType)
            throw new InvalidDataException("Elasticsearch indexed projection differs from its canonical Holon.");
        row.Children = Array.Empty<IHolon>();
        return (row, raw.Length);
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            if (_queryProbeId is not Guid id) throw new InvalidOperationException("An explicit existing Elasticsearch teaching record is required.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var observed = await ReadQueryRecordAsync(id, deadline.Token);
            if (observed.Row == null || observed.Row.IsDeleted)
                throw new InvalidDataException("The existing live Elasticsearch teaching record was not verified.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return new() { IsError = true, ErrorCount = 1,
                ErrorCode = error is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "Elasticsearch query activation did not verify the existing teaching record.", Exception = error };
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
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Elasticsearch target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Elasticsearch historical CQL reads are not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Error("CQL_PROVIDER_INACTIVE", "Activate the existing Elasticsearch query collection first.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>();
            long bytes = 0;
            foreach (var id in selected.Result.Where(id => plan.Query.ReadAccess.CanRead(id, false, Guid.Empty)))
            {
                var observed = await ReadQueryRecordAsync(id, deadline.Token);
                bytes += observed.Bytes;
                if (bytes > 4 * 1024 * 1024) throw new QueryPayloadLimitException();
                var row = observed.Row;
                if (row == null) continue;
                if (!plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Native Elasticsearch holon differs from read authority.");
                if (row.IsDeleted) continue;
                rows.Add(row);
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete Elasticsearch page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Elasticsearch query cancelled; no partial rows published.", error); }
        catch (QueryPayloadLimitException error)
        { return Error("CQL_QUERY_LIMIT", "Elasticsearch native payload exceeds its byte budget.", error); }
        catch (Exception error)
        { return Error("CQL_QUERY_FAILED", "Native Elasticsearch query failed; no partial rows published.", error); }
    }
}
