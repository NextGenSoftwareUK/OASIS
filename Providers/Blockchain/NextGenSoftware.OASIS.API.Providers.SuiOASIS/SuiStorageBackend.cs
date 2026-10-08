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
        return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        => await _invoke("put", new { key, value }, cancellationToken);

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => await _invoke("delete", new { key }, cancellationToken);

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        prefix ??= string.Empty;
        var response = await _invoke("list", new { prefix }, cancellationToken);
        var keys = new List<string>();
        string previous = null;
        foreach (var item in response.EnumerateArray())
        {
            var key = item.GetString();
            if (key == null || !key.StartsWith(prefix, StringComparison.Ordinal)
                || (previous != null && string.CompareOrdinal(previous, key) >= 0))
                throw new InvalidDataException("Sui storage returned duplicate, unordered or out-of-prefix keys.");
            keys.Add(key);
            previous = key;
        }
        return keys;
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await _invoke("storageProbe", null, cancellationToken);
        var key = "oasis/probe/" + Guid.NewGuid().ToString("N");
        await PutAsync(key, key, cancellationToken);
        try
        {
            if (await GetAsync(key, cancellationToken) != key)
                throw new InvalidDataException("Sui committed storage readback did not match its value.");
        }
        finally { await DeleteAsync(key, cancellationToken); }
    }
}
