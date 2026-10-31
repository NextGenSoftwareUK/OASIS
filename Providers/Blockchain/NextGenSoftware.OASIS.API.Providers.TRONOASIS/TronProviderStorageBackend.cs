using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

internal sealed class TronProviderStorageBackend : IKeyValueBackend
{
    private TronPrivateStorageBackend _storage;
    private readonly AsyncLocal<TronPrivateStorageBackend> _executionStorage=new();
    private readonly AsyncLocal<CancellationToken> _executionToken=new();
    internal Func<TronPrivateStorageBackend> Factory;
    internal SemaphoreSlim Lifecycle;
    internal Func<bool> IsActive;
    internal TronProviderStorageBackend(TronPrivateStorageBackend storage) => _storage = storage;
    private TronPrivateStorageBackend Storage => _executionStorage.Value ?? (_storage ??= Factory?.Invoke()
        ?? throw new InvalidOperationException("TRON encrypted storage backend is not configured."));
    internal async Task<T> WithPreparedStorageAsync<T>(Func<Task<T>> operation,
        Func<TronNativeTransferClient,TronNativeTransferClient> prepare,CancellationToken token)
    {
        if(_executionStorage.Value!=null)throw new InvalidOperationException("Nested TRON storage execution owners are not permitted.");
        TronPrivateStorageBackend view;
        await Lifecycle.WaitAsync(token);
        try {
            if(!IsActive())throw new InvalidOperationException("Activate TRON before admitted storage execution.");
            var original=Storage;
            view=original.CreatePreparedView(prepare(original.Client));
        } finally {Lifecycle.Release();}
        using(view) {
            _executionStorage.Value=view;
            _executionToken.Value=token;
            try {token.ThrowIfCancellationRequested();return await operation();}
            finally {_executionStorage.Value=null;_executionToken.Value=default;}
        }
    }
    internal void ResetOwnedStorage()
    {
        if (Factory == null) return; // Explicitly injected storage remains caller-owned.
        _storage?.Dispose();
        _storage = null;
    }
    internal Task VerifyForActivationAsync() => Storage.VerifyAsync();
    private async Task<T> Run<T>(Func<TronPrivateStorageBackend,CancellationToken,Task<T>> action, CancellationToken token)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(token,_executionToken.Value);
        token=linked.Token;
        token.ThrowIfCancellationRequested();
        await Lifecycle.WaitAsync(token);
        try
        {
            if (_storage == null && Factory == null) throw new InvalidOperationException("TRON encrypted storage backend is not configured.");
            if (!IsActive()) throw new InvalidOperationException("TRON provider must be activated before storage access.");
            var storage = Storage;
            return await action(storage,token);
        }
        finally { Lifecycle.Release(); }
    }
    public Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        => Run((storage,token) => storage.GetAsync(key,token), cancellationToken);
    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => Run(async (storage,token) => { await storage.PutAsync(key,value,token); return true; }, cancellationToken);
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => Run(async (storage,token) => { await storage.DeleteAsync(key,token); return true; }, cancellationToken);
    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        => Run((storage,token) => storage.ListKeysAsync(prefix,token), cancellationToken);
    public Task VerifyAsync(CancellationToken cancellationToken = default)
        => Run(async (storage,token) => { await storage.VerifyAsync(token); return true; }, cancellationToken);
}
