using System;
using System.Threading.Tasks;
using Amazon.S3;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.Shared.S3;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS;

/// <summary>MinIO identity/configuration over shared encrypted S3 storage.
/// Legacy plaintext objects are refused without mutation; configure a separately persisted key.</summary>
public class MinIOOASIS : S3StorageProviderBase
{
    private static readonly string[] LegacyPrefixes = ["oasis-avatars/", "oasis-avatar-details/", "oasis-holons/", "oasis-indexes/"];
    protected virtual Core.Enums.ProviderType StorageProviderType => Core.Enums.ProviderType.MinIOOASIS;

    public MinIOOASIS(string endpoint, string accessKey, string secretKey, string bucketName = "oasis", bool useSSL = false)
        : this(endpoint, accessKey, secretKey,
            () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_MINIO_STORAGE_KEY"), bucketName, useSSL) { }

    public MinIOOASIS(string endpoint, string accessKey, string secretKey, Func<byte[]> storageKeyResolver,
        string bucketName = "oasis", bool useSSL = false)
        : this(CreateClient(endpoint, accessKey, secretKey, bucketName, storageKeyResolver, useSSL), bucketName,
            storageKeyResolver, ownsClient: true) { }

    public MinIOOASIS(IAmazonS3 client, string bucketName = "oasis")
        : this(client, bucketName, () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_MINIO_STORAGE_KEY")) { }

    public MinIOOASIS(IAmazonS3 client, string bucketName, Func<byte[]> storageKeyResolver,
        string keyPrefix = "oasis/", bool ownsClient = false)
        : base(client, bucketName, storageKeyResolver, keyPrefix, ownsClient, LegacyPrefixes)
    {
        ProviderName = "MinIOOASIS";
        ProviderDescription = "MinIO encrypted storage using shared S3 transport and OASIS record policy";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MinIOOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
    }

    private static IAmazonS3 CreateClient(string endpoint, string accessKey, string secretKey, string bucket,
        Func<byte[]> resolver, bool useSSL)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentNullException.ThrowIfNull(resolver);
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
            || uri.UserInfo != "" || uri.Query != "" || uri.Fragment != "" || (uri.Scheme == "https") != useSSL)
            throw new ArgumentException("An explicit HTTP(S) endpoint matching useSSL is required.", nameof(endpoint));
        return new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
            { ServiceURL = endpoint, ForcePathStyle = true, UseHttp = !useSSL, MaxErrorRetry = 0, AuthenticationRegion = "us-east-1" });
    }

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => DeleteHolonRecordAsync(id, true);
    public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
    {
        var deleted = await DeleteHolonRecordAsync(id, softDelete);
        var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, bool>(deleted);
        result.Result = !deleted.IsError && deleted.IsDeleted;
        return result;
    }
    public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true)
        => DeleteHolonSoftAsync(id, softDelete).GetAwaiter().GetResult();
    public async Task<OASISResult<bool>> DeleteHolonSoftAsync(string providerKey, bool softDelete = true)
    {
        var deleted = await DeleteHolonRecordAsync(providerKey, softDelete);
        var result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon, bool>(deleted);
        result.Result = !deleted.IsError && deleted.IsDeleted;
        return result;
    }
    public OASISResult<bool> DeleteHolonSoft(string providerKey, bool softDelete = true)
        => DeleteHolonSoftAsync(providerKey, softDelete).GetAwaiter().GetResult();
    public string GetProviderVersion() => "1.0.0";
    public Task<string> GetProviderVersionAsync() => Task.FromResult(GetProviderVersion());
}
