using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>Mandatory private storage composition using the existing SDK client and externally persisted encryption key.</summary>
public sealed class TronPrivateStorageBackend : IKeyValueBackend, IDisposable
{
    private readonly Lazy<PrivateKeyValueBackend> _backend;
    private bool _disposed;
    internal TronNativeTransferClient Client { get; }
    private readonly string _owner,_contract;
    private readonly Func<string> _signingKeyResolver;
    private readonly Func<byte[]> _encryptionKeyResolver;

    /// <param name="encryptionKeyResolver">Returns an owned 32-byte buffer, cleared after copying into the encryption layer.</param>
    public TronPrivateStorageBackend(TronNativeTransferClient client, string owner, string contract,
        Func<string> signingKeyResolver, Func<byte[]> encryptionKeyResolver)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(signingKeyResolver);
        ArgumentNullException.ThrowIfNull(encryptionKeyResolver);
        Client=client;_owner=owner;_contract=contract;
        _signingKeyResolver=signingKeyResolver;_encryptionKeyResolver=encryptionKeyResolver;
        var storage = new TronStorageBackend(client, owner, contract, signingKeyResolver);
        _backend = new Lazy<PrivateKeyValueBackend>(() =>
        {
            var key = encryptionKeyResolver();
            try
            {
                if (key == null || key.Length != 32)
                    throw new InvalidOperationException("TRON storage requires an externally persisted 32-byte encryption key.");
                return new PrivateKeyValueBackend(storage, key, compress: true);
            }
            finally { if (key != null) CryptographicOperations.ZeroMemory(key); }
        });
    }

    internal TronPrivateStorageBackend CreatePreparedView(TronNativeTransferClient client)
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        return new TronPrivateStorageBackend(client,_owner,_contract,_signingKeyResolver,_encryptionKeyResolver);
    }

    private PrivateKeyValueBackend Resolve(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _backend.Value;
    }

    public Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        => Resolve(cancellationToken).GetAsync(key, cancellationToken);
    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => Resolve(cancellationToken).PutAsync(key, value, cancellationToken);
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => Resolve(cancellationToken).DeleteAsync(key, cancellationToken);
    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        => Resolve(cancellationToken).ListKeysAsync(prefix, cancellationToken);
    public Task VerifyAsync(CancellationToken cancellationToken = default)
        => Resolve(cancellationToken).VerifyReadOnlyAsync(cancellationToken);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_backend.IsValueCreated) _backend.Value.Dispose();
    }
}
