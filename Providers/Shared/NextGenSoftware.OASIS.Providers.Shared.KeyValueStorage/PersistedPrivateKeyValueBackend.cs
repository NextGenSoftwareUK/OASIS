using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Mandatory private storage with a separately persisted key; never generates keys or accepts plaintext.</summary>
public sealed class PersistedPrivateKeyValueBackend : IKeyValueBackend, IDisposable
{
    public static byte[] ResolveEnvironmentKey(string variableName)
    {
        var encoded=Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(encoded))
            throw new InvalidOperationException("Private storage encryption key must be configured separately from the signing key.");
        var key=Convert.FromBase64String(encoded);
        if (key.Length!=32 || Convert.ToBase64String(key)!=encoded)
        {
            CryptographicOperations.ZeroMemory(key);
            throw new InvalidOperationException("Storage encryption key must be canonical base64 for exactly 32 bytes.");
        }
        return key;
    }
    private readonly Lazy<PrivateKeyValueBackend> _private;
    private readonly IKeyValueBackend _inner;
    private bool _disposed;
    /// <param name="keyResolver">Returns a caller-owned temporary 32-byte key buffer, zeroed after copying.</param>
    public PersistedPrivateKeyValueBackend(IKeyValueBackend inner, Func<byte[]> keyResolver)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(keyResolver);
        _inner=inner;
        _private = new Lazy<PrivateKeyValueBackend>(() => {
            var key = keyResolver();
            try {
                if (key == null || key.Length != 32)
                    throw new InvalidOperationException("Private ledger storage requires a separately persisted 32-byte encryption key.");
                return new PrivateKeyValueBackend(_inner, key, compress: true);
            } finally { if (key != null) CryptographicOperations.ZeroMemory(key); }
        });
    }
    private PrivateKeyValueBackend Resolve(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _private.Value;
    }
    public Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        => Resolve(cancellationToken).GetAsync(key,cancellationToken);
    public Task PutAsync(string key,string value,CancellationToken cancellationToken = default)
        => Resolve(cancellationToken).PutAsync(key,value,cancellationToken);
    public Task DeleteAsync(string key,CancellationToken cancellationToken = default)
        => Resolve(cancellationToken).DeleteAsync(key,cancellationToken);
    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken cancellationToken = default)
        => Resolve(cancellationToken).ListKeysAsync(prefix,cancellationToken);
    public Task VerifyAsync(CancellationToken cancellationToken = default)
        => Resolve(cancellationToken).VerifyReadOnlyAsync(cancellationToken);
    public void Dispose()
    {
        if (_disposed) return;
        _disposed=true;
        if (_private.IsValueCreated) _private.Value.Dispose();
    }
}
