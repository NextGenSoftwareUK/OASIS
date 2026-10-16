using Nethereum.Web3;
using Nethereum.Contracts;
using Nethereum.Web3.Accounts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Hex.HexTypes;
using System.Threading.Tasks;
using System;
using System.Numerics;
using System.Threading;
using Nethereum.Hex.HexConvertors.Extensions;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public sealed class Web3CoreOASIS
{
    private readonly Web3 _web3;
    private readonly string _contractAddress;
    private readonly string _abi;
    private readonly BigInteger? _chainId;
    private readonly AsyncLocal<Func<System.Text.Json.JsonElement,Task>> _preparation=new();
    private readonly SemaphoreSlim _mutationGate;
    private readonly string _blockchainUrl;
    internal string SigningAddress=>_web3.TransactionManager.Account.Address;
    internal bool HasNativePreparation => _preparation.Value != null;
    internal async Task<BigInteger> ReadChainIdAsync()=> (await _web3.Eth.ChainId.SendRequestAsync()).Value;
    internal Task<TransactionReceipt> ReadOriginalReceiptAsync(string hash)=>_web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash);

    internal async Task<T> WithNativePreparationAsync<T>(Func<Task<T>> operation,Func<System.Text.Json.JsonElement,Task> persist)
    {
        ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(persist);
        if(_preparation.Value!=null) throw new InvalidOperationException("Nested EVM preparation owners are not allowed.");
        _preparation.Value=persist;
        try {return await operation();}
        finally {_preparation.Value=null;}
    }

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
        =>UpsertObservedAsync("avatar",id,external,info,()=>GetAvatarByIdAsync(id));
    public Task<TransactionReceipt> UpsertAvatarDetailAsync(uint id,byte[] external,byte[] info)
        =>UpsertObservedAsync("detail",id,external,info,()=>GetAvatarDetailByIdAsync(id));
    public Task<TransactionReceipt> UpsertHolonAsync(uint id,byte[] external,byte[] info)
        =>UpsertObservedAsync("holon",id,external,info,()=>GetHolonByIdAsync(id));

    private async Task<TransactionReceipt> UpsertObservedAsync(string kind,uint id,byte[] external,byte[] info,Func<Task<EntityOASIS>> read)
    {
        // A failed read is known to precede submission: retain its original error, not an uncertain-write marker.
        var previous=await read();
        if(previous==null || previous.Info==null || previous.ExternalId?.Length!=32)
            throw new System.IO.InvalidDataException("Malformed Web3Core pre-write snapshot.");
        var present=!Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(previous);
        var hash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(previous.Info);
        return await UpsertCheckedAsync(kind,id,external,present,hash,info);
    }

    public Task<TransactionReceipt> UpsertCheckedAsync(string kind,uint id,byte[] external,bool expectedPresent,byte[] expectedHash,byte[] info)
    {
        var name=kind switch {"avatar"=>"upsertAvatarChecked","detail"=>"upsertAvatarDetailChecked","holon"=>"upsertHolonChecked",_=>throw new ArgumentException("Unknown Web3Core collection.",nameof(kind))};
        return SendAndWaitForReceiptAsync(_web3.Eth.GetContract(_abi,_contractAddress).GetFunction(name),id,external,expectedPresent,expectedHash,info);
    }

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
        =>SendInputAndWaitForReceiptAsync(new TransactionInput {From=_web3.TransactionManager.Account.Address,To=_contractAddress,
            Gas=new HexBigInteger(3_000_000),Data=function.GetData(functionInput)});

    private async Task<TransactionReceipt> SendInputAndWaitForReceiptAsync(TransactionInput input)
    {
        await _mutationGate.WaitAsync();
        try
        {
        var persist=_preparation.Value;
        if(persist!=null && (input.Value?.Value??BigInteger.Zero)!=BigInteger.Zero)
            throw new InvalidOperationException("Admitted native-value recovery requires a value-aware journal contract before submission.");
        // Signing must not consume a cached nonce if preparation fails before submission.
        input.Nonce=await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(input.From,BlockParameter.CreatePending());
        var signed=await _web3.TransactionManager.SignTransactionAsync(input);
        var bytes=signed.HexToByteArray();
        if(bytes.Length==0) throw new InvalidOperationException("EVM signer returned empty signed transaction.");
        var hash="0x"+Convert.ToHexString(Nethereum.Util.Sha3Keccack.Current.CalculateHash(bytes)).ToLowerInvariant();
        Web3SignedPreparation.Verify(bytes,hash,_chainId ?? throw new InvalidOperationException("Signed EVM mutations require an explicit chain identity."),input.From,_contractAddress,input.Data.HexToByteArray(),input.Value?.Value??System.Numerics.BigInteger.Zero);
        if(persist!=null)
            await persist(System.Text.Json.JsonSerializer.SerializeToElement(new {
                transactionHash=hash,chainId=_chainId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),sender=input.From,
                signedTransactionBase64=Convert.ToBase64String(bytes),contract=_contractAddress,calldata=input.Data}));
        return await Web3TransactionOutcome.SubmitAndObserveAsync(
            ()=>_web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(signed),
            hash=>_web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(hash),
            TimeSpan.FromSeconds(60),TimeSpan.FromMilliseconds(100),hash);
        }
        finally {_mutationGate.Release();}
    }

    public Web3CoreOASIS(string accountPrivateKey, string blockchainUrl, string contractAddress, string abi,BigInteger? chainId=null)
        : this(accountPrivateKey, blockchainUrl, contractAddress, abi, chainId, new SemaphoreSlim(1, 1)) { }

    private Web3CoreOASIS(string accountPrivateKey, string blockchainUrl, string contractAddress, string abi,
        BigInteger? chainId, SemaphoreSlim mutationGate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blockchainUrl);
        ArgumentException.ThrowIfNullOrEmpty(contractAddress);
        ArgumentException.ThrowIfNullOrEmpty(abi);

        _web3 = new(new Account(accountPrivateKey,chainId), blockchainUrl);
        _contractAddress = contractAddress;
        _abi = abi;
        _chainId = chainId;
        _blockchainUrl = blockchainUrl;
        _mutationGate = mutationGate;
    }

    internal Web3CoreOASIS ForNftTransaction(string privateKey, string contractAddress, string abi)
        =>ForContractTransaction(privateKey,contractAddress,abi);

    internal Web3CoreOASIS ForContractTransaction(string privateKey,string contractAddress,string abi)
    {
        var sdk = new Web3CoreOASIS(privateKey, _blockchainUrl, contractAddress, abi, _chainId, _mutationGate);
        if (HasNativePreparation)
            throw new InvalidOperationException("Contract transaction target or signer differs from the admitted native preparation owner.");
        return sdk;
    }

    internal Task<TransactionReceipt> MintErc20Async(string recipient,BigInteger amount)
        =>SendAndWaitForReceiptAsync(_web3.Eth.GetContract(_abi,_contractAddress).GetFunction("mint"),recipient,amount);

    internal Task<TransactionReceipt> BurnErc20Async(BigInteger amount)
        =>SendAndWaitForReceiptAsync(_web3.Eth.GetContract(_abi,_contractAddress).GetFunction("burn"),amount);

    internal Task<TransactionReceipt> TransferErc20Async(string recipient,BigInteger amount)
        =>SendAndWaitForReceiptAsync(_web3.Eth.GetContract(_abi,_contractAddress).GetFunction("transfer"),recipient,amount);

    internal async Task<TransactionReceipt> SendNativeAsync(BigInteger wei)
    {
        var input=new TransactionInput {From=_web3.TransactionManager.Account.Address,To=_contractAddress,Value=new HexBigInteger(wei),Data="0x"};
        input.Gas=await _web3.Eth.Transactions.EstimateGas.SendRequestAsync(input);
        return await SendInputAndWaitForReceiptAsync(input);
    }

    internal Task<TransactionReceipt> TransferErc721Async(string from, string to, BigInteger tokenId)
        => SendAndWaitForReceiptAsync(_web3.Eth.GetContract(_abi, _contractAddress).GetFunction("transferFrom"), from, to, tokenId);

    internal Task<TransactionReceipt> BurnErc721Async(BigInteger tokenId)
        => SendAndWaitForReceiptAsync(_web3.Eth.GetContract(Web3Erc721Burn.Abi, _contractAddress).GetFunction("burn"), tokenId);

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

    public Task<TransactionReceipt> DeleteHolonCheckedAsync(uint entityId,byte[] externalId,byte[] expectedHash)
    {
        var function=_web3.Eth.GetContract(_abi,_contractAddress).GetFunction("deleteHolonChecked");
        return SendAndWaitForReceiptAsync(function,entityId,externalId,expectedHash);
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
