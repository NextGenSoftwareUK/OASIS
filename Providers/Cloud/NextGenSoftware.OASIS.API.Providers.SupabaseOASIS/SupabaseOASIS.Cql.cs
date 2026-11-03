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

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;

public partial class SupabaseOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => ProviderType.Value;
    private static readonly CosmicQueryCapabilities UnsupportedAlias = new(CosmicQueryFeatures.None,
        Array.Empty<CosmicQueryFieldCapability>());
    public CosmicQueryCapabilities QueryCapabilities => QueryProviderType == Core.Enums.ProviderType.SupabaseOASIS
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
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Supabase target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = SupabaseCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Supabase provider before querying.");
            var rows = new List<IHolon>();
            var ids = selection.Result.ToHashSet();
            if (ids.Count > 0)
            {
                // Native primary-key selection, not a whole-table managed filter.
                var filter = "id=in.(" + string.Join(",", selection.Result.Select(id => id.ToString("D")))
                    + ")&is_deleted=eq.false&order=id.asc&limit=100";
                var native = await ReadCompleteHolonRows(filter, cancellationToken);
                if (native.Count > ids.Count) throw new InvalidDataException("Supabase exceeded the admitted selection.");
                var seen = new HashSet<Guid>();
                string? previous = null;
                foreach (var record in native)
                {
                    var row = ParseHolon(record);
                    if (row == null || !ids.Contains(row.Id) || !seen.Add(row.Id) || !plan.Query.ReadAccess.CanRead(row)
                        || previous != null && StringComparer.Ordinal.Compare(previous, record.id) >= 0)
                        throw new InvalidDataException("Supabase returned an unauthorized, deleted, duplicate or unordered native row.");
                    previous = record.id;
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
        { return Error("CQL_QUERY_CANCELLED", "Supabase query cancelled; no partial result published.", new OperationCanceledException(cancellationToken)); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Supabase query failed; no partial result published.", cause); }
    }
}

public static class SupabaseCqlCompiler
{
    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority) where T : IHolon
        => BoundedIdentityCqlCompiler.Compile(query, authority, ProviderType.SupabaseOASIS);
}
