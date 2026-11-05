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

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS;

public partial class TypesenseOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.TypesenseOASIS;
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

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_host}/collections/{ColHolons}");
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await ReadQueryBodyAsync(response, 64 * 1024, deadline.Token));
            if (document.RootElement.GetProperty("name").GetString() != ColHolons)
                throw new InvalidDataException("Existing Typesense collection identity differs.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return new() { IsError = true, ErrorCount = 1,
                ErrorCode = error is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "Typesense query activation did not verify the existing collection.", Exception = error };
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
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Typesense target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Typesense historical CQL reads are not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Error("CQL_PROVIDER_INACTIVE", "Activate the existing Typesense query collection first.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>();
            long bytes = 0;
            foreach (var id in selected.Result.Where(id => plan.Query.ReadAccess.CanRead(id, false, Guid.Empty)))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{_host}/collections/{ColHolons}/documents/{id:D}");
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                if (response.StatusCode == HttpStatusCode.NotFound) continue;
                response.EnsureSuccessStatusCode();
                var raw = await ReadQueryBodyAsync(response, 1024 * 1024, deadline.Token);
                bytes += raw.Length;
                if (bytes > 4 * 1024 * 1024) throw new QueryPayloadLimitException();
                using var document = JsonDocument.Parse(raw);
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in document.RootElement.EnumerateObject())
                    if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate native document property.");
                if (document.RootElement.GetProperty("id").GetString() != id.ToString("D"))
                    throw new InvalidDataException("Native Typesense identity differs from selected key.");
                var row = JsonSerializer.Deserialize<Holon>(raw, _jsonOpts);
                if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Native Typesense holon differs from read authority.");
                if (row.IsDeleted) continue;
                row.Children = Array.Empty<IHolon>();
                rows.Add(row);
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete Typesense page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Typesense query cancelled; no partial rows published.", error); }
        catch (QueryPayloadLimitException error)
        { return Error("CQL_QUERY_LIMIT", "Typesense native payload exceeds its byte budget.", error); }
        catch (Exception error)
        { return Error("CQL_QUERY_FAILED", "Native Typesense query failed; no partial rows published.", error); }
    }
}
