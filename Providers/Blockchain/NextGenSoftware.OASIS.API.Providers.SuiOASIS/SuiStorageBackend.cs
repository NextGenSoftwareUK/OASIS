using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

// The provider supplies its single SDK transport; no second RPC or signing path.
internal sealed class SuiStorageBackend : IKeyValueBackend
{
    internal SuiSdkClient Client { get; }

    private Task<JsonElement> _invoke(string operation, object args, CancellationToken cancellationToken)
        => Client.InvokeAsync(operation, args, cancellationToken);

    internal SuiStorageBackend(SuiSdkClient client)
        => Client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _invoke("get", new { key }, cancellationToken);
        return StorageJsonResponse.NullableString(value);
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => await _invoke("put", new { key, value }, cancellationToken);

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => await _invoke("delete", new { key }, cancellationToken);

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        prefix ??= string.Empty;
        var response = await _invoke("list", new { prefix }, cancellationToken);
        return StorageJsonResponse.KeyPage(response,prefix);
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await _invoke("storageProbe", null, cancellationToken);
        // SDK probe verifies package ABI and owner without a paid mutation.
    }
}
