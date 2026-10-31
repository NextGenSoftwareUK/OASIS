using Amazon.S3;
using Amazon.S3.Model;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AWSS3OASIS;

/// <summary>
/// Explicit read-only view of native public S3 object metadata. It never downloads
/// object bodies and is separate from encrypted, owned AWSS3OASIS record storage.
/// </summary>
public sealed class AwsS3PublicMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string ObjectKeyMetadata = "S3ObjectKey";
    private const string Prefix = "public-metadata/";
    private readonly MetadataBackend metadata;
    private bool disposed;

    public AwsS3PublicMetadataSource(IAmazonS3 client, string bucket, IEnumerable<string> keys,
        Guid sourceOwner, bool ownsClient = false)
        : this(new MetadataBackend(client, bucket, keys, sourceOwner, ownsClient)) { }

    private AwsS3PublicMetadataSource(MetadataBackend backend) : base(backend, Prefix)
    {
        metadata = backend;
        ProviderName = "AWSS3OASIS";
        ProviderDescription = "Read-only public AWS S3 object metadata; no object-body downloads or owned-record writes";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AWSS3OASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.AWSS3OASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; } =
        CosmicQueryCapabilities.ForStringMetadataKeyLookup(ObjectKeyMetadata, 128, 8);

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            await metadata.VerifyAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = error is OperationCanceledException
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public S3 metadata source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public S3 source is required.");
            if (maximumResults is < 1 or > 10000)
                return Failure("CQL_QUERY_LIMIT", "The result budget must be between one and 10,000.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Failure(admitted.ErrorCode, admitted.Message);

            IReadOnlyList<string> keys;
            if (QueryCapabilities.StringKeySetLookup.IsKeySetQuery(plan.Query))
            {
                var selection = QueryCapabilities.StringKeySetLookup.SelectKeys(plan.Query);
                if (selection.IsError) return Failure(selection.ErrorCode, selection.Message);
                keys = selection.Result;
            }
            else
            {
                var selection = QueryCapabilities.StringPointLookup.SelectKey(plan.Query);
                if (selection.IsError) return Failure(selection.ErrorCode, selection.Message);
                keys = new[] { selection.Result };
            }
            if (keys.Any(key => !metadata.Contains(key)))
                return Failure("CQL_INVALID_SOURCE_KEY", "Select keys from the configured public metadata catalogue.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Failure("CQL_PROVIDER_INACTIVE", "Explicitly activate this public source before querying.");

            var rows = new List<IHolon>();
            foreach (var key in keys)
            {
                var row = await metadata.ReadAsync(key, cancellationToken);
                if (row != null && (plan.Query.ReadAccess == null || plan.Query.ReadAccess.CanRead(row))) rows.Add(row);
            }
            // Finish and validate the selected source before publishing any page.
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Failure("CQL_QUERY_LIMIT", "The complete selected page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Public S3 metadata query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public S3 metadata query failed; no partial rows.", error); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        IsProviderActivated = false;
        metadata.Dispose();
    }

    private static OASISResult<IReadOnlyList<IHolon>> Failure(string code, string message, Exception? error = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    private sealed class MetadataBackend : IKeyValueBackend, IDisposable
    {
        private readonly IAmazonS3 client;
        private readonly string bucket;
        private readonly string sourceName;
        private readonly Guid owner;
        private readonly bool ownsClient;
        private readonly Dictionary<string, string> records;
        private readonly HashSet<string> keys;

        public MetadataBackend(IAmazonS3 client, string bucket, IEnumerable<string> sourceKeys, Guid owner, bool ownsClient)
        {
            ArgumentNullException.ThrowIfNull(client);
            if (string.IsNullOrWhiteSpace(bucket) || bucket.Length is < 3 or > 63 ||
                bucket.Any(character => !char.IsAsciiLetterLower(character) && !char.IsAsciiDigit(character) && character is not ('-' or '.')) ||
                bucket.Contains("..", StringComparison.Ordinal) || owner == Guid.Empty)
                throw new ArgumentException("An explicit general-purpose public bucket and source owner are required.");
            var selected = sourceKeys?.ToArray() ?? throw new ArgumentNullException(nameof(sourceKeys));
            if (selected.Length is < 1 or > 8 || selected.Any(key => string.IsNullOrEmpty(key) || key.Length > 128 || key.Any(c => c < 33 || c > 126)) ||
                selected.Distinct(StringComparer.Ordinal).Count() != selected.Length)
                throw new ArgumentException("Configure one to eight unique bounded public object keys.");
            this.client = client; this.bucket = bucket; this.owner = owner; this.ownsClient = ownsClient;
            sourceName = "s3-public-metadata:" + bucket;
            keys = new(selected, StringComparer.Ordinal);
            records = selected.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, Core.Enums.ProviderType.AWSS3OASIS, sourceName, key).ToString("N"), key => key, StringComparer.Ordinal);
        }

        public bool Contains(string key) => keys.Contains(key);
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            if (await ReadAsync(keys.First(), cancellationToken) == null)
                throw new InvalidDataException("The configured public source probe object is missing.");
        }

        public async Task<IHolon?> ReadAsync(string key, CancellationToken token)
        {
            GetObjectMetadataResponse response;
            try { response = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = bucket, Key = key }, token); }
            catch (AmazonS3Exception error) when (error.StatusCode == System.Net.HttpStatusCode.NotFound) { return null; }
            token.ThrowIfCancellationRequested();
            if (response == null || response.ContentLength < 0 || string.IsNullOrWhiteSpace(response.ETag))
                throw new InvalidDataException("Native S3 metadata is incomplete.");
            var fields = new Dictionary<string, object>
            {
                ["name"] = key,
                ["description"] = $"Public S3 object metadata: s3://{bucket}/{key}; {response.ContentLength} bytes. Object body not downloaded.",
                [ObjectKeyMetadata] = key,
                ["S3Bucket"] = bucket,
                ["S3ContentLength"] = response.ContentLength,
                ["S3ContentType"] = response.Headers.ContentType ?? "",
                ["S3ETag"] = response.ETag,
                ["S3LastModified"] = response.LastModified.ToUniversalTime()
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, Core.Enums.ProviderType.AWSS3OASIS,
                new NativeRecord(sourceName, key, fields), NativeDataMapping.ForSource(sourceName));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[Core.Enums.ProviderType.AWSS3OASIS] = key;
            return row;
        }

        public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!records.TryGetValue(key, out var nativeKey)) return null;
            var row = await ReadAsync(nativeKey, cancellationToken);
            return row == null ? null : StoredRecordJson.Serialize<IHolon>(row);
        }

        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<string>>(Array.AsReadOnly(records.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray()));
        }

        // This source views external public objects; mutations are deliberately unsupported.
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public S3 object metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public S3 object metadata is read-only."));
        public void Dispose() { if (ownsClient) client.Dispose(); }
    }
}
