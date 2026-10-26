using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

// Wallet activation does not require storage secrets. Every storage operation
// still requires an active network and resolves exactly one configured encrypted store.
internal sealed class PolkadotConfiguredRecordBackend : IConditionalKeyValueBackend, IDisposable
{
    private readonly PolkadotStorageConfiguration _configuration;
    private readonly Lazy<(PolkadotRecordBackend Raw, PersistedPrivateKeyValueBackend Private)> _state;
    private Func<PolkadotStorageConfiguration, PolkadotRecordBackend> _factory;
    private Action _guard;
    private bool _disposed;
    internal PolkadotConfiguredRecordBackend(PolkadotStorageConfiguration configuration)
    {
        _configuration = configuration;
        _state = new Lazy<(PolkadotRecordBackend, PersistedPrivateKeyValueBackend)>(() => {
            if (_configuration == null) throw new InvalidOperationException("Polkadot storage requires an explicit deployed contract/code/owner and separate encryption-key configuration.");
            var raw = _factory(_configuration);
            return (raw, PersistedPrivateKeyValueBackend.Create(raw, _configuration.EncryptionKeyResolver));
        });
    }
    internal void Bind(Func<PolkadotStorageConfiguration, PolkadotRecordBackend> factory, Action guard)
    {
        if (_factory != null) throw new InvalidOperationException("Storage factory is already bound.");
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _guard = guard ?? throw new ArgumentNullException(nameof(guard));
    }
    private (PolkadotRecordBackend Raw, PersistedPrivateKeyValueBackend Private) Resolve(CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); ObjectDisposedException.ThrowIf(_disposed, this); _guard();
        return _state.Value;
    }
    private IConditionalKeyValueBackend Conditional(CancellationToken token) => (IConditionalKeyValueBackend)Resolve(token).Private;
    internal Task<T> WithPreparationAsync<T>(Func<Task<T>> action, Func<string,string,PreparedSubmissionServer> factory, CancellationToken token)
        => Resolve(token).Raw.WithPreparationAsync(action, factory, token);
    public Task<string> GetAsync(string key,CancellationToken token=default)=>Resolve(token).Private.GetAsync(key,token);
    public Task PutAsync(string key,string value,CancellationToken token=default)=>Resolve(token).Private.PutAsync(key,value,token);
    public Task DeleteAsync(string key,CancellationToken token=default)=>Resolve(token).Private.DeleteAsync(key,token);
    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken token=default)=>Resolve(token).Private.ListKeysAsync(prefix,token);
    public Task VerifyAsync(CancellationToken token=default)=>Resolve(token).Private.VerifyAsync(token);
    public Task<KeyValueObservation> ReadObservedAsync(string key,CancellationToken token=default)=>Conditional(token).ReadObservedAsync(key,token);
    public Task CreateOnlyAsync(string key,string value,CancellationToken token=default)=>Conditional(token).CreateOnlyAsync(key,value,token);
    public Task ReplaceUnchangedAsync(string key,string value,string revision,CancellationToken token=default)=>Conditional(token).ReplaceUnchangedAsync(key,value,revision,token);
    public Task DeleteUnchangedAsync(string key,string revision,CancellationToken token=default)=>Conditional(token).DeleteUnchangedAsync(key,revision,token);
    public void Dispose() { if (_disposed) return; _disposed=true; if (_state.IsValueCreated) _state.Value.Private.Dispose(); }
}
