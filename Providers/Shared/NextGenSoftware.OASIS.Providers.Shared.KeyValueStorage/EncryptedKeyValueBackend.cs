using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Authenticated encrypted values. Keys remain visible; callers own key persistence.</summary>
public sealed class EncryptedKeyValueBackend : IKeyValueBackend, IDisposable
{
    private readonly IKeyValueBackend _inner;
    private readonly EncryptedRecordCodec _codec;
    private bool _disposed;

    public EncryptedKeyValueBackend(IKeyValueBackend inner, byte[] encryptionKey, bool compress = false)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _codec = new EncryptedRecordCodec(encryptionKey, compress);
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        var value = await _inner.GetAsync(key, cancellationToken);
        if(value==null)return null;
        try{return _codec.Decrypt(key,value,cancellationToken);}
        catch(Exception cause) when(cause is System.Security.Cryptography.CryptographicException || cause is FormatException || cause is System.Text.DecoderFallbackException)
        {throw new System.IO.InvalidDataException("Stored encrypted record failed integrity or encoding validation.",cause);}
    }

    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return _inner.PutAsync(key, _codec.Encrypt(key, value, cancellationToken), cancellationToken);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        return _inner.DeleteAsync(key, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ValidateKey(prefix);
        return _inner.ListKeysAsync(prefix, cancellationToken);
    }

    public Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _inner.VerifyAsync(cancellationToken);
    }

    private void ValidateKey(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        new System.Text.UTF8Encoding(false, true).GetByteCount(key);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _codec.Dispose();
    }
}
