using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS;

public partial class MinIOOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly Guid? _queryProbeId;
    private readonly Func<IAmazonS3>? _queryClientFactory;
    public Core.Enums.ProviderType QueryProviderType => StorageProviderType;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    private sealed class QueryLimitException : IOException { }

    public void Dispose()
    {
        _s3?.Dispose();
        _s3 = null!;
        IsProviderActivated = false;
    }

    public MinIOOASIS(string endpoint, string accessKey, string secretKey, string bucketName, bool useSSL,
        Guid queryProbeId, IAmazonS3? client = null, Func<IAmazonS3>? clientFactory = null) : this(endpoint, accessKey, secretKey, bucketName, useSSL)
    {
        if (queryProbeId == Guid.Empty) throw new ArgumentException("Explicit live probe identity required.", nameof(queryProbeId));
        if (client != null && clientFactory != null) throw new ArgumentException("Specify one native query client owner.");
        _queryProbeId = queryProbeId;
        _queryClientFactory = clientFactory ?? (() => new AmazonS3Client(_accessKey, _secretKey, new AmazonS3Config {
            ServiceURL = _endpoint, ForcePathStyle = true, UseHttp = !_useSSL, AuthenticationRegion = "us-east-1", MaxErrorRetry = 0 }));
        if (client != null) _s3 = client;
    }

    private async Task<Holon?> ReadQueryRecordAsync(Guid id, Action<int> accountBytes, CancellationToken token)
    {
        GetObjectResponse response;
        try
        {
            response = await _s3.GetObjectAsync(new GetObjectRequest { BucketName = _bucket, Key = $"oasis-holons/{id:D}.json" }, token);
        }
        catch (AmazonS3Exception error) when (error.StatusCode == HttpStatusCode.NotFound && error.ErrorCode == "NoSuchKey")
        { return null; }
        using (response)
        {
            if (response.HttpStatusCode != HttpStatusCode.OK || response.ResponseStream == null)
                throw new InvalidDataException("Native object read did not acknowledge a complete response.");
            if (response.ContentLength > 1024 * 1024) throw new QueryLimitException();
            using var body = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await response.ResponseStream.ReadAsync(buffer, token)) != 0)
            {
                if (body.Length + count > 1024 * 1024) throw new QueryLimitException();
                accountBytes(count);
                body.Write(buffer, 0, count);
            }
            if (response.ContentLength >= 0 && body.Length != response.ContentLength)
                throw new InvalidDataException("Native object response length is incomplete or inconsistent.");
            using var document = JsonDocument.Parse(body.ToArray());
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
            var row = JsonSerializer.Deserialize<Holon>(projection.ToArray(), _jsonOpts);
            if (row == null || row.Id != id) throw new InvalidDataException("Native canonical identity differs from the admitted object key.");
            row.Children = Array.Empty<IHolon>();
            return row;
        }
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            if (_queryProbeId is not Guid probe) throw new InvalidOperationException("An existing live teaching record must be configured.");
            if (!Uri.TryCreate(_endpoint, UriKind.Absolute, out var endpoint) || endpoint.UserInfo.Length != 0 ||
                endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 || endpoint.AbsolutePath != "/" ||
                (endpoint.Scheme != "https" && !(endpoint.Scheme == "http" && endpoint.IsLoopback)) ||
                _useSSL != (endpoint.Scheme == "https"))
                throw new InvalidOperationException("Explicit matching HTTPS or HTTP loopback configuration required.");
            if (string.IsNullOrWhiteSpace(_bucket) || string.IsNullOrWhiteSpace(_accessKey) || string.IsNullOrWhiteSpace(_secretKey))
                throw new InvalidOperationException("Explicit bucket and native source credentials required.");
            if (_s3 == null)
                _s3 = _queryClientFactory!() ?? throw new InvalidOperationException("Native query client factory returned no client.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var row = await ReadQueryRecordAsync(probe, _ => { }, deadline.Token);
            if (row == null || row.IsDeleted) throw new InvalidDataException("The existing live teaching record was not verified.");
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
            if (maximumResults < 1 || maximumResults > 10000) return Error("CQL_QUERY_LIMIT", "Result budget must be between1 and10000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, StorageProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Native object history requires a verified version-selection policy.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated || _s3 == null) return Error("CQL_PROVIDER_INACTIVE", "Activate the configured native object source first.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var bytes = 0;
            var rows = new List<IHolon>();
            foreach (var id in selected.Result.Where(id => plan.Query.ReadAccess.CanRead(id, false, Guid.Empty)))
            {
                var row = await ReadQueryRecordAsync(id, count => { bytes = checked(bytes + count); if (bytes > 4 * 1024 * 1024) throw new QueryLimitException(); }, deadline.Token);
                if (row == null) continue;
                if (!plan.Query.ReadAccess.CanRead(row)) throw new InvalidDataException("Native canonical row differs from read authority.");
                if (!row.IsDeleted) rows.Add(row);
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete native page exceeds result budget.");
            deadline.Token.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException error) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Native object query cancelled; no partial rows published.", error); }
        catch (QueryLimitException error) { return Error("CQL_QUERY_LIMIT", "Native object payload exceeds its byte budget.", error); }
        catch (Exception error) { return Error("CQL_QUERY_FAILED", "Native object query failed; no partial rows published.", error); }
    }
}
