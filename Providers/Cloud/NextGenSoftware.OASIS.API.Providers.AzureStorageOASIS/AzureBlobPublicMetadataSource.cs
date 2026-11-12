using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

/// <summary>
/// Explicit read-only view of native public Azure Blob object metadata. It never downloads
/// object bodies and is separate from encrypted, owned AzureStorageOASIS record storage.
/// </summary>
public sealed class AzureBlobPublicMetadataSource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    public const string ObjectKeyMetadata = "AzureBlobKey";
    private const string Prefix = "public-metadata/";
    private readonly MetadataBackend metadata;
    private bool disposed;

    public AzureBlobPublicMetadataSource(string account, string container, IEnumerable<string> keys, Guid sourceOwner)
        : this(new MetadataBackend(account, container, keys, sourceOwner, null)) { }

    // The production constructor always creates an anonymous Azure SDK client.
    // Controlled tests inject a native client without adding credentials to configuration.
    internal AzureBlobPublicMetadataSource(string account, string container, IEnumerable<string> keys,
        Guid sourceOwner, BlobContainerClient client)
        : this(new MetadataBackend(account, container, keys, sourceOwner, () => client)) { }

    public static AzureBlobPublicMetadataSource FromJson(string json)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > 16384)
            throw new ArgumentException("A bounded Azure public metadata catalogue is required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 4 });
        var root = document.RootElement;
        var names = new[] { "schema", "account", "container", "keys", "sourceOwner" };
        if (root.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("An explicit Azure Blob source object is required.");
        var fields = root.EnumerateObject().ToArray();
        if (fields.Length != names.Length || fields.Select(f => f.Name).Distinct(StringComparer.Ordinal).Count() != names.Length
            || fields.Any(f => !names.Contains(f.Name, StringComparer.Ordinal))
            || root.GetProperty("schema").ValueKind != JsonValueKind.Number
            || !root.GetProperty("schema").TryGetInt32(out var schema) || schema != 1
            || root.GetProperty("account").ValueKind != JsonValueKind.String
            || root.GetProperty("container").ValueKind != JsonValueKind.String
            || root.GetProperty("keys").ValueKind != JsonValueKind.Array
            || root.GetProperty("keys").GetArrayLength() is < 1 or > 8
            || root.GetProperty("keys").EnumerateArray().Any(k => k.ValueKind != JsonValueKind.String)
            || root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String
            || !Guid.TryParseExact(root.GetProperty("sourceOwner").GetString(), "D", out var owner))
            throw new ArgumentException("Exact anonymous Azure catalogue schema required; no endpoints, credentials or SAS.");
        return new(root.GetProperty("account").GetString()!, root.GetProperty("container").GetString()!,
            root.GetProperty("keys").EnumerateArray().Select(k => k.GetString()!).ToArray(), owner);
    }

    private AzureBlobPublicMetadataSource(MetadataBackend backend) : base(backend, Prefix)
    {
        metadata = backend;
        ProviderName = "AzureStorageOASIS";
        ProviderDescription = "Read-only public AWS S3 object metadata; no object-body downloads or owned-record writes";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AzureStorageOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.AzureStorageOASIS;
    public CosmicQueryCapabilities QueryCapabilities { get; } =
        CosmicQueryCapabilities.ForStringMetadataKeyLookup(ObjectKeyMetadata, 512, 8);

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
                ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE", Message = "Public Azure Blob metadata source activation failed.", Exception = error };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure("CQL_PLAN_TARGET_UNAVAILABLE", "One explicitly admitted public Azure Blob source is required.");
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
        catch (OperationCanceledException error) { return Failure("CQL_QUERY_CANCELLED", "Public Azure Blob metadata query cancelled; no partial rows.", error); }
        catch (Exception error) { return Failure("CQL_QUERY_FAILED", "Public Azure Blob metadata query failed; no partial rows.", error); }
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
        private readonly BlobContainerClient client;
        private readonly string account;
        private readonly string container;
        private readonly string sourceName;
        private readonly Guid owner;
        private readonly Dictionary<string, string> records;
        private readonly HashSet<string> keys;

        public MetadataBackend(string account, string container, IEnumerable<string> sourceKeys, Guid owner,
            Func<BlobContainerClient>? createClient)
        {
            if (string.IsNullOrEmpty(account) || account.Length is < 3 or > 24
                || account.Any(c => !char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c))
                || string.IsNullOrEmpty(container) || container.Length is < 3 or > 63
                || container.Any(c => !char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '-')
                || container.StartsWith('-') || container.EndsWith('-') || container.Contains("--", StringComparison.Ordinal)
                || owner == Guid.Empty)
                throw new ArgumentException("A public Azure account, container and nonempty source owner are required.");
            var selected = sourceKeys?.ToArray() ?? throw new ArgumentNullException(nameof(sourceKeys));
            if (selected.Length is < 1 or > 8 || selected.Any(key => string.IsNullOrEmpty(key) || key.Length > 512
                    || key.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '/' or '.' or '='))
                    || key.Split('/').Any(part => part is "" or "." or ".."))
                || selected.Distinct(StringComparer.Ordinal).Count() != selected.Length)
                throw new ArgumentException("Configure one to eight unique, canonical Azure blob names of at most 512 characters.");
            this.account = account; this.container = container; this.owner = owner;
            var options = new BlobClientOptions();
            options.Retry.MaxRetries = 0;
            options.Retry.NetworkTimeout = TimeSpan.FromSeconds(10);
            // No connection string, SAS, account key, default credentials or endpoint override.
            client = createClient == null
                ? new BlobContainerClient(new Uri($"https://{account}.blob.core.windows.net/{container}"), options)
                : createClient() ?? throw new ArgumentNullException(nameof(createClient));
            sourceName = $"azure-public-metadata:{account}/{container}";
            keys = new(selected, StringComparer.Ordinal);
            records = selected.ToDictionary(key => Prefix + "holon/" +
                NativeRecordHolonMapper.StableHolonId(owner, Core.Enums.ProviderType.AzureStorageOASIS, sourceName, key).ToString("N"),
                key => key, StringComparer.Ordinal);
        }

        public bool Contains(string key) => keys.Contains(key);
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            if (await ReadAsync(keys.First(), cancellationToken) == null)
                throw new InvalidDataException("The configured public source probe object is missing.");
        }

        public async Task<IHolon?> ReadAsync(string key, CancellationToken token)
        {
            BlobProperties response;
            try
            {
                var native = await client.GetBlobClient(key).GetPropertiesAsync(cancellationToken: token).ConfigureAwait(false);
                using var raw = native.GetRawResponse();
                if (raw.Status != 200) throw new InvalidDataException("Azure Blob properties were not acknowledged.");
                response = native.Value;
            }
            catch (RequestFailedException error) when (error.Status == 404 && error.ErrorCode == "BlobNotFound") { return null; }
            token.ThrowIfCancellationRequested();
            if (response == null || response.ContentLength < 0 || string.IsNullOrWhiteSpace(response.ETag.ToString())
                || response.LastModified == default || response.BlobType != BlobType.Block)
                throw new InvalidDataException("Native Azure block-blob properties are incomplete.");
            var fields = new Dictionary<string, object>
            {
                ["name"] = key,
                ["description"] = $"Public Azure Blob properties: {account}/{container}/{key}; {response.ContentLength.ToString(CultureInfo.InvariantCulture)} bytes. Object body not downloaded.",
                [ObjectKeyMetadata] = key,
                ["AzureStorageAccount"] = account,
                ["AzureBlobContainer"] = container,
                ["AzureBlobContentLength"] = response.ContentLength.ToString(CultureInfo.InvariantCulture),
                ["AzureBlobContentType"] = response.ContentType ?? "",
                ["AzureBlobETag"] = response.ETag.ToString(),
                ["AzureBlobLastModified"] = response.LastModified.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, Core.Enums.ProviderType.AzureStorageOASIS,
                new NativeRecord(sourceName, key, fields), NativeDataMapping.ForSource(sourceName));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS] = key;
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
            => Task.FromException(new NotSupportedException("Public Azure Blob object metadata is read-only."));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromException(new NotSupportedException("Public Azure Blob object metadata is read-only."));
        public void Dispose() { } // Azure SDK clients are reusable and do not own disposable transport handles here.
    }
}
