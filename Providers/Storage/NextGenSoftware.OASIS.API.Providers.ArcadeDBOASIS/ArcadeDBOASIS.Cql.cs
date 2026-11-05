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

namespace NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS;

public partial class ArcadeDBOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.ArcadeDBOASIS;
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

    public ArcadeDBOASIS(string baseUrl, string username, string password, Guid queryProbeId, HttpClient? httpClient = null)
        : this(baseUrl, username, password, httpClient)
    {
        if (queryProbeId == Guid.Empty) throw new ArgumentException("An explicit existing teaching record is required.", nameof(queryProbeId));
        _queryProbeId = queryProbeId;
    }

    public const string CqlExactRecordRoute = "/api/v1/query/" + Database;
    public const string CqlExactRecordQuery = "SELECT oasisId, doc FROM Holon WHERE oasisId = :id LIMIT 2";

    private async Task<(Holon? Row, int Bytes)> ReadQueryRecordAsync(Guid id, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + CqlExactRecordRoute) {
            Content = Json(new { language = "sql", command = CqlExactRecordQuery,
                @params = new { id = id.ToString("D") } }) };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        var raw = await ReadQueryBodyAsync(response, 1024 * 1024, token);
        using var document = JsonDocument.Parse(raw);
        static void RejectDuplicates(JsonElement element)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate native ArcadeDB property.");
        }
        RejectDuplicates(document.RootElement);
        if (document.RootElement.TryGetProperty("error", out _) || document.RootElement.TryGetProperty("exception", out _))
            throw new InvalidDataException("ArcadeDB returned an error envelope.");
        var rows = document.RootElement.GetProperty("result");
        if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 1)
            throw new InvalidDataException("ArcadeDB query did not verify native identity uniqueness.");
        if (rows.GetArrayLength() == 0) return (null, raw.Length);
        var native = rows[0];
        RejectDuplicates(native);
        if (native.GetProperty("oasisId").GetString() != id.ToString("D"))
            throw new InvalidDataException("ArcadeDB envelope identity differs from selected key.");
        var serialized = native.GetProperty("doc").GetString();
        if (serialized == null) throw new InvalidDataException("ArcadeDB native document is absent.");
        using var stored = JsonDocument.Parse(serialized);
        RejectDuplicates(stored.RootElement);
        // The existing provider writes the canonical default (Pascal-case) Holon JSON.
        if (stored.RootElement.GetProperty("Id").GetString() != id.ToString("D"))
            throw new InvalidDataException("ArcadeDB stored Holon identity differs from selected key.");
        var row = JsonSerializer.Deserialize<Holon>(serialized);
        if (row == null || row.Id != id) throw new InvalidDataException("ArcadeDB stored Holon identity is invalid.");
        row.Children = Array.Empty<IHolon>();
        return (row, raw.Length);
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            if (_queryProbeId is not Guid id) throw new InvalidOperationException("An explicit existing ArcadeDB teaching record is required.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var observed = await ReadQueryRecordAsync(id, deadline.Token);
            if (observed.Row == null || observed.Row.IsDeleted)
                throw new InvalidDataException("The existing live ArcadeDB teaching record was not verified.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return new() { IsError = true, ErrorCount = 1,
                ErrorCode = error is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "ArcadeDB query activation did not verify the existing teaching record.", Exception = error };
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
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit ArcadeDB target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "ArcadeDB historical CQL reads are not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Error("CQL_PROVIDER_INACTIVE", "Activate the existing ArcadeDB query collection first.");
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
                    throw new InvalidDataException("Native ArcadeDB holon differs from read authority.");
                if (row.IsDeleted) continue;
                rows.Add(row);
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete ArcadeDB page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "ArcadeDB query cancelled; no partial rows published.", error); }
        catch (QueryPayloadLimitException error)
        { return Error("CQL_QUERY_LIMIT", "ArcadeDB native payload exceeds its byte budget.", error); }
        catch (Exception error)
        { return Error("CQL_QUERY_FAILED", "Native ArcadeDB query failed; no partial rows published.", error); }
    }
}
