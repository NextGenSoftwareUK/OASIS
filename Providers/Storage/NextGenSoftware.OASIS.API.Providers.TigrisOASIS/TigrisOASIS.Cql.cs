using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TigrisOASIS;

public partial class TigrisOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly Guid? _queryProbeId;
    private readonly bool _requirePublicQueryRecords;
    public Core.Enums.ProviderType QueryProviderType => Core.Enums.ProviderType.TigrisOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public TigrisOASIS(string accessKey, string secretKey, string bucketName, string endpoint, Guid queryProbeId)
        : this(new S3ObjectBackend(CreateClient(accessKey, secretKey, endpoint, boundedQuery: true), bucketName), queryProbeId)
    { }

    public TigrisOASIS(string accessKey, string secretKey, string bucketName, string endpoint, Guid queryProbeId,
        Amazon.Runtime.HttpClientFactory publicQueryTransport)
        : this(new S3ObjectBackend(CreateClient(accessKey, secretKey, endpoint, boundedQuery: true,
            httpClientFactory: publicQueryTransport ?? throw new ArgumentNullException(nameof(publicQueryTransport))), bucketName),
            queryProbeId, requirePublicQueryRecords: true)
    { }

    internal TigrisOASIS(S3ObjectBackend backend, Guid queryProbeId, bool requirePublicQueryRecords = false) : this(backend)
    {
        if (queryProbeId == Guid.Empty) throw new ArgumentException("Explicit existing live probe required.");
        _queryProbeId = queryProbeId;
        _requirePublicQueryRecords = requirePublicQueryRecords;
    }

    private async Task<Holon?> ReadQueryRecordAsync(Guid id, Action<int> accountBytes, CancellationToken token)
    {
        var json = await _objectBackend.GetBoundedAsync(RecordKey("holon", id), 1024 * 1024, accountBytes, token);
        if (json == null) return null;
        using var document = JsonDocument.Parse(json);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var projection = new MemoryStream();
        using (var writer = new Utf8JsonWriter(projection))
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate native canonical property.");
                if (!property.Name.Equals("Children", StringComparison.OrdinalIgnoreCase)) property.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        var row = OasisJson.DeserializeStored<Holon>(System.Text.Encoding.UTF8.GetString(projection.ToArray()));
        if (row == null || row.Id != id) throw new InvalidDataException("Native canonical identity differs from the admitted object key.");
        row.Children = Array.Empty<IHolon>();
        return row;
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            if (_queryProbeId is not Guid probe) throw new InvalidOperationException("An existing live teaching record must be configured.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var row = await ReadQueryRecordAsync(probe, _ => { }, deadline.Token);
            if (row == null || row.IsDeleted || (_requirePublicQueryRecords && !row.IsPublic))
                throw new InvalidDataException("The existing live teaching record was not verified.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "Native object source activation did not verify the configured teaching record.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string? code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit configured object-store target required.");
            if (maximumResults < 1 || maximumResults > 10000) return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Native object history requires a verified version-selection policy.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Error("CQL_PROVIDER_INACTIVE", "Activate the configured native object source first.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var bytes = 0;
            var rows = new List<IHolon>();
            foreach (var id in selected.Result.Where(id => plan.Query.ReadAccess.CanRead(id, false, Guid.Empty)))
            {
                var row = await ReadQueryRecordAsync(id, count => { bytes = checked(bytes + count); if (bytes > 4 * 1024 * 1024) throw new S3ObjectBackend.ObjectPayloadLimitException(); }, deadline.Token);
                if (row == null) continue;
                if (!plan.Query.ReadAccess.CanRead(row)) throw new InvalidDataException("Native canonical row differs from read authority.");
                if (row.IsDeleted) continue;
                if (_requirePublicQueryRecords && !row.IsPublic)
                    throw new InvalidDataException("Native teaching record is no longer public.");
                rows.Add(row);
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete native page exceeds result budget.");
            deadline.Token.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Native object query cancelled; no partial rows published.", error); }
        catch (S3ObjectBackend.ObjectPayloadLimitException error) { return Error("CQL_QUERY_LIMIT", "Native object payload exceeds its byte budget.", error); }
        catch (Exception error) { return Error("CQL_QUERY_FAILED", "Native object query failed; no partial rows published.", error); }
    }
}
