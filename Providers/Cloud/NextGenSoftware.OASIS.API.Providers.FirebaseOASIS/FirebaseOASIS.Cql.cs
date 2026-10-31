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
            long bytes = 0;
            foreach (var id in selection.Result.OrderBy(value => value.ToString("D"), StringComparer.Ordinal))
            {
                using var response = await _http.GetAsync(QueryUrl("oasis_holons", "$key", id.ToString("D")),
                    HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var buffer = new MemoryStream();
                var block = new byte[16384];
                for (var read = await stream.ReadAsync(block, cancellationToken); read != 0; read = await stream.ReadAsync(block, cancellationToken))
                {
                    bytes += read;
                    if (bytes > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "Firebase aggregate native response exceeds4MiB; no partial result.");
                    buffer.Write(block, 0, read);
                }
                using var document = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(buffer.ToArray()));
                if (document.RootElement.ValueKind == JsonValueKind.Null) continue;
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException("Firebase key query must return an object or null.");
                var records = document.RootElement.EnumerateObject().ToArray();
                if (records.Length > 1) throw new InvalidDataException("Firebase key equality returned multiple records.");
                if (records.Length == 0) continue;
                var record = records[0];
                if (record.Name != id.ToString("D") || record.Value.ValueKind != JsonValueKind.Object)
                    throw new InvalidDataException("Firebase key equality returned a foreign or malformed row.");
                var row = ParseFirebaseHolon(id, record.Value.GetRawText());
                if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Firebase key/payload identity or authorization mismatch.");
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
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Firebase key-query union failed; no partial result published.", cause); }
    }
}

public static class FirebaseCqlCompiler
{
    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority) where T : IHolon
        => BoundedIdentityCqlCompiler.Compile(query, authority, ProviderType.FirebaseOASIS);
}
