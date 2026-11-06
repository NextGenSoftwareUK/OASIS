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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.FirebaseOASIS;

public partial class FirebaseOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => ProviderType.Value;
    private static readonly CosmicQueryCapabilities UnsupportedAlias = new(CosmicQueryFeatures.None,
        Array.Empty<CosmicQueryFieldCapability>());
    public CosmicQueryCapabilities QueryCapabilities => QueryProviderType == Core.Enums.ProviderType.FirebaseOASIS
        ? BoundedIdentityCqlCompiler.Capabilities : UnsupportedAlias;

    private const int MaximumQueryBytes = 4 * 1024 * 1024;
    private sealed class QueryPayloadLimitException : IOException { }

    private async Task<(Holon? Row, int Bytes)> ReadExactQueryRecordAsync(Guid id, int maximumBytes, CancellationToken token)
    {
        var key = id.ToString("D");
        using var response = await _http.GetAsync(QueryUrl("oasis_holons", "$key", key),
            HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > maximumBytes) throw new QueryPayloadLimitException();
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        using var buffer = new MemoryStream();
        var block = new byte[16384];
        for (var read = await stream.ReadAsync(block, token); read != 0; read = await stream.ReadAsync(block, token))
        {
            if (buffer.Length + read > maximumBytes) throw new QueryPayloadLimitException();
            buffer.Write(block, 0, read);
        }
        using var document = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(buffer.ToArray()));
        var count = (int)buffer.Length;
        if (document.RootElement.ValueKind == JsonValueKind.Null) return (null, count);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Firebase key query must return an object or null.");
        var records = document.RootElement.EnumerateObject().ToArray();
        if (records.Length == 0) return (null, count);
        if (records.Length != 1 || records[0].Name != key || records[0].Value.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Firebase key equality returned a foreign or malformed row.");
        var row = ParseFirebaseHolon(id, records[0].Value.GetRawText());
        if (row == null || row.Id != id)
            throw new InvalidDataException("Firebase key/payload identity differs.");
        return (row, count);
    }

    protected async Task<OASISResult<bool>> VerifyPublicQueryProbeAsync(Guid id, CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (id == Guid.Empty || QueryProviderType != Core.Enums.ProviderType.FirebaseOASIS)
                throw new InvalidOperationException("An explicit Firebase teaching probe is required.");
            var (row, _) = await ReadExactQueryRecordAsync(id, 1024 * 1024, cancellationToken);
            if (row == null || !row.IsPublic || row.IsDeleted)
                throw new InvalidDataException("Firebase did not return one exact public teaching record.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception cause)
        {
            return new() { IsError = true, ErrorCount = 1,
                ErrorCode = cause is OperationCanceledException && cancellationToken.IsCancellationRequested
                    ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "Firebase did not verify its public teaching record.", Exception = cause };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Firebase target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = FirebaseCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Firebase provider before querying.");

            // Realtime Database has no native IN operator. Compile one bounded
            // union of exact indexed $key equality queries (at most100 requests),
            // never a whole-collection download followed by managed filtering.
            var rows = new List<IHolon>();
            int bytes = 0;
            foreach (var id in selection.Result.OrderBy(value => value.ToString("D"), StringComparer.Ordinal))
            {
                var (row, received) = await ReadExactQueryRecordAsync(id, MaximumQueryBytes - bytes, cancellationToken);
                bytes += received;
                if (row == null) continue;
                if (!plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Firebase record does not match the read authority.");
                if (row.IsDeleted) continue;
                row.Children = Array.Empty<IHolon>();
                rows.Add(row);
            }
            // All native predicates completed before paging; missing/deleted
            // records never consume page positions. Reads are not one snapshot.
            var result = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (result.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Firebase complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(result)) { IsLoaded = true, LoadedCount = result.Length };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Firebase query cancelled; no partial result published.", new OperationCanceledException(cancellationToken)); }
        catch (QueryPayloadLimitException)
        { return Error("CQL_QUERY_LIMIT", "Firebase aggregate native response exceeds4MiB; no partial result."); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Firebase key-query union failed; no partial result published.", cause); }
    }
}

public static class FirebaseCqlCompiler
{
    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority) where T : IHolon
        => BoundedIdentityCqlCompiler.Compile(query, authority, ProviderType.FirebaseOASIS);
}
