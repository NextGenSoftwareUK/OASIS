using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using System.Text.Json;
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
    public HederaStorageQueryPreparation QueryPreparation {get;init;}
}

/// <summary>Explicit native spending/admission policy for paid reads within a prepared storage scope.
/// These are ceilings, not actual charges or authorization to sponsor a user's costs.</summary>
public sealed class HederaStorageQueryPreparation
{
    public string[] AuthorizedNodeAccountIds {get;init;}
    public long MaxPaymentTinybars {get;init;}
    public long MaxTransactionFeeTinybars {get;init;}
    /// <summary>Per SDK invocation, not a replacement for host/Core whole-operation admission limits.</summary>
    public int MaximumRequestsPerInvocation {get;init;}=64;
    public int PersistenceTimeoutMs {get;init;}=10000;
    internal HederaStorageQueryPreparation Capture()
    {
        var nodes=AuthorizedNodeAccountIds?.ToArray();
        if(nodes==null||nodes.Length==0||nodes.Length>1024||nodes.Distinct(StringComparer.Ordinal).Count()!=nodes.Length
            ||nodes.Any(node=>!System.Text.RegularExpressions.Regex.IsMatch(node??"",@"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
            ||MaxPaymentTinybars<=0||MaxTransactionFeeTinybars<=0||MaximumRequestsPerInvocation<1||MaximumRequestsPerInvocation>512||PersistenceTimeoutMs<1||PersistenceTimeoutMs>30000)
            throw new ArgumentException("Prepared Hedera storage requires explicit bounded query nodes, payment/fee ceilings, count and persistence timeout.");
        return new(){AuthorizedNodeAccountIds=nodes,MaxPaymentTinybars=MaxPaymentTinybars,MaxTransactionFeeTinybars=MaxTransactionFeeTinybars,
            MaximumRequestsPerInvocation=MaximumRequestsPerInvocation,PersistenceTimeoutMs=PersistenceTimeoutMs};
    }
}

// Resolve storage secrets only when storage is used. Offline key operations do not
// require a deployed storage contract, but storage never succeeds without one.
internal sealed class ConfiguredHederaStorageBackend : IKeyValueBackend, IDisposable
{
    private readonly Lazy<PrivateKeyValueBackend> _backend;
    private readonly AsyncLocal<PrivateKeyValueBackend> _executionBackend=new();
    private readonly Func<Func<string,object,string,CancellationToken,Task<JsonElement>>,CancellationToken,PrivateKeyValueBackend> _createBackend;
    private readonly HederaStorageQueryPreparation _queryPreparation;
    private readonly string _sender;
    private bool _disposed;

    internal ConfiguredHederaStorageBackend(string network, IDictionary<string, string> nodes, HederaStorageConfiguration configuration)
    {
        var copiedNodes = nodes == null ? null : new Dictionary<string, string>(nodes);
        _sender=configuration?.OperatorAccountId;
        _queryPreparation=configuration?.QueryPreparation?.Capture();
        if(_queryPreparation?.AuthorizedNodeAccountIds.Contains(_sender)==true)
            throw new ArgumentException("The storage payer cannot also be its authorized paid-query node.");
        _createBackend = (invocation,scopeToken) => {
            if (configuration == null)
                throw new InvalidOperationException("Hedera storage requires an explicit contract, operator, signing key, encryption key and fee configuration.");
            var inner = new HederaStorageBackend(network, configuration.ContractId, configuration.OperatorAccountId,
                configuration.SigningKeyType, configuration.SigningKeyResolver, configuration.GasLimit,
                configuration.MaxFeeTinybars, copiedNodes,invocation,scopeToken);
            var key = configuration.EncryptionKeyResolver?.Invoke();
            try { return new PrivateKeyValueBackend(inner, key); }
            finally { if (key != null) CryptographicOperations.ZeroMemory(key); }
        };
        _backend=new Lazy<PrivateKeyValueBackend>(()=>_createBackend(null,default));
    }

    internal HederaStorageQueryPreparation QueryPreparation=>_queryPreparation?.Capture()
        ??throw new InvalidOperationException("Prepared Hedera storage needs an explicitly configured paid-query preparation policy before entering the operation.");

    internal async Task<T> WithInvocationAsync<T>(Func<Task<T>> operation,string expectedSender,
        Func<string,object,string,CancellationToken,Task<JsonElement>> invocation,CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        token.ThrowIfCancellationRequested();ObjectDisposedException.ThrowIf(_disposed,this);
        if(string.IsNullOrWhiteSpace(_sender)||expectedSender!=_sender)
            throw new InvalidOperationException("Admitted Hedera storage sender must match the configured operator.");
        if(_executionBackend.Value!=null)throw new InvalidOperationException("Nested Hedera storage execution owners are not permitted.");
        using var view=_createBackend(invocation,token);
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
