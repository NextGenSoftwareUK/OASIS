using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

internal sealed class TronProviderStorageBackend : IKeyValueBackend
{
    private readonly TronPrivateStorageBackend _storage;
    internal SemaphoreSlim Lifecycle;
    internal Func<bool> IsActive;
    internal TronProviderStorageBackend(TronPrivateStorageBackend storage) => _storage = storage;
    private TronPrivateStorageBackend Storage => _storage ?? throw new InvalidOperationException("TRON encrypted storage backend is not configured.");
    internal Task VerifyForActivationAsync() => Storage.VerifyAsync();
    private async Task<T> Run<T>(Func<TronPrivateStorageBackend, Task<T>> action, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await Lifecycle.WaitAsync(token);
        try
        {
            var storage = Storage;
            if (!IsActive()) throw new InvalidOperationException("TRON provider must be activated before storage access.");
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
