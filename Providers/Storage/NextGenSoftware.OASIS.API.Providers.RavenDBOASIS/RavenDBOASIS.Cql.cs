using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

public partial class RavenDBOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly Guid? _queryProbeId;
    protected virtual bool RequiresPublicQueryRecords => false;

    public RavenDBOASIS(string[] serverUrls, string database, Guid queryProbeId)
        : this(serverUrls, database, queryProbeId, null)
    {
    }

    public RavenDBOASIS(string[] serverUrls, string database, Guid queryProbeId, X509Certificate2? clientCertificate)
        : this(serverUrls, database, clientCertificate)
    {
        if (queryProbeId == Guid.Empty) throw new ArgumentException("An explicit teaching record is required.", nameof(queryProbeId));
        _queryProbeId = queryProbeId;
    }

    private sealed class QueryPayloadLimitException : IOException { }

    private static Holon DecodeQueryEnvelope(HolonDoc document, Guid id)
    {
        if (document.Id != "OasisHolons/" + id.ToString("D") || document.DataJson == null)
            throw new InvalidDataException("Native RavenDB envelope identity or payload is invalid.");
        if (Encoding.UTF8.GetByteCount(document.DataJson) > 1024 * 1024)
            throw new QueryPayloadLimitException();
        var row = Des<Holon>(document.DataJson);
        if (row == null || row.Id != id || row.IsDeleted != document.IsDeleted)
            throw new InvalidDataException("Native RavenDB payload identity or deletion state is invalid.");
        return row;
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_queryProbeId is not Guid id)
                throw new InvalidOperationException("An explicit existing RavenDB teaching record is required.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            using var session = GetStore().OpenAsyncSession(new Raven.Client.Documents.Session.SessionOptions
            { NoTracking = true, NoCaching = true });
            var key = "OasisHolons/" + id.ToString("D");
            var rows = await session.LoadAsync<HolonDoc>(new[] { key }, deadline.Token);
            if (!rows.TryGetValue(key, out var envelope) || envelope == null)
                throw new InvalidDataException("The existing RavenDB teaching record was not verified.");
            var row = DecodeQueryEnvelope(envelope, id);
            if (row.IsDeleted || (RequiresPublicQueryRecords && !row.IsPublic))
                throw new InvalidDataException("The existing RavenDB teaching record was not verified.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return new() { IsError = true, ErrorCount = 1,
                ErrorCode = error is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "RavenDB query activation did not verify the existing teaching record.", Exception = error };
        }
    }

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
            var selected = RavenDBCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled)
                return Error("CQL_CAPABILITY_UNSUPPORTED", "RavenDB historical CQL reads are not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated || _store == null)
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
                    var row = DecodeQueryEnvelope(document, id);
                    bytes += Encoding.UTF8.GetByteCount(document.DataJson!);
                    if (bytes > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "Stored RavenDB payload exceeds the admitted byte budget.");
                    if (!plan.Query.ReadAccess.CanRead(row))
                        return Error("CQL_QUERY_FAILED", "Native RavenDB holon differs from read authority.");
                    if (row.IsDeleted) continue;
                    if (RequiresPublicQueryRecords && !row.IsPublic)
                        return Error("CQL_QUERY_FAILED", "Native RavenDB teaching record is no longer public.");
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
        catch (QueryPayloadLimitException error)
        { return Error("CQL_QUERY_LIMIT", "Stored RavenDB payload exceeds the admitted byte budget.", error); }
        catch (Exception error)
        { return Error("CQL_QUERY_FAILED", "Native RavenDB query failed; no partial rows published.", error); }
    }
}
