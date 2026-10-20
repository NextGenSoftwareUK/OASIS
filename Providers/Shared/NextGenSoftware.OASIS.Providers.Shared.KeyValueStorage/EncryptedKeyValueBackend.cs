using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Authenticated encrypted values. Keys remain visible; callers own key persistence.</summary>
public class EncryptedKeyValueBackend : IKeyValueBackend, IDisposable
{
    public static EncryptedKeyValueBackend Create(IKeyValueBackend inner,byte[] key,bool compress=false)
        =>inner is IConditionalKeyValueBackend conditional?new Conditional(conditional,key,compress):new EncryptedKeyValueBackend(inner,key,compress);
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
        return DecryptValue(key,value,cancellationToken);
    }

    private string DecryptValue(string key,string value,CancellationToken cancellationToken)
    {
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

    private sealed class Conditional:EncryptedKeyValueBackend,IConditionalKeyValueBackend
    {
        private readonly IConditionalKeyValueBackend _native;
        public Conditional(IConditionalKeyValueBackend inner,byte[] key,bool compress):base(inner,key,compress)=>_native=inner;
        public async Task<KeyValueObservation> ReadObservedAsync(string key,CancellationToken token=default)
        {
            ValidateKey(key);token.ThrowIfCancellationRequested();
            var observed=await _native.ReadObservedAsync(key,token);
            return observed==null?null:new KeyValueObservation(DecryptValue(key,observed.Value,token),observed.Revision);
        }
        public Task CreateOnlyAsync(string key,string value,CancellationToken token=default)
        {
            ValidateKey(key);return _native.CreateOnlyAsync(key,_codec.Encrypt(key,value,token),token);
        }
        public Task ReplaceUnchangedAsync(string key,string value,string revision,CancellationToken token=default)
        {
            ValidateKey(key);ArgumentException.ThrowIfNullOrWhiteSpace(revision);
            return _native.ReplaceUnchangedAsync(key,_codec.Encrypt(key,value,token),revision,token);
        }
        public Task DeleteUnchangedAsync(string key,string revision,CancellationToken token=default)
        {
            ValidateKey(key);token.ThrowIfCancellationRequested();ArgumentException.ThrowIfNullOrWhiteSpace(revision);
            return _native.DeleteUnchangedAsync(key,revision,token);
        }
    }
}
