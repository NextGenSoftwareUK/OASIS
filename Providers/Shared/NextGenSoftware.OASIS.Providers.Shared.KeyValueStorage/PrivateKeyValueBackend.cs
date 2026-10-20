using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Hides logical keys and values on publicly readable stores.
/// Record counts, sizes and access patterns remain public.</summary>
public class PrivateKeyValueBackend : IKeyValueBackend, IDisposable
{
    public static PrivateKeyValueBackend Create(IKeyValueBackend inner,byte[] key,bool compress=false)
        =>inner is IConditionalKeyValueBackend?new Conditional(inner,key,compress):new PrivateKeyValueBackend(inner,key,compress);
    private const string PhysicalPrefix = "oasis/private/v1/";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly EncryptedKeyValueBackend _encrypted;
    private readonly byte[] _indexKey;
    private bool _disposed;

    public PrivateKeyValueBackend(IKeyValueBackend inner, byte[] encryptionKey, bool compress = false)
    {
        _encrypted = EncryptedKeyValueBackend.Create(inner, encryptionKey, compress);
        _indexKey = HMACSHA256.HashData(encryptionKey, Encoding.UTF8.GetBytes("OASIS private storage index v1"));
    }

    private string PhysicalKey(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(key);
        return PhysicalPrefix + Convert.ToHexString(HMACSHA256.HashData(_indexKey, StrictUtf8.GetBytes(key))).ToLowerInvariant();
    }

    private static (string Key, string Value) Parse(string payload)
    {
        try
        {
            using var record=JsonDocument.Parse(payload);
            var root=record.RootElement;
            if(root.ValueKind!=JsonValueKind.Object || !root.TryGetProperty("key",out var key)
                || !root.TryGetProperty("value",out var value) || key.ValueKind!=JsonValueKind.String || value.ValueKind!=JsonValueKind.String)
                throw new InvalidDataException("Private storage record requires string key/value fields.");
            return (key.GetString(),value.GetString());
        }
        catch(JsonException cause){throw new InvalidDataException("Private storage record JSON is invalid.",cause);}
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var payload = await _encrypted.GetAsync(PhysicalKey(key), cancellationToken);
        if (payload == null) return null;
        return ReadLogicalValue(key,payload);
    }

    private static string ReadLogicalValue(string key,string payload)
    {
        var record = Parse(payload);
        if (!string.Equals(record.Key, key, StringComparison.Ordinal))
            throw new InvalidDataException("Private storage record identity mismatch.");
        return record.Value;
    }

    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        return _encrypted.PutAsync(PhysicalKey(key),EncodeValue(key,value),cancellationToken);
    }

    private static string EncodeValue(string key,string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        StrictUtf8.GetByteCount(value);
        return JsonSerializer.Serialize(new { key, value });
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => _encrypted.DeleteAsync(PhysicalKey(key), cancellationToken);

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(prefix);
        StrictUtf8.GetByteCount(prefix);
        var physicalKeys = await _encrypted.ListKeysAsync(PhysicalPrefix, cancellationToken);
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var physicalKey in physicalKeys)
        {
            var payload = await _encrypted.GetAsync(physicalKey, cancellationToken);
            if (payload == null) throw new InvalidDataException("Private storage changed during enumeration.");
            var record = Parse(payload);
            if (!string.Equals(PhysicalKey(record.Key), physicalKey, StringComparison.Ordinal))
                throw new InvalidDataException("Private storage index identity mismatch.");
            if (record.Key.StartsWith(prefix, StringComparison.Ordinal) && !keys.Add(record.Key))
                throw new InvalidDataException("Private storage returned duplicate logical keys.");
        }
        return new List<string>(keys);
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        var existing = await _encrypted.ListKeysAsync(PhysicalPrefix, cancellationToken);
        if (existing.Count > 0)
        {
            var payload = await _encrypted.GetAsync(existing[0], cancellationToken);
            if (payload == null) throw new InvalidDataException("Private storage changed during activation.");
            var record = Parse(payload);
            if (!string.Equals(PhysicalKey(record.Key), existing[0], StringComparison.Ordinal))
                throw new InvalidDataException("Private storage index identity mismatch during activation.");
        }
        var key = "oasis/probe/" + Guid.NewGuid().ToString("N");
        var value = Guid.NewGuid().ToString("N");
        await PutAsync(key, value, cancellationToken);
        try
        {
            if (await GetAsync(key, cancellationToken) != value)
                throw new InvalidDataException("Private storage committed readback did not match its value.");
        }
        finally { await DeleteAsync(key, cancellationToken); }
        if (await GetAsync(key, cancellationToken) != null)
            throw new InvalidDataException("Private storage probe deletion was not reflected by the service.");
    }

    /// <summary>Validate transport and existing encrypted records without ledger writes or fee permission.</summary>
    public async Task VerifyReadOnlyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _encrypted.VerifyAsync(cancellationToken);
        await ListKeysAsync(string.Empty, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CryptographicOperations.ZeroMemory(_indexKey);
        _encrypted.Dispose();
    }

    private sealed class Conditional:PrivateKeyValueBackend,IConditionalKeyValueBackend
    {
        private readonly IConditionalKeyValueBackend _native;
        public Conditional(IKeyValueBackend inner,byte[] key,bool compress):base(inner,key,compress)
            =>_native=(IConditionalKeyValueBackend)_encrypted;
        public async Task<KeyValueObservation> ReadObservedAsync(string key,CancellationToken token=default)
        {
            token.ThrowIfCancellationRequested();var observed=await _native.ReadObservedAsync(PhysicalKey(key),token);
            return observed==null?null:new KeyValueObservation(ReadLogicalValue(key,observed.Value),observed.Revision);
        }
        public Task CreateOnlyAsync(string key,string value,CancellationToken token=default)
            =>_native.CreateOnlyAsync(PhysicalKey(key),EncodeValue(key,value),token);
        public Task ReplaceUnchangedAsync(string key,string value,string revision,CancellationToken token=default)
            =>_native.ReplaceUnchangedAsync(PhysicalKey(key),EncodeValue(key,value),revision,token);
        public Task DeleteUnchangedAsync(string key,string revision,CancellationToken token=default)
            =>_native.DeleteUnchangedAsync(PhysicalKey(key),revision,token);
    }
}
