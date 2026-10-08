using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.Web3.Accounts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Hex.HexTypes;
using System.Threading.Tasks;
using System;
using System.Numerics;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public sealed class Web3CoreOASIS
{
    private readonly Web3 _web3;
    private readonly string _contractAddress;
    private readonly string _abi;

    private async Task<TransactionReceipt> SendAndWaitForReceiptAsync(Function function, params object[] functionInput)
    {
        string transactionHash = await function.SendTransactionAsync(
            _web3.TransactionManager.Account.Address,
            gas: new HexBigInteger(3_000_000),
            value: null,
            functionInput);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (!timeout.IsCancellationRequested)
        {
            TransactionReceipt? receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            if (receipt is not null)
            {
                if (!receipt.Succeeded())
                    throw new InvalidOperationException($"EVM transaction '{transactionHash}' reverted.");
                return receipt;
            }
            await Task.Delay(100, timeout.Token);
        }

        throw new TimeoutException($"Timed out waiting for EVM transaction '{transactionHash}' to be mined.");
    }

    public Web3CoreOASIS(string accountPrivateKey, string blockchainUrl, string contractAddress, string abi)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blockchainUrl);
        ArgumentException.ThrowIfNullOrEmpty(contractAddress);
        ArgumentException.ThrowIfNullOrEmpty(abi);

        _web3 = new(new Account(accountPrivateKey), blockchainUrl);
        _contractAddress = contractAddress;
        _abi = abi;
    }

    public Task<TransactionReceipt> CreateAvatarAsync(uint entityId, byte[] avatarId, byte[] info)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function createAvatarFunction = contract.GetFunction(Web3CoreOASISHelper.CreateAvatarFuncName);
        return SendAndWaitForReceiptAsync(createAvatarFunction, entityId, avatarId, info);
    }

    public Task<TransactionReceipt> CreateHolonAsync(uint entityId, byte[] holonId, byte[] info)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function createHolonFunction = contract.GetFunction(Web3CoreOASISHelper.CreateHolonFuncName);
        return SendAndWaitForReceiptAsync(createHolonFunction, entityId, holonId, info);
    }

    public Task<TransactionReceipt> CreateAvatarDetailAsync(uint entityId, byte[] avatarId, byte[] info)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function createAvatarDetailFunction = contract.GetFunction(Web3CoreOASISHelper.CreateAvatarDetailFuncName);
        return SendAndWaitForReceiptAsync(createAvatarDetailFunction, entityId, avatarId, info);
    }

    public async Task<bool> UpdateAvatarAsync(uint entityId, byte[] info)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function updateAvatarFunction = contract.GetFunction(Web3CoreOASISHelper.UpdateAvatarFuncName);
        var receipt = await SendAndWaitForReceiptAsync(updateAvatarFunction, entityId, info);
        return receipt.Succeeded();
    }

    public async Task<bool> UpdateAvatarDetailAsync(uint entityId, byte[] info)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function updateAvatarDetailFunction = contract.GetFunction(Web3CoreOASISHelper.UpdateAvatarDetailFuncName);
        var receipt = await SendAndWaitForReceiptAsync(updateAvatarDetailFunction, entityId, info);
        return receipt.Succeeded();
    }

    public async Task<bool> UpdateHolonAsync(uint entityId, byte[] info)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function updateHolonFunction = contract.GetFunction(Web3CoreOASISHelper.UpdateHolonFuncName);
        var receipt = await SendAndWaitForReceiptAsync(updateHolonFunction, entityId, info);
        return receipt.Succeeded();
    }

    public async Task<bool> DeleteAvatarAsync(uint entityId)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function deleteAvatarFunction = contract.GetFunction(Web3CoreOASISHelper.DeleteAvatarFuncName);
        var receipt = await SendAndWaitForReceiptAsync(deleteAvatarFunction, entityId);
        return receipt.Succeeded();
    }

    public async Task<bool> DeleteAvatarDetailAsync(uint entityId)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function deleteAvatarDetailFunction = contract.GetFunction(Web3CoreOASISHelper.DeleteAvatarDetailFuncName);
        var receipt = await SendAndWaitForReceiptAsync(deleteAvatarDetailFunction, entityId);
        return receipt.Succeeded();
    }

    public async Task<bool> DeleteHolonAsync(uint entityId)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function deleteHolonFunction = contract.GetFunction(Web3CoreOASISHelper.DeleteHolonFuncName);
        var receipt = await SendAndWaitForReceiptAsync(deleteHolonFunction, entityId);
        return receipt.Succeeded();
    }

    public async Task<EntityOASIS> GetAvatarByIdAsync(uint entityId)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function getAvatarByIdFunction = contract.GetFunction(Web3CoreOASISHelper.GetAvatarByIdFuncName);
        return (await getAvatarByIdFunction.CallDeserializingToObjectAsync<EntityOASISOutput>(entityId)).Entity;
    }

    public async Task<EntityOASIS> GetAvatarDetailByIdAsync(uint entityId)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function getAvatarDetailByIdFunction = contract.GetFunction(Web3CoreOASISHelper.GetAvatarDetailByIdFuncName);
        return (await getAvatarDetailByIdFunction.CallDeserializingToObjectAsync<EntityOASISOutput>(entityId)).Entity;
    }

    public async Task<EntityOASIS> GetHolonByIdAsync(uint entityId)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function getHolonByIdFunction = contract.GetFunction(Web3CoreOASISHelper.GetHolonByIdFuncName);
        return (await getHolonByIdFunction.CallDeserializingToObjectAsync<EntityOASISOutput>(entityId)).Entity;
    }

    public Task<uint> GetAvatarsCountAsync()
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function getAvatarsCountFunction = contract.GetFunction(Web3CoreOASISHelper.GetAvatarsCountFuncName);
        return getAvatarsCountFunction.CallAsync<uint>();
    }

    public Task<uint> GetAvatarDetailsCountAsync()
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function getAvatarDetailsCountFunction = contract.GetFunction(Web3CoreOASISHelper.GetAvatarDetailsCountFuncName);
        return getAvatarDetailsCountFunction.CallAsync<uint>();
    }

    public Task<uint> GetHolonsCountAsync()
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function getHolonsCountFunction = contract.GetFunction(Web3CoreOASISHelper.GetHolonsCountFuncName);
        return getHolonsCountFunction.CallAsync<uint>();
    }

    public Task<TransactionReceipt> SendTransactionAsync(string receiverAddress, decimal etherAmount) =>
        _web3.Eth.GetEtherTransferService().TransferEtherAndWaitForReceiptAsync(receiverAddress, etherAmount);
    

    public Task<TransactionReceipt> MintAsync(string toAddress, string metadataJson)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function mintFunction = contract.GetFunction(Web3CoreOASISHelper.MintFuncName);
        return SendAndWaitForReceiptAsync(mintFunction, toAddress, metadataJson);
    }

    public Task<TransactionReceipt> SendNFTAsync(
        string fromAddress, 
        string toAddress, 
        BigInteger tokenId, 
        string fromProviderType, 
        string toProviderType, 
        BigInteger amount, 
        string memoText)
    {
        Contract contract = _web3.Eth.GetContract(_abi, _contractAddress);
        Function sendNFTFunction = contract.GetFunction(Web3CoreOASISHelper.SendNFTFuncName);
        return SendAndWaitForReceiptAsync(sendNFTFunction,
            fromAddress,
            toAddress,
            tokenId,
            fromProviderType,
            toProviderType,
            amount,
            memoText
        );
    }
}

file static class Web3CoreOASISHelper
{
    public const string CreateAvatarFuncName = "createAvatar";
    public const string CreateAvatarDetailFuncName = "createAvatarDetail";
    public const string CreateHolonFuncName = "createHolon";
    public const string UpdateAvatarFuncName = "updateAvatar";
    public const string UpdateAvatarDetailFuncName = "updateAvatarDetail";
    public const string UpdateHolonFuncName = "updateHolon";
    public const string DeleteAvatarFuncName = "deleteAvatar";
    public const string DeleteAvatarDetailFuncName = "deleteAvatarDetail";
    public const string DeleteHolonFuncName = "deleteHolon";
    public const string GetAvatarByIdFuncName = "getAvatarById";
    public const string GetAvatarDetailByIdFuncName = "getAvatarDetailById";
    public const string GetHolonByIdFuncName = "getHolonById";
    public const string GetAvatarsCountFuncName = "getAvatarsCount";
    public const string GetAvatarDetailsCountFuncName = "getAvatarDetailsCount";
    public const string GetHolonsCountFuncName = "getHolonsCount";
    public const string MintFuncName = "mint";
    public const string SendNFTFuncName = "sendNFT";
}
