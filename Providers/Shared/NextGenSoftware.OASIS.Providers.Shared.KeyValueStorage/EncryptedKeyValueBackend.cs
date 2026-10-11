using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Authenticated encryption for values stored on publicly readable services.
/// Keys remain visible. The caller owns secure key persistence and rotation.</summary>
public sealed class EncryptedKeyValueBackend : IKeyValueBackend, IDisposable
{
    private const string Format = "oasis-aes256gcm-v1:";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly IKeyValueBackend _inner;
    private readonly byte[] _key;
    private bool _disposed;

    public EncryptedKeyValueBackend(IKeyValueBackend inner, byte[] encryptionKey)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        if (encryptionKey == null || encryptionKey.Length != 32)
            throw new ArgumentException("An externally managed 32-byte encryption key is required.", nameof(encryptionKey));
        _key = (byte[])encryptionKey.Clone();
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        var associatedData = StrictUtf8.GetBytes(Format + key);
        var value = await _inner.GetAsync(key, cancellationToken);
        if (value == null) return null;
        if (!value.StartsWith(Format, StringComparison.Ordinal))
            throw new CryptographicException("Stored value is not in the required encrypted format.");
        var payload = Convert.FromBase64String(value.Substring(Format.Length));
        if (payload.Length < 28) throw new CryptographicException("Encrypted value is truncated.");
        var plaintext = new byte[payload.Length - 28];
        try
        {
            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(payload.AsSpan(0, 12), payload.AsSpan(28), payload.AsSpan(12, 16), plaintext,
                associatedData);
            return StrictUtf8.GetString(plaintext);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        var associatedData = StrictUtf8.GetBytes(Format + key);
        var plaintext = StrictUtf8.GetBytes(value);
        var payload = new byte[28 + plaintext.Length];
        RandomNumberGenerator.Fill(payload.AsSpan(0, 12));
        try
        {
            using var aes = new AesGcm(_key, 16);
            aes.Encrypt(payload.AsSpan(0, 12), plaintext, payload.AsSpan(28), payload.AsSpan(12, 16),
                associatedData);
            await _inner.PutAsync(key, Format + Convert.ToBase64String(payload), cancellationToken);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        StrictUtf8.GetByteCount(key);
        return _inner.DeleteAsync(key, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(prefix);
        StrictUtf8.GetByteCount(prefix);
        return _inner.ListKeysAsync(prefix, cancellationToken);
    }

    public Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _inner.VerifyAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CryptographicOperations.ZeroMemory(_key);
    }
}
