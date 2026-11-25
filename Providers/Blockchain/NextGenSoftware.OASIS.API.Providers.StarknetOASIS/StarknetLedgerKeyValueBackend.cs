using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

/// <summary>
/// Key-value storage on Starknet. Each value is an (already encrypted) envelope held by a Kubo node; the OasisKv contract
/// stores, per owning account, a commitment {k: key, c: CID, h: SHA-256} under felt(sha256(key)[0..31]). Writes and
/// removals are account transactions acknowledged only once their receipt reports SUCCEEDED.
/// </summary>
internal sealed class StarknetLedgerKeyValueBackend : IKeyValueBackend, IDisposable
{
    private const int MaximumEnvelopeBytes = 4 * 1024 * 1024;
    private readonly IStarknetSdkBridge _sdk;
    private readonly string _endpoint;
    private readonly string _chainId;
    private readonly string _contract;
    private readonly string _accountClassHash;
    private readonly string _privateKey;
    private readonly HttpClient _kubo;
    private readonly SemaphoreSlim _submission = new(1, 1);
    private string _owner;

    public StarknetLedgerKeyValueBackend(IStarknetSdkBridge sdk, string endpoint, string chainId, string contract, string accountClassHash,
        string privateKey, string kuboApi)
    {
        _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _chainId = chainId ?? throw new ArgumentNullException(nameof(chainId));
        _contract = contract ?? throw new ArgumentNullException(nameof(contract));
        _accountClassHash = accountClassHash ?? throw new ArgumentNullException(nameof(accountClassHash));
        _privateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
        _kubo = new HttpClient { BaseAddress = new Uri((kuboApi ?? throw new ArgumentNullException(nameof(kuboApi))).TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        var owner = await OwnerAsync(cancellationToken);
        var status = await _sdk.InvokeAsync(new { operation = "accountStatus", endpoint = _endpoint, chainId = _chainId, address = owner }, cancellationToken);
        if (!status.GetProperty("deployed").GetBoolean()) throw new InvalidOperationException($"The Starknet storage account {owner} is not deployed.");
        await _sdk.InvokeAsync(new { operation = "kvKeys", endpoint = _endpoint, chainId = _chainId, contract = _contract, owner }, cancellationToken);
        using var kubo = await _kubo.PostAsync("api/v0/version", null, cancellationToken);
        kubo.EnsureSuccessStatusCode();
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var commitment = await ReadCommitmentAsync(KeyFelt(key), cancellationToken);
        if (commitment == null) return null;
        if (!string.Equals(commitment.Value.Key, key, StringComparison.Ordinal))
            throw new InvalidDataException("The Starknet commitment belongs to a different key (key hash collision).");
        return await ReadEnvelopeAsync(commitment.Value.Cid, commitment.Value.Sha256, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var owner = await OwnerAsync(cancellationToken);
        var listed = await _sdk.InvokeAsync(new { operation = "kvKeys", endpoint = _endpoint, chainId = _chainId, contract = _contract, owner }, cancellationToken);
        var keys = new List<string>();
        foreach (var felt in listed.GetProperty("keys").EnumerateArray())
        {
            var commitment = await ReadCommitmentAsync(felt.GetString(), cancellationToken);
            if (commitment != null && commitment.Value.Key.StartsWith(prefix ?? string.Empty, StringComparison.Ordinal)) keys.Add(commitment.Value.Key);
        }
        return keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > MaximumEnvelopeBytes) throw new InvalidDataException("The Starknet record envelope exceeds the blob size limit.");
        using var form = new MultipartFormDataContent { { new ByteArrayContent(bytes), "file", "record" } };
        using var added = await _kubo.PostAsync("api/v0/add?cid-version=0&pin=true", form, cancellationToken);
        added.EnsureSuccessStatusCode();
        using var receipt = JsonDocument.Parse(await added.Content.ReadAsStringAsync(cancellationToken));
        var cid = receipt.RootElement.GetProperty("Hash").GetString() ?? throw new InvalidDataException("Kubo returned no CID.");
        var commitment = JsonSerializer.Serialize(new { k = key, c = cid, h = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() });
        await ExecuteAsync(new { operation = "kvPut", endpoint = _endpoint, chainId = _chainId, contract = _contract, classHash = _accountClassHash,
            privateKey = _privateKey, key = KeyFelt(key), value = commitment }, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var felt = KeyFelt(key);
        var commitment = await ReadCommitmentAsync(felt, cancellationToken);
        if (commitment == null) return; // deleting a missing key succeeds
        await ExecuteAsync(new { operation = "kvRemove", endpoint = _endpoint, chainId = _chainId, contract = _contract, classHash = _accountClassHash,
            privateKey = _privateKey, key = felt }, cancellationToken);
        using var unpinned = await _kubo.PostAsync($"api/v0/pin/rm?arg={Uri.EscapeDataString(commitment.Value.Cid)}&recursive=true", null, cancellationToken);
        if (!unpinned.IsSuccessStatusCode && !(await unpinned.Content.ReadAsStringAsync(cancellationToken)).Contains("not pinned", StringComparison.OrdinalIgnoreCase))
            unpinned.EnsureSuccessStatusCode();
    }

    /// <summary>felt252 holds 251 bits, so the key id is the first 31 bytes of SHA-256(key).</summary>
    internal static string KeyFelt(string key) => "0x" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)), 0, 31).ToLowerInvariant();

    private async Task<(string Key, string Cid, string Sha256)?> ReadCommitmentAsync(string felt, CancellationToken cancellationToken)
    {
        var owner = await OwnerAsync(cancellationToken);
        var read = await _sdk.InvokeAsync(new { operation = "kvGet", endpoint = _endpoint, chainId = _chainId, contract = _contract, owner, key = felt }, cancellationToken);
        var text = read.GetProperty("value").GetString();
        if (string.IsNullOrEmpty(text)) return null;
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        return (root.GetProperty("k").GetString()!, root.GetProperty("c").GetString()!, root.GetProperty("h").GetString()!);
    }

    private async Task<string> ReadEnvelopeAsync(string cid, string sha256, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v0/cat?arg={Uri.EscapeDataString(cid)}&length={MaximumEnvelopeBytes + 1}");
        using var response = await _kubo.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length > MaximumEnvelopeBytes || !string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), sha256, StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException("The Starknet record envelope does not match its on-chain commitment.");
        return Encoding.UTF8.GetString(bytes);
    }

    private async Task ExecuteAsync(object request, CancellationToken cancellationToken)
    {
        await _submission.WaitAsync(cancellationToken); // one account nonce stream
        try { await _sdk.InvokeAsync(request, cancellationToken); }
        finally { _submission.Release(); }
    }

    private async Task<string> OwnerAsync(CancellationToken cancellationToken)
    {
        if (_owner != null) return _owner;
        var derived = await _sdk.InvokeAsync(new { operation = "deriveFromPrivateKey", privateKey = _privateKey, classHash = _accountClassHash }, cancellationToken);
        return _owner = derived.GetProperty("address").GetString() ?? throw new InvalidDataException("The Starknet bridge returned no account address.");
    }

    public void Dispose() { _kubo.Dispose(); _submission.Dispose(); }
}
