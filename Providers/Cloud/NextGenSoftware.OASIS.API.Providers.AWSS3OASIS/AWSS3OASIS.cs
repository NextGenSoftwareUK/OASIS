using Amazon.S3;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.Shared.S3;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AWSS3OASIS;

/// <summary>Private OASIS records over an existing general-purpose AWS S3 bucket.
/// SDK transport is shared; canonical record/query/history/encryption handling is not duplicated.</summary>
public sealed class AWSS3OASIS : KeyValueStorageProviderBase, IDisposable
{
    private readonly PersistedPrivateKeyValueBackend _private;
    private readonly IAmazonS3 _client;
    private readonly bool _ownsClient;
    private bool _disposed;

    public AWSS3OASIS(IAmazonS3 client, string bucketName, Func<byte[]> storageKeyResolver,
        string keyPrefix = "oasis/", bool ownsClient = false)
        : this(client, PersistedPrivateKeyValueBackend.Create(new S3ObjectBackend(client, bucketName, keyPrefix), storageKeyResolver), keyPrefix, ownsClient) { }

    private AWSS3OASIS(IAmazonS3 client, PersistedPrivateKeyValueBackend backend, string prefix, bool ownsClient)
        : base(backend, prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        _client = client; _private = backend; _ownsClient = ownsClient;
        ProviderName = "AWSS3OASIS";
        ProviderDescription = "AWS S3 storage with encrypted OASIS records and opaque keyed indexes";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AWSS3OASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
    }

    protected override Task<OASISResult<IEnumerable<IAvatar>>> LoadAvatarsForCredentialSelectionAsync(int version)
    {
        if (version != 0) throw new NotSupportedException("S3 credential authentication requires latest version zero; archived credentials cannot authenticate an avatar.");
        return base.LoadAvatarsForCredentialSelectionAsync(version);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; IsProviderActivated = false;
        _private.Dispose();
        if (_ownsClient) _client.Dispose();
    }
}
