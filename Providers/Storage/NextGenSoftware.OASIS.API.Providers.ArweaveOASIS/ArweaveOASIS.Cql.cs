using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS;

public partial class ArweaveOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.ArweaveOASIS;
    public CosmicQueryCapabilities QueryCapabilities => ArweaveCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Arweave target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = ArweaveCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Arweave provider first.");
            var rows = new List<IHolon>();
            long payloadBytes = 0;
            foreach (var id in selected.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Pin tag discovery to the configured signing wallet's native owner.
                // A public transaction with a copied GUID cannot impersonate its record.
                var tags = new Dictionary<string,string> { ["OASIS-Type"] = "Holon", ["OASIS-Id"] = id.ToString("D") };
                var ids = await _arweaveService.QueryOwnedByTagsAsync(tags);
                cancellationToken.ThrowIfCancellationRequested();
                if (ids.Count == 0) continue;
                if (ids.Count != 1)
                    return Error("CQL_REVISION_AMBIGUOUS", "Multiple native revisions require an explicit revision policy; none was silently selected.");
                var tombstones = await _arweaveService.QueryOwnedByTagsAsync(new() {
                    ["OASIS-Type"] = "Holon-Tombstone", ["OASIS-Id"] = id.ToString("D") });
                cancellationToken.ThrowIfCancellationRequested();
                if (tombstones.Count != 0) continue;
                var transactionTombstones = await _arweaveService.QueryOwnedByTagsAsync(new() {
                    ["OASIS-Type"] = "Holon-Tombstone", ["OASIS-Original-TxId"] = ids[0] });
                cancellationToken.ThrowIfCancellationRequested();
                if (transactionTombstones.Count != 0) continue;
                var bytes = await _arweaveService.GetTransactionDataAsync(ids[0]);
                cancellationToken.ThrowIfCancellationRequested();
                payloadBytes = checked(payloadBytes + bytes.Length);
                if (payloadBytes > 4 * 1024 * 1024)
                    return Error("CQL_QUERY_LIMIT", "The complete decoded payload exceeds 4 MiB.");
                var row = JsonConvert.DeserializeObject<Holon>(new UTF8Encoding(false, true).GetString(bytes));
                if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Native Arweave record differs from the authorized identity.");
                if (row.IsDeleted) continue;
                row.Children = Array.Empty<IHolon>();
                rows.Add(row);
            }
            // Moving native reads; no gateway-wide or cross-provider snapshot claimed.
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Arweave query cancelled; no partial result published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Arweave query failed; no partial result published.", cause); }
    }
}
