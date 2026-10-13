using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

public partial class CosmosBlockChainOASIS
{
    private async Task<string> SigningKeyAsync(Guid avatarId, string suppliedKey = null, string suppliedSeed = null, string suppliedPublicKey = null)
    {
        if (!string.IsNullOrWhiteSpace(suppliedKey) && !string.IsNullOrWhiteSpace(suppliedSeed) && suppliedKey != suppliedSeed)
            throw new ArgumentException("Specify one sender key or seed phrase, not conflicting credentials.");
        string key;
        if (!string.IsNullOrWhiteSpace(suppliedKey)) key = suppliedKey;
        else if (!string.IsNullOrWhiteSpace(suppliedSeed)) key = suppliedSeed;
        else if (avatarId == Guid.Empty)
        {
            if (string.IsNullOrWhiteSpace(_privateKey)) throw new InvalidOperationException("Cosmos signer is not configured.");
            key = _privateKey;
        }
        else
        {
            var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(avatarId,
                Core.Enums.ProviderType.CosmosBlockChainOASIS, showPrivateKeys: true);
            if (wallet.IsError || wallet.Result == null || string.IsNullOrWhiteSpace(wallet.Result.PrivateKey))
                throw new InvalidOperationException($"Cosmos sender wallet could not be loaded: {wallet.Message}");
            key = wallet.Result.PrivateKey;
        }
        if (!string.IsNullOrWhiteSpace(suppliedPublicKey))
        {
            var restored = await _sdkBackend.InvokeAsync("restoreKey", new { privateKey = key });
            if (!string.Equals(restored.GetProperty("publicKey").GetString(), suppliedPublicKey, StringComparison.Ordinal))
                throw new ArgumentException("The supplied public key does not match the signing key.");
        }
        return key;
    }

    private async Task<string> SignerAddressAsync(string signingKey)
    {
        var key = await _sdkBackend.InvokeAsync("restoreKey", new { privateKey = signingKey });
        return key.GetProperty("address").GetString();
    }

    private async Task<string> ExecuteAssetAsync(object message, string signingKey, string fromWalletAddress = null, string contractAddress = null,
        PreparedSubmissionServer preparationServer = null, CancellationToken token = default)
    {
        var committed = await ExecuteContractCoreAsync(contractAddress ?? _contractAddress, new { asset = new { message } }, signingKey, fromWalletAddress, token, preparationServer);
        if (committed.IsError) throw committed.Exception ?? new InvalidOperationException(committed.Message);
        return committed.Result.TransactionResult;
    }

    private async Task<JsonElement> QueryAssetAsync(object message, string contractAddress = null)
    {
        var queried = await QueryContractAsync(contractAddress ?? _contractAddress, new { asset = new { message } });
        if (queried.IsError) throw queried.Exception ?? new InvalidOperationException(queried.Message);
        return queried.Result;
    }

    public async Task<OASISResult<JsonElement>> QueryContractAsync(string contractAddress, object message, CancellationToken token = default)
    {
        var result = new OASISResult<JsonElement>();
        try
        {
            if (string.IsNullOrWhiteSpace(contractAddress) || message == null)
                throw new ArgumentException("Contract address and query message are required.");
            result.Result = await _sdkBackend.InvokeAsync("query",
                new { targetContractAddress = contractAddress, message }, token);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public Task<OASISResult<ITransactionResponse>> ExecuteContractAsync(string contractAddress, object message,
        string signingKey = null, string fromWalletAddress = null, CancellationToken token = default)
        => ExecuteContractCoreAsync(contractAddress, message, signingKey, fromWalletAddress, token, null);

    private async Task<OASISResult<ITransactionResponse>> ExecuteContractCoreAsync(string contractAddress, object message,
        string signingKey, string fromWalletAddress, CancellationToken token, PreparedSubmissionServer preparationServer)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (string.IsNullOrWhiteSpace(contractAddress) || message == null)
                throw new ArgumentException("Contract address and execute message are required.");
            var key = signingKey ?? await SigningKeyAsync(Guid.Empty);
            var arguments = new { targetContractAddress = contractAddress, message, privateKey = key, fromWalletAddress };
            var committed = preparationServer == null
                ? await _sdkBackend.InvokeAsync("execute", arguments, token)
                : await _sdkBackend.InvokePreparedAsync("execute", arguments, preparationServer, token);
            var hash = committed.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(hash)) throw new InvalidOperationException("SDK returned no committed contract transaction hash.");
            result.Result = new TransactionResponse { TransactionResult = hash };
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public async Task<OASISResult<(long CodeId, string TransactionHash)>> UploadContractAsync(byte[] wasm, CancellationToken token = default)
    {
        var result = new OASISResult<(long CodeId, string TransactionHash)>();
        try
        {
            if (wasm == null || wasm.Length == 0) throw new ArgumentException("Compiled CosmWasm bytecode is required.");
            var uploaded = await _sdkBackend.InvokeAsync("upload", new { wasmBase64 = Convert.ToBase64String(wasm) }, token);
            var codeId = uploaded.GetProperty("codeId").GetInt64();
            var hash = uploaded.GetProperty("transactionHash").GetString();
            if (codeId <= 0 || string.IsNullOrWhiteSpace(hash))
                throw new InvalidOperationException("SDK returned no committed upload code ID or transaction hash.");
            result.Result = (codeId, hash);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public async Task<OASISResult<(string ContractAddress, string TransactionHash)>> InstantiateContractAsync(
        long codeId, object message, string label, CancellationToken token = default)
    {
        var result = new OASISResult<(string ContractAddress, string TransactionHash)>();
        try
        {
            if (codeId <= 0 || message == null || string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Positive code ID, instantiate message and label are required.");
            var created = await _sdkBackend.InvokeAsync("instantiate", new { codeId, message, label }, token);
            var address = created.GetProperty("contractAddress").GetString();
            var hash = created.GetProperty("transactionHash").GetString();
            if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(hash))
                throw new InvalidOperationException("SDK returned no instantiated contract address or committed transaction hash.");
            result.Result = (address, hash);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }
}
