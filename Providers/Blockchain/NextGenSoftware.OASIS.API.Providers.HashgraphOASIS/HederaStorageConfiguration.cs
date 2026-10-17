using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public sealed class HederaStorageConfiguration
{
    public string ContractId { get; init; }
    public string OperatorAccountId { get; init; }
    public string SigningKeyType { get; init; }
    public Func<string> SigningKeyResolver { get; init; }
    // Must resolve the same persistent 32-byte key for this store across restarts.
    // Return an owned buffer; it is cleared after the encryption layer copies it.
    public Func<byte[]> EncryptionKeyResolver { get; init; }
    public long GasLimit { get; init; }
    public long MaxFeeTinybars { get; init; }
}

// Resolve storage secrets only when storage is used. Offline key operations do not
// require a deployed storage contract, but storage never succeeds without one.
internal sealed class ConfiguredHederaStorageBackend : IKeyValueBackend, IDisposable
{
    private readonly Lazy<PrivateKeyValueBackend> _backend;
    private readonly AsyncLocal<PrivateKeyValueBackend> _executionBackend=new();
    private readonly Func<Func<string,string,string,string,PreparedSubmissionServer>,PrivateKeyValueBackend> _createBackend;
    private readonly string _sender;
    private bool _disposed;

    internal ConfiguredHederaStorageBackend(string network, IDictionary<string, string> nodes, HederaStorageConfiguration configuration)
    {
        var copiedNodes = nodes == null ? null : new Dictionary<string, string>(nodes);
        _sender=configuration?.OperatorAccountId;
        _createBackend = preparation => {
            if (configuration == null)
                throw new InvalidOperationException("Hedera storage requires an explicit contract, operator, signing key, encryption key and fee configuration.");
            var inner = new HederaStorageBackend(network, configuration.ContractId, configuration.OperatorAccountId,
                configuration.SigningKeyType, configuration.SigningKeyResolver, configuration.GasLimit,
                configuration.MaxFeeTinybars, copiedNodes,preparation);
            var key = configuration.EncryptionKeyResolver?.Invoke();
            try { return new PrivateKeyValueBackend(inner, key); }
            finally { if (key != null) CryptographicOperations.ZeroMemory(key); }
        };
        _backend=new Lazy<PrivateKeyValueBackend>(()=>_createBackend(null));
    }

    internal async Task<T> WithPreparationAsync<T>(Func<Task<T>> operation,string expectedSender,
        Func<string,string,string,string,PreparedSubmissionServer> preparation,CancellationToken token)
    {
        token.ThrowIfCancellationRequested();ObjectDisposedException.ThrowIf(_disposed,this);
        if(string.IsNullOrWhiteSpace(_sender)||expectedSender!=_sender)
            throw new InvalidOperationException("Admitted Hedera storage sender must match the configured operator.");
        if(_executionBackend.Value!=null)throw new InvalidOperationException("Nested Hedera storage execution owners are not permitted.");
        using var view=_createBackend(preparation);
        _executionBackend.Value=view;
        try {return await operation();}
        finally {_executionBackend.Value=null;}
    }

    private PrivateKeyValueBackend Resolve(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _executionBackend.Value ?? _backend.Value;
    }

    public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Resolve(cancellationToken).GetAsync(key, cancellationToken);
    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Resolve(cancellationToken).PutAsync(key, value, cancellationToken);
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Resolve(cancellationToken).DeleteAsync(key, cancellationToken);
    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) => Resolve(cancellationToken).ListKeysAsync(prefix, cancellationToken);
    public Task VerifyAsync(CancellationToken cancellationToken = default) => Resolve(cancellationToken).VerifyReadOnlyAsync(cancellationToken);
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_backend.IsValueCreated) _backend.Value.Dispose();
    }
}
