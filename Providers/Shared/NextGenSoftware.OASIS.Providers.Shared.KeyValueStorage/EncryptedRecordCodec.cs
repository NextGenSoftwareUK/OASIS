using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Authenticated encryption for values stored on publicly readable services.
/// Logical identity is authenticated as associated data, not included in the envelope.
/// The caller owns secure key persistence and rotation.</summary>
public sealed class EncryptedRecordCodec : IDisposable
{
    private const string Format = "oasis-aes256gcm-v1:";
    private const string CompressedFormat = "oasis-aes256gcm-gzip-v2:";
    private const int MaximumExpandedBytes = 4 * 1024 * 1024;
    private readonly bool _compress;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly byte[] _key;
    private bool _disposed;

    public EncryptedRecordCodec(byte[] encryptionKey, bool compress = false)
    {
        if (encryptionKey == null || encryptionKey.Length != 32)
            throw new ArgumentException("An externally managed 32-byte encryption key is required.", nameof(encryptionKey));
        _key = (byte[])encryptionKey.Clone();
        _compress = compress;
    }

    public string Decrypt(string key, string value, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        StrictUtf8.GetByteCount(key);
        ArgumentNullException.ThrowIfNull(value);
        var format = value.StartsWith(CompressedFormat, StringComparison.Ordinal) ? CompressedFormat : Format;
        if (!value.StartsWith(format, StringComparison.Ordinal))
            throw new CryptographicException("Stored value is not in the required encrypted format.");
        var associatedData = StrictUtf8.GetBytes(format + key);
        var payload = Convert.FromBase64String(value.Substring(format.Length));
        if (payload.Length < 28) throw new CryptographicException("Encrypted value is truncated.");
        var plaintext = new byte[payload.Length - 28];
        try
        {
            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(payload.AsSpan(0, 12), payload.AsSpan(28), payload.AsSpan(12, 16), plaintext,
                associatedData);
            if (format == Format) return StrictUtf8.GetString(plaintext);
            using var input = new MemoryStream(plaintext, writable: false);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var expanded = new MemoryStream();
            var buffer = new byte[8192];
            try
            {
                int count;
                while ((count = gzip.Read(buffer, 0, buffer.Length)) != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (expanded.Length + count > MaximumExpandedBytes)
                        throw new InvalidDataException("Compressed encrypted record exceeds its expansion limit.");
                    expanded.Write(buffer, 0, count);
                }
                return StrictUtf8.GetString(expanded.GetBuffer(), 0, checked((int)expanded.Length));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(buffer);
                if (expanded.TryGetBuffer(out var bytes)) CryptographicOperations.ZeroMemory(bytes.AsSpan());
            }
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    public string Encrypt(string key, string value, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        var format = _compress ? CompressedFormat : Format;
        var associatedData = StrictUtf8.GetBytes(format + key);
        var plaintext = StrictUtf8.GetBytes(value);
        if (_compress)
        {
            try
            {
                if (plaintext.Length > MaximumExpandedBytes)
                    throw new InvalidDataException("Encrypted record exceeds its compression input limit.");
                using var compressed = new MemoryStream();
                try
                {
                    using (var gzip = new GZipStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
                        gzip.Write(plaintext);
                    var original = plaintext;
                    plaintext = compressed.ToArray();
                    CryptographicOperations.ZeroMemory(original);
                }
                finally
                {
                    if (compressed.TryGetBuffer(out var bytes)) CryptographicOperations.ZeroMemory(bytes.AsSpan());
                }
            }
            catch { CryptographicOperations.ZeroMemory(plaintext); throw; }
        }
        var payload = new byte[28 + plaintext.Length];
        RandomNumberGenerator.Fill(payload.AsSpan(0, 12));
        try
        {
            using var aes = new AesGcm(_key, 16);
            aes.Encrypt(payload.AsSpan(0, 12), plaintext, payload.AsSpan(28), payload.AsSpan(12, 16),
                associatedData);
            return format + Convert.ToBase64String(payload);
        }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CryptographicOperations.ZeroMemory(_key);
    }
}
