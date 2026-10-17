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
            try {token.ThrowIfCancellationRequested();return await operation();}
            finally {_executionStorage.Value=null;}
        }
    }
    internal void ResetOwnedStorage()
    {
        if (Factory == null) return; // Explicitly injected storage remains caller-owned.
        _storage?.Dispose();
        _storage = null;
    }
    internal Task VerifyForActivationAsync() => Storage.VerifyAsync();
    private async Task<T> Run<T>(Func<TronPrivateStorageBackend, Task<T>> action, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await Lifecycle.WaitAsync(token);
        try
        {
            if (_storage == null && Factory == null) throw new InvalidOperationException("TRON encrypted storage backend is not configured.");
            if (!IsActive()) throw new InvalidOperationException("TRON provider must be activated before storage access.");
            var storage = Storage;
            return await action(storage);
        }
        finally { Lifecycle.Release(); }
    }
    public Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        => Run(storage => storage.GetAsync(key, cancellationToken), cancellationToken);
    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => Run(async storage => { await storage.PutAsync(key, value, cancellationToken); return true; }, cancellationToken);
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => Run(async storage => { await storage.DeleteAsync(key, cancellationToken); return true; }, cancellationToken);
    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        => Run(storage => storage.ListKeysAsync(prefix, cancellationToken), cancellationToken);
    public Task VerifyAsync(CancellationToken cancellationToken = default)
        => Run(async storage => { await storage.VerifyAsync(cancellationToken); return true; }, cancellationToken);
}
