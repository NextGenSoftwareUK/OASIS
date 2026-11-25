using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using RadixEngineToolkit;
using RetDecimal = RadixEngineToolkit.Decimal;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

/// <summary>Raised when a notarised transaction does not reach CommittedSuccess, or its status cannot be established.</summary>
public sealed class RadixTransactionException : Exception
{
    public RadixTransactionException(string intentHash, string status, string message)
        : base($"Radix transaction {intentHash} ended {status}: {message}")
    {
        IntentHash = intentHash;
        Status = status;
    }

    public string IntentHash { get; }
    public string Status { get; }
}

/// <summary>
/// One signing key and one node: builds, notarises and submits V1 transactions through the node's Core API (LTS
/// endpoints) and reads committed state through its Engine State API. A write is acknowledged only once the node
/// reports the intent as <c>CommittedSuccess</c>; every other terminal status is an exception.
/// </summary>
public sealed class RadixLedgerClient : IDisposable
{
    public const string CommittedSuccess = "CommittedSuccess";

    private readonly HttpClient http;
    private readonly bool ownsHttp;
    private readonly Uri coreApi;
    private readonly Uri engineStateApi;
    private readonly PrivateKey signer;
    private readonly TimeSpan commitTimeout;
    private readonly SemaphoreSlim submitGate = new(1, 1);

    /// <param name="coreApiUri">Node Core API root, for example <c>http://127.0.0.1:3333/core/</c>.</param>
    /// <param name="engineStateApiUri">Node Engine State API root, for example <c>http://127.0.0.1:3336/engine-state/</c>.</param>
    /// <param name="networkId">Radix network id (1 mainnet, 2 stokenet, 240 localnet).</param>
    /// <param name="networkName">Logical network name the node expects in requests (mainnet, stokenet, localnet).</param>
    /// <param name="privateKeyHex">32-byte secp256k1 private key of the signing account, hex encoded.</param>
    public RadixLedgerClient(Uri coreApiUri, Uri engineStateApiUri, byte networkId, string networkName, string privateKeyHex,
        HttpClient httpClient = null, TimeSpan? commitTimeout = null)
    {
        coreApi = Root(coreApiUri ?? throw new ArgumentNullException(nameof(coreApiUri)));
        engineStateApi = Root(engineStateApiUri ?? throw new ArgumentNullException(nameof(engineStateApiUri)));
        if (string.IsNullOrWhiteSpace(networkName)) throw new ArgumentException("Network name is required.", nameof(networkName));
        NetworkId = networkId;
        NetworkName = networkName;
        var keyBytes = Convert.FromHexString(privateKeyHex ?? throw new ArgumentNullException(nameof(privateKeyHex)));
        if (keyBytes.Length != 32) throw new ArgumentException("A 32-byte secp256k1 private key is required.", nameof(privateKeyHex));
        signer = PrivateKey.NewSecp256k1(keyBytes);
        AccountAddress = Address.PreallocatedAccountAddressFromPublicKey(signer.PublicKey(), networkId).AddressString();
        SignatureBadge = NonFungibleGlobalId.SignatureBadge(signer.PublicKey(), networkId).AsStr();
        this.commitTimeout = commitTimeout ?? TimeSpan.FromMinutes(2);
        ownsHttp = httpClient == null;
        http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public byte NetworkId { get; }
    public string NetworkName { get; }

    /// <summary>The preallocated (virtual) account of the signing key; it pays fees.</summary>
    public string AccountAddress { get; }

    /// <summary>The signing key's signature badge, as a manifest NonFungibleGlobalId string.</summary>
    public string SignatureBadge { get; }

    private static Uri Root(Uri uri) => uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");

    /// <summary>Manifest text for a string literal: quotes, backslashes and control characters are escaped (the manifest
    /// lexer's JSON-style escapes); every other character is written as itself.</summary>
    public static string ManifestString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var text = new StringBuilder(value.Length + 2).Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': text.Append("\\\""); break;
                case '\\': text.Append("\\\\"); break;
                case '\n': text.Append("\\n"); break;
                case '\r': text.Append("\\r"); break;
                case '\t': text.Append("\\t"); break;
                default:
                    if (char.IsControl(c)) text.Append("\\u").Append(((int)c).ToString("x4"));
                    else text.Append(c);
                    break;
            }
        }
        return text.Append('"').ToString();
    }

    /// <summary>Compiles, notarises and submits a manifest, then waits for CommittedSuccess. Returns the committed receipt.</summary>
    public Task<JsonNode> SubmitManifestAsync(string manifest, CancellationToken cancellationToken, params byte[][] blobs)
        => SubmitAsync(new TransactionManifestV1(InstructionsV1.FromString(manifest, NetworkId), blobs ?? Array.Empty<byte[]>()), cancellationToken);

    /// <summary>Submits a typed manifest, then waits for CommittedSuccess. Returns the committed receipt.</summary>
    public async Task<JsonNode> SubmitAsync(TransactionManifestV1 manifest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        // One in-flight intent per key keeps fee locks and status polling unambiguous.
        await submitGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var construction = await CoreAsync("lts/transaction/construction", new JsonObject(), cancellationToken).ConfigureAwait(false);
            var epoch = construction["current_epoch"]!.GetValue<ulong>();
            var header = new TransactionHeaderV1(NetworkId, epoch, epoch + 10, (uint)Random.Shared.NextInt64(0, uint.MaxValue),
                signer.PublicKey(), true, 0);
            using var notarized = new TransactionV1Builder().Header(header).Manifest(manifest)
                .Message(new MessageV1.None()).NotarizeWithPrivateKey(signer);
            var intentHash = notarized.IntentHash().AsStr();

            var submit = await CoreAsync("lts/transaction/submit", new JsonObject
            {
                ["notarized_transaction_hex"] = Convert.ToHexString(notarized.ToPayloadBytes()).ToLowerInvariant()
            }, cancellationToken, allowError: true).ConfigureAwait(false);
            if (submit["code"] != null)
                throw new RadixTransactionException(intentHash, "SubmitRejected", submit.ToJsonString());

            await WaitForCommitAsync(intentHash, cancellationToken).ConfigureAwait(false);
            return await CoreAsync("transaction/receipt", new JsonObject { ["intent_hash"] = intentHash }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            submitGate.Release();
        }
    }

    private async Task WaitForCommitAsync(string intentHash, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + commitTimeout;
        while (true)
        {
            var status = await CoreAsync("lts/transaction/status", new JsonObject { ["intent_hash"] = intentHash }, cancellationToken)
                .ConfigureAwait(false);
            var intentStatus = status["intent_status"]?.GetValue<string>();
            switch (intentStatus)
            {
                case CommittedSuccess:
                    return;
                case "CommittedFailure":
                case "PermanentRejection":
                    throw new RadixTransactionException(intentHash, intentStatus, status.ToJsonString());
            }
            if (DateTime.UtcNow > deadline)
                throw new RadixTransactionException(intentHash, intentStatus ?? "Unknown",
                    $"not committed within {commitTimeout}; the outcome is unknown and must be reconciled: {status.ToJsonString()}");
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Gives the signing account XRD from the network faucet. Only test networks have a faucet.</summary>
    public Task FundFromFaucetAsync(CancellationToken cancellationToken = default)
        => SubmitAsync(new ManifestV1Builder().FaucetLockFee().FaucetFreeXrd().AccountDepositEntireWorktop(new Address(AccountAddress))
            .Build(NetworkId), cancellationToken);

    /// <summary>Publishes a compiled Scrypto package (code + package definition) and returns its address.</summary>
    public async Task<string> PublishPackageAsync(byte[] wasm, byte[] packageDefinition, CancellationToken cancellationToken = default)
    {
        var manifest = new ManifestV1Builder()
            .AccountLockFee(new Address(AccountAddress), new RetDecimal("5000"))
            .PackagePublish(wasm, packageDefinition, new Dictionary<string, MetadataInitEntry>())
            .AccountDepositEntireWorktop(new Address(AccountAddress))
            .Build(NetworkId);
        var receipt = await SubmitAsync(manifest, cancellationToken).ConfigureAwait(false);
        return SingleNewEntity(receipt, "package_");
    }

    /// <summary>Instantiates <c>OasisKeyValueStore</c> owned by this key's signature badge and returns the component address.</summary>
    public async Task<string> InstantiateKeyValueStoreAsync(string packageAddress, CancellationToken cancellationToken = default)
    {
        var manifest =
            $"CALL_METHOD Address(\"{AccountAddress}\") \"lock_fee\" Decimal(\"50\");\n" +
            $"CALL_FUNCTION Address(\"{packageAddress}\") \"OasisKeyValueStore\" \"instantiate\" NonFungibleGlobalId(\"{SignatureBadge}\");\n";
        var receipt = await SubmitManifestAsync(manifest, cancellationToken).ConfigureAwait(false);
        return SingleNewEntity(receipt, "component_");
    }

    private string SingleNewEntity(JsonNode receipt, string prefix)
    {
        var found = new List<string>();
        foreach (var entity in receipt["committed"]?["receipt"]?["state_updates"]?["new_global_entities"]?.AsArray() ?? new JsonArray())
        {
            var address = entity?["entity_address"]?.GetValue<string>();
            if (address != null && address.StartsWith(prefix, StringComparison.Ordinal)) found.Add(address);
        }
        if (found.Count != 1)
            throw new InvalidOperationException($"Expected one new {prefix} entity in the committed receipt, found {found.Count}.");
        return found[0];
    }

    public Task<JsonNode> CoreAsync(string path, JsonObject body, CancellationToken cancellationToken, bool allowError = false)
    {
        body["network"] = NetworkName;
        return PostAsync(new Uri(coreApi, path), body, cancellationToken, allowError);
    }

    public Task<JsonNode> EngineStateAsync(string path, JsonObject body, CancellationToken cancellationToken, bool allowError = false)
        => PostAsync(new Uri(engineStateApi, path), body, cancellationToken, allowError);

    private async Task<JsonNode> PostAsync(Uri uri, JsonObject body, CancellationToken cancellationToken, bool allowError)
    {
        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await http.PostAsync(uri, content, cancellationToken).ConfigureAwait(false);
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode && !allowError)
            throw new HttpRequestException($"Radix node {uri.AbsolutePath} returned {(int)response.StatusCode}: {text}", null, response.StatusCode);
        if (!response.IsSuccessStatusCode && !text.TrimStart().StartsWith('{'))
            return new JsonObject { ["code"] = (int)response.StatusCode, ["message"] = text };
        return JsonNode.Parse(text) ?? throw new HttpRequestException($"Radix node {uri.AbsolutePath} returned an empty body.");
    }

    public void Dispose()
    {
        signer.Dispose();
        submitGate.Dispose();
        if (ownsHttp) http.Dispose();
    }
}
