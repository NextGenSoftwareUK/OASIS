using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS;

public partial class PocketBaseOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => ProviderType.Value;
    private static readonly CosmicQueryCapabilities UnsupportedAlias = new(CosmicQueryFeatures.None,
        Array.Empty<CosmicQueryFieldCapability>());
    public CosmicQueryCapabilities QueryCapabilities => QueryProviderType == Core.Enums.ProviderType.PocketBaseOASIS
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
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit PocketBase target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = PocketBaseCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured PocketBase provider before querying.");
            var rows = new List<IHolon>();
            var ids = selection.Result.ToHashSet();
            if (ids.Count > 0)
            {
                // The server evaluates only the frozen authorized GUID selection
                // over the same oasis_id/data_json records written by SaveHolon.
                var filter = "(" + string.Join("||", selection.Result.Select(id => "oasis_id='" + id.ToString("D") + "'")) + ")&&is_deleted=false";
                var url = _baseUrl + "/api/collections/oasis_holons/records?perPage=100&page=1&sort=oasis_id&skipTotal=false&filter=" + Uri.EscapeDataString(filter);
                await EnsureAuthAsync();
                using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var buffer = new MemoryStream();
                var block = new byte[16384];
                for (var read = await source.ReadAsync(block, cancellationToken); read != 0; read = await source.ReadAsync(block, cancellationToken))
                {
                    if (buffer.Length + read > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "The complete native PocketBase response exceeds 4 MiB.");
                    buffer.Write(block, 0, read);
                }
                var page = ReadPage(new UTF8Encoding(false, true).GetString(buffer.ToArray()), 1);
                if (page.TotalItems != page.Items!.Count || page.TotalItems > ids.Count)
                    throw new InvalidDataException("PocketBase omitted or duplicated part of the bounded native result.");
                var seen = new HashSet<Guid>();
                string? previous = null;
                foreach (var record in page.Items)
                {
                    var row = ParseHolon(record);
                    if (row == null || !ids.Contains(row.Id) || !seen.Add(row.Id) || !plan.Query.ReadAccess.CanRead(row)
                        || previous != null && StringComparer.Ordinal.Compare(previous, record.OasisId) >= 0)
                        throw new InvalidDataException("PocketBase returned an unauthorized, deleted, duplicate or unordered native row.");
                    previous = record.OasisId;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(row);
                }
            }
            // Native selection is complete and bounded before paging; missing
            // and tombstoned rows never consume a requested page position.
            var result = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (result.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(result)) { IsLoaded = true, LoadedCount = result.Length };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "PocketBase query cancelled; no partial result published.", new OperationCanceledException(cancellationToken)); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native PocketBase query failed; no partial result published.", cause); }
    }
}

public static class PocketBaseCqlCompiler
{
    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority) where T : IHolon
        => BoundedIdentityCqlCompiler.Compile(query, authority, ProviderType.PocketBaseOASIS);
}
