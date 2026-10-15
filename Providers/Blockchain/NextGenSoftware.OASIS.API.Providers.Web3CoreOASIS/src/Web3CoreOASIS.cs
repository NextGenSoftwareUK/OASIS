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

    public Task<TransactionReceipt> ApplyAvatarLifecycleAsync(AvatarLifecycleRequest request)
        =>SendAndWaitForReceiptAsync(_web3.Eth.GetContract(_abi,_contractAddress).GetFunction("applyAvatarLifecycle"),request);

    public async Task<(EntityOASIS Avatar,EntityOASIS Detail)> ReadAvatarPairAsync(uint id)
    {
        var block=new BlockParameter(await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
        var contract=_web3.Eth.GetContract(_abi,_contractAddress);
        var avatar=await contract.GetFunction("lookupAvatar").CallDeserializingToObjectAsync<EntityOASISOutput>(block,id);
        var detail=await contract.GetFunction("lookupAvatarDetail").CallDeserializingToObjectAsync<EntityOASISOutput>(block,id);
        return (avatar.Entity,detail.Entity);
    }

    public Task<TransactionReceipt> UpsertAvatarAsync(uint id,byte[] external,byte[] info)
        =>SendAndWaitForReceiptAsync(_web3.Eth.GetContract(_abi,_contractAddress).GetFunction("upsertAvatar"),id,external,info);
    public Task<TransactionReceipt> UpsertAvatarDetailAsync(uint id,byte[] external,byte[] info)
        =>SendAndWaitForReceiptAsync(_web3.Eth.GetContract(_abi,_contractAddress).GetFunction("upsertAvatarDetail"),id,external,info);
    public Task<TransactionReceipt> UpsertHolonAsync(uint id,byte[] external,byte[] info)
        =>SendAndWaitForReceiptAsync(_web3.Eth.GetContract(_abi,_contractAddress).GetFunction("upsertHolon"),id,external,info);

    internal async Task<System.Collections.Generic.List<EntityOASIS>> ReadCollectionAsync(string kind)
    {
        var functionName=kind switch {"avatar"=>"getAvatarsPage","detail"=>"getAvatarDetailsPage","holon"=>"getHolonsPage",_=>throw new ArgumentException("Unknown Web3Core collection.",nameof(kind))};
        var block=new BlockParameter(await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
        var function=_web3.Eth.GetContract(_abi,_contractAddress).GetFunction(functionName);
        var records=new System.Collections.Generic.List<EntityOASIS>();
        var seen=new System.Collections.Generic.HashSet<BigInteger>();
        const uint limit=128;
        for(BigInteger offset=0;;offset+=limit)
        {
            var page=await function.CallDeserializingToObjectAsync<EntityOASISPageOutput>(block,offset,limit);
            if(page?.Entities==null || page.Entities.Count>limit) throw new InvalidOperationException("Invalid Web3Core page response.");
            foreach(var record in page.Entities)
            {
                if(record==null || !seen.Add(record.EntityId)) throw new InvalidOperationException("Duplicate/null Web3Core page identity.");
                records.Add(record);
            }
            if(page.Entities.Count<limit) return records;
        }
    }

    private Task<TransactionReceipt> SendAndWaitForReceiptAsync(Function function, params object[] functionInput)
        => Web3TransactionOutcome.SubmitAndObserveAsync(
            ()=>function.SendTransactionAsync(_web3.TransactionManager.Account.Address,
                gas:new HexBigInteger(3_000_000),value:null,functionInput),
            hash=>_web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash),
            TimeSpan.FromSeconds(60),TimeSpan.FromMilliseconds(100));

    public Web3CoreOASIS(string accountPrivateKey, string blockchainUrl, string contractAddress, string abi,BigInteger? chainId=null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blockchainUrl);
        ArgumentException.ThrowIfNullOrEmpty(contractAddress);
        ArgumentException.ThrowIfNullOrEmpty(abi);

        _web3 = new(new Account(accountPrivateKey,chainId), blockchainUrl);
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
    public const string GetAvatarByIdFuncName = "lookupAvatar";
    public const string GetAvatarDetailByIdFuncName = "lookupAvatarDetail";
    public const string GetHolonByIdFuncName = "lookupHolon";
    public const string GetAvatarsCountFuncName = "getAvatarsCount";
    public const string GetAvatarDetailsCountFuncName = "getAvatarDetailsCount";
    public const string GetHolonsCountFuncName = "getHolonsCount";
    public const string MintFuncName = "mint";
    public const string SendNFTFuncName = "sendNFT";
}
