using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

// All ledger operations use the provider's existing official SDK transport.
// Compose with EncryptedKeyValueBackend before persisting private OASIS records.
internal sealed class HederaStorageBackend : IKeyValueBackend
{
    private readonly string _network, _contractId, _sender, _keyType, _maxFeeTinybars;
    private readonly Func<string> _privateKey;
    private readonly long _gas;
    private readonly Dictionary<string, string> _nodes;
    private readonly Func<string,string,string,string,PreparedSubmissionServer> _preparation;
    private readonly CancellationToken _scopeToken;

    internal HederaStorageBackend(string network, string contractId, string sender, string keyType,
        Func<string> privateKey, long gas, long maxFeeTinybars, IDictionary<string, string> nodes = null)
        :this(network,contractId,sender,keyType,privateKey,gas,maxFeeTinybars,nodes,null) { }

    internal HederaStorageBackend(string network,string contractId,string sender,string keyType,
        Func<string> privateKey,long gas,long maxFeeTinybars,IDictionary<string,string> nodes,
        Func<string,string,string,string,PreparedSubmissionServer> preparation,CancellationToken scopeToken=default)
    {
        _preparation=preparation;_scopeToken=scopeToken;
        if (network != "mainnet" && network != "testnet" && network != "previewnet" && network != "local")
            throw new ArgumentException("An explicit Hedera network is required.");
        if (keyType != "der" && keyType != "ed25519" && keyType != "ecdsa")
            throw new ArgumentException("An explicit signing key algorithm is required.");
        if (gas <= 0 || gas > 9007199254740991L || maxFeeTinybars <= 0)
            throw new ArgumentException("Positive exact gas and tinybar fee limits are required.");
        // The same numeric account identity is used for transaction generation and SDK signing.
        if (!System.Text.RegularExpressions.Regex.IsMatch(sender ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z")
            || !System.Text.RegularExpressions.Regex.IsMatch(contractId ?? "", @"\A[0-9]+\.[0-9]+\.[0-9]+\z"))
            throw new ArgumentException("Numeric Hedera account and storage contract IDs are required.");
        if ((network == "local") != (nodes != null && nodes.Count > 0))
            throw new ArgumentException("Only local Hedera requires custom consensus nodes.");
        _network = network;
        _contractId = contractId;
        _sender = sender;
        _keyType = keyType;
        _privateKey = privateKey ?? throw new ArgumentNullException(nameof(privateKey));
        _gas = gas;
        _maxFeeTinybars = maxFeeTinybars.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _nodes = nodes == null ? null : new Dictionary<string, string>(nodes);
    }

    private async Task<JsonElement> InvokeAsync(string action, string key, string value, string prefix, bool mutation,
        CancellationToken cancellationToken)
    {
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,_scopeToken);
        cancellationToken=linked.Token;
        cancellationToken.ThrowIfCancellationRequested();
        var secret = _privateKey();
        if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Hedera storage signing key is unavailable.");
        var transactionId=mutation ? HederaSdkClient.CreateTransactionId(_sender):null;
        var request=new {
            network = _network, nodes = _nodes, contractId = _contractId, sender = _sender,
            keyType = _keyType, privateKey = secret, gas = _gas, maxFeeTinybars = _maxFeeTinybars,
            transactionId, key, value, prefix
        };
        if(mutation && _preparation!=null) {
            // Derive trusted signer identity from the configured key, never from child-supplied evidence.
            var restored=await HederaSdkClient.InvokeAsync("restoreKey",new {keyType=_keyType,privateKey=secret},cancellationToken);
            var publicKey=restored.GetProperty("publicKey").GetString();
            ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);
            using var server=_preparation(_network,_sender,publicKey,transactionId);
            return await HederaSdkClient.InvokePreparedAsync(action,request,server,cancellationToken);
        }
        return await HederaSdkClient.InvokeAsync(action,request,cancellationToken);
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var result = await InvokeAsync("storageGet", key, null, null, false, cancellationToken);
        return StorageJsonResponse.NullableString(result);
    }

    public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ValidateReceipt(await InvokeAsync("storagePut", key, value, null, true, cancellationToken));
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ValidateReceipt(await InvokeAsync("storageDelete", key, null, null, true, cancellationToken));
    }

    private static void ValidateReceipt(JsonElement receipt)
    {
        if (receipt.GetProperty("status").GetString() != "SUCCESS"
            || string.IsNullOrWhiteSpace(receipt.GetProperty("transactionId").GetString()))
            throw new InvalidDataException("Hedera storage mutation did not establish consensus success.");
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        var result = await InvokeAsync("storageList", null, null, prefix, false, cancellationToken);
        return StorageJsonResponse.KeyPage(result,prefix);
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        await InvokeAsync("storageProbe",null,null,null,false,cancellationToken);
        // Consensus queries may have fees; this verifies authority without put/delete transactions.
    }
}
