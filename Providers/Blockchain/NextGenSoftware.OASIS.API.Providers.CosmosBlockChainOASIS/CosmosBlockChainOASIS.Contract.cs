using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

public partial class CosmosBlockChainOASIS
{
    private async Task<string> SigningKeyAsync(Guid avatarId, string suppliedKey = null, string suppliedSeed = null)
    {
        if (!string.IsNullOrWhiteSpace(suppliedKey) && !string.IsNullOrWhiteSpace(suppliedSeed) && suppliedKey != suppliedSeed)
            throw new ArgumentException("Specify one sender key or seed phrase, not conflicting credentials.");
        if (!string.IsNullOrWhiteSpace(suppliedKey)) return suppliedKey;
        if (!string.IsNullOrWhiteSpace(suppliedSeed)) return suppliedSeed;
        if (avatarId == Guid.Empty)
        {
            if (string.IsNullOrWhiteSpace(_privateKey)) throw new InvalidOperationException("Cosmos signer is not configured.");
            return _privateKey;
        }
        var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(avatarId,
            Core.Enums.ProviderType.CosmosBlockChainOASIS, showPrivateKeys: true);
        if (wallet.IsError || wallet.Result == null || string.IsNullOrWhiteSpace(wallet.Result.PrivateKey))
            throw new InvalidOperationException($"Cosmos sender wallet could not be loaded: {wallet.Message}");
        return wallet.Result.PrivateKey;
    }

    private async Task<string> SignerAddressAsync(string signingKey)
    {
        var key = await ((CosmosSdkBackend)Backend).InvokeAsync("restoreKey", new { privateKey = signingKey });
        return key.GetProperty("address").GetString();
    }

    private async Task<string> ExecuteAssetAsync(object message, string signingKey, string fromWalletAddress = null, string contractAddress = null)
    {
        var committed = await ExecuteContractAsync(contractAddress ?? _contractAddress, new { asset = new { message } }, signingKey, fromWalletAddress);
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
            result.Result = await ((CosmosSdkBackend)Backend).InvokeAsync("query",
                new { targetContractAddress = contractAddress, message }, token);
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public async Task<OASISResult<ITransactionResponse>> ExecuteContractAsync(string contractAddress, object message,
        string signingKey = null, string fromWalletAddress = null, CancellationToken token = default)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (string.IsNullOrWhiteSpace(contractAddress) || message == null)
                throw new ArgumentException("Contract address and execute message are required.");
            var key = signingKey ?? await SigningKeyAsync(Guid.Empty);
            var committed = await ((CosmosSdkBackend)Backend).InvokeAsync("execute",
                new { targetContractAddress = contractAddress, message, privateKey = key, fromWalletAddress }, token);
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
            var uploaded = await ((CosmosSdkBackend)Backend).InvokeAsync("upload", new { wasmBase64 = Convert.ToBase64String(wasm) }, token);
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
            var created = await ((CosmosSdkBackend)Backend).InvokeAsync("instantiate", new { codeId, message, label }, token);
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
