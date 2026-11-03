using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS;

/// <summary>One lifecycle gate around the shared encrypted storage owner.</summary>
internal sealed class TonProviderBackend : IKeyValueBackend
{
    internal readonly SemaphoreSlim Gate = new(1, 1);
    internal readonly PersistedPrivateKeyValueBackend Private;
    internal Func<bool> IsActive = () => false;
    internal TonProviderBackend(PersistedPrivateKeyValueBackend storage) => Private = storage;

    private async Task<T> Run<T>(Func<Task<T>> operation, CancellationToken token)
    {
        await Gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (!IsActive()) throw new TonStorageException("TON_PROVIDER_INACTIVE", "Activate native TON before storage access.");
            return await operation().ConfigureAwait(false);
        }
        finally { Gate.Release(); }
    }
    public Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        => Run(() => Private.GetAsync(key, cancellationToken), cancellationToken);
    public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        => Run(() => Private.ListKeysAsync(prefix, cancellationToken), cancellationToken);
    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => _ = await Run(async () => { await Private.PutAsync(key, value, cancellationToken).ConfigureAwait(false); return true; }, cancellationToken).ConfigureAwait(false);
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => _ = await Run(async () => { await Private.DeleteAsync(key, cancellationToken).ConfigureAwait(false); return true; }, cancellationToken).ConfigureAwait(false);
    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { await Private.VerifyAsync(cancellationToken).ConfigureAwait(false); }
        finally { Gate.Release(); }
    }
}
