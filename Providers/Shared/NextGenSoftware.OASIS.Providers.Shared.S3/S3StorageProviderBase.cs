using Amazon.S3;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.Providers.Shared.S3;

/// <summary>Private OASIS records over an existing S3-compatible bucket.
/// SDK transport is shared; canonical record/query/history/encryption handling is not duplicated.</summary>
public abstract class S3StorageProviderBase : KeyValueStorageProviderBase, IDisposable
{
    private readonly PersistedPrivateKeyValueBackend _private;
    private readonly IAmazonS3 _client;
    private readonly bool _ownsClient;
    private bool _disposed;

    protected S3StorageProviderBase(IAmazonS3 client, string bucketName, Func<byte[]> storageKeyResolver,
        string keyPrefix = "oasis/", bool ownsClient = false, IReadOnlyList<string>? legacyPrefixes = null)
        : this(client, PersistedPrivateKeyValueBackend.Create(new S3ObjectBackend(client, bucketName, keyPrefix, legacyPrefixes ?? Array.Empty<string>()), storageKeyResolver), keyPrefix, ownsClient) { }

    private S3StorageProviderBase(IAmazonS3 client, PersistedPrivateKeyValueBackend backend, string prefix, bool ownsClient)
        : base(backend, prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        _client = client; _private = backend; _ownsClient = ownsClient;
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
