using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.RavenDBOASIS;

public partial class RavenDBOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.RavenDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? error = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit RavenDB target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled)
                return Error("CQL_CAPABILITY_UNSUPPORTED", "RavenDB historical CQL reads are not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (_store == null)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured RavenDB store before querying.");
            var ids = selected.Result.Where(id => plan.Query.ReadAccess.CanRead(id, false, Guid.Empty)).ToArray();
            var rows = new List<IHolon>();
            if (ids.Length != 0)
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(15));
                using var session = _store.OpenAsyncSession(new Raven.Client.Documents.Session.SessionOptions
                { NoTracking = true, NoCaching = true });
                var keys = ids.Select(id => "OasisHolons/" + id.ToString("D")).ToArray();
                var documents = await session.LoadAsync<HolonDoc>(keys, deadline.Token);
                long bytes = 0;
                foreach (var id in ids)
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    var key = "OasisHolons/" + id.ToString("D");
                    if (!documents.TryGetValue(key, out var document) || document == null) continue;
                    if (document.Id != key || document.DataJson == null)
                        return Error("CQL_QUERY_FAILED", "Native RavenDB envelope identity or payload is invalid.");
                    var size = Encoding.UTF8.GetByteCount(document.DataJson);
                    bytes += size;
                    if (size > 1024 * 1024 || bytes > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "Stored RavenDB payload exceeds the admitted byte budget.");
                    var row = Des<Holon>(document.DataJson);
                    if (row == null || row.Id != id || row.IsDeleted != document.IsDeleted
                        || !plan.Query.ReadAccess.CanRead(row))
                        return Error("CQL_QUERY_FAILED", "Native RavenDB holon differs from read authority.");
                    if (row.IsDeleted) continue;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(row);
                }
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete RavenDB page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "RavenDB query cancelled; no partial rows published.", error); }
        catch (Exception error)
        { return Error("CQL_QUERY_FAILED", "Native RavenDB query failed; no partial rows published.", error); }
    }
}
