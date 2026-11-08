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

namespace NextGenSoftware.OASIS.API.Providers.MarqoOASIS;

public partial class MarqoOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.MarqoOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    private sealed class QueryPayloadLimitException : IOException { }

    private static void RequireUniqueObject(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Marqo query object required.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in value.EnumerateObject())
            if (!names.Add(field.Name)) throw new InvalidDataException("Duplicate Marqo query property.");
    }

    private static async Task<byte[]> ReadQueryBodyAsync(HttpResponseMessage response, int maximum, CancellationToken token)
    {
        if (maximum <= 0 || response.Content.Headers.ContentLength is long size && size > maximum)
            throw new QueryPayloadLimitException();
        using var stream = await response.Content.ReadAsStreamAsync(token);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await stream.ReadAsync(buffer, token)) > 0)
        {
            if (body.Length + read > maximum) throw new QueryPayloadLimitException();
            body.Write(buffer, 0, read);
        }
        return body.ToArray();
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/indexes/{CollHolons}/settings");
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            using var settings = JsonDocument.Parse(await ReadQueryBodyAsync(response, 64 * 1024, deadline.Token));
            RequireUniqueObject(settings.RootElement);
            if (settings.RootElement.GetProperty("type").GetString() != "unstructured"
                || string.IsNullOrWhiteSpace(settings.RootElement.GetProperty("model").GetString()))
                throw new InvalidDataException("Existing Marqo Holon index settings differ from the admitted layout.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return new() { IsError = true, ErrorCount = 1, Exception = error,
                ErrorCode = error is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "Marqo query activation did not verify the existing index." };
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
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Marqo target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Marqo historical CQL reads are not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Error("CQL_PROVIDER_INACTIVE", "Activate the existing Marqo query index first.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>();
            int wireBytes = 0;
            foreach (var id in selected.Result.Where(id => plan.Query.ReadAccess.CanRead(id, false, Guid.Empty)))
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/indexes/{CollHolons}/documents/{id:D}");
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                if (response.StatusCode != HttpStatusCode.NotFound) response.EnsureSuccessStatusCode();
                var raw = await ReadQueryBodyAsync(response, Math.Min(1024 * 1024, 4 * 1024 * 1024 - wireBytes), deadline.Token);
                wireBytes += raw.Length;
                using var native = JsonDocument.Parse(raw);
                RequireUniqueObject(native.RootElement);
                if (response.StatusCode == HttpStatusCode.NotFound
                    && native.RootElement.GetProperty("code").GetString() == "document_not_found") continue;
                response.EnsureSuccessStatusCode();
                if (native.RootElement.GetProperty("_id").GetString() != id.ToString("D"))
                    throw new InvalidDataException("Marqo native document identity differs from selected key.");
                var payload = native.RootElement.GetProperty("doc").GetString();
                if (string.IsNullOrEmpty(payload)) throw new InvalidDataException("Marqo canonical doc is absent.");
                using var canonical = JsonDocument.Parse(payload);
                RequireUniqueObject(canonical.RootElement);
                if (canonical.RootElement.GetProperty("Id").GetString() != id.ToString("D"))
                    throw new InvalidDataException("Marqo canonical Holon identity differs from selected key.");
                using var projected = new MemoryStream();
                using (var writer = new Utf8JsonWriter(projected))
                {
                    writer.WriteStartObject();
                    foreach (var property in canonical.RootElement.EnumerateObject())
                        if (!property.NameEquals("Children")) property.WriteTo(writer);
                    writer.WriteEndObject();
                }
                // The existing provider's ToDoc stores default Pascal-case Holon JSON.
                var row = JsonSerializer.Deserialize<Holon>(projected.ToArray());
                if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Marqo canonical Holon differs from read authority.");
                if (row.IsDeleted) continue;
                row.Children = Array.Empty<IHolon>();
                rows.Add(row);
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete Marqo page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Marqo query cancelled; no partial rows published.", error); }
        catch (QueryPayloadLimitException error)
        { return Error("CQL_QUERY_LIMIT", "Marqo native payload exceeds its byte budget.", error); }
        catch (Exception error)
        { return Error("CQL_QUERY_FAILED", "Native Marqo query failed; no partial rows published.", error); }
    }
}
