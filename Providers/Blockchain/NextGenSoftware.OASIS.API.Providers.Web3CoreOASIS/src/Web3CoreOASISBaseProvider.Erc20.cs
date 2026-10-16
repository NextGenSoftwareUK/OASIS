using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider
{
    private enum ExplicitTokenMutation { Mint,Burn,Transfer }
    private const string Erc20MutationAbi="""
    [{"type":"function","name":"decimals","inputs":[],"outputs":[{"type":"uint8"}],"stateMutability":"view"},
    {"type":"function","name":"totalSupply","inputs":[],"outputs":[{"type":"uint256"}],"stateMutability":"view"},
    {"type":"function","name":"balanceOf","inputs":[{"type":"address"}],"outputs":[{"type":"uint256"}],"stateMutability":"view"},
    {"type":"function","name":"mint","inputs":[{"type":"address"},{"type":"uint256"}],"outputs":[],"stateMutability":"nonpayable"},
    {"type":"function","name":"burn","inputs":[{"type":"uint256"}],"outputs":[],"stateMutability":"nonpayable"},
    {"type":"function","name":"transfer","inputs":[{"type":"address"},{"type":"uint256"}],"outputs":[{"type":"bool"}],"stateMutability":"nonpayable"}]
    """;

    protected virtual OASISResult<List<string>> ResolveErc20MintSigningKeys(Guid avatarId)
        =>KeyManager.Instance.GetProviderPrivateKeysForAvatarById(avatarId,ProviderType.Value);

    protected virtual async Task<OASISResult<string>> ResolveDefaultErc20MintWalletAsync(Guid avatarId)
    {
        var wallet=await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarId,ProviderType.Value);
        return new OASISResult<string>{Result=wallet.Result?.WalletAddress,IsError=wallet.IsError,
            Message=wallet.Message,Exception=wallet.Exception,ErrorCode=wallet.ErrorCode};
    }

    private async Task<OASISResult<ITransactionResponse>> MintExplicitErc20Async(IMintWeb3TokenRequest request)
    {
        var result=new OASISResult<ITransactionResponse>();
        try
        {
            if(request==null || request.MintedByAvatarId==Guid.Empty || request.Amount<=0
                || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(request.TokenAddress)
                || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(request.ToWalletAddress)
                || string.Equals(request.ToWalletAddress,"0x"+new string('0',40),StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A minter avatar, explicit existing token, nonzero recipient and positive amount are required.");
            if(!_expectedChainId.HasValue || _expectedChainId.Value<=0) throw new InvalidOperationException("ERC20 minting requires an explicit chain identity.");
            var keys=ResolveErc20MintSigningKeys(request.MintedByAvatarId);
            if(keys.IsError){OASISErrorHandling.HandleError(ref result,keys.Message,keys.Exception);result.ErrorCode=keys.ErrorCode;return result;}
            var selected=await NextGenSoftware.OASIS.Providers.Shared.Evm.EvmMintAccountSelection.SelectAsync(
                keys.Result,_expectedChainId.Value,()=>ResolveDefaultErc20MintWalletAsync(request.MintedByAvatarId));
            if(selected.IsError){OASISErrorHandling.HandleError(ref result,selected.Message,selected.Exception);result.ErrorCode=selected.ErrorCode;return result;}
            return await ExecuteExplicitErc20Async(selected.Result,request.TokenAddress,request.ToWalletAddress,request.Amount,ExplicitTokenMutation.Mint);
        }
        catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Shared EVM mint preparation failed.",cause);if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";}
        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> BurnExplicitErc20Async(IBurnWeb3TokenRequest request)
    {
        var result=new OASISResult<ITransactionResponse>();
        try
        {
            if(request==null || request.Amount<=0 || string.IsNullOrWhiteSpace(request.OwnerPrivateKey)
                || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(request.TokenAddress))
                throw new ArgumentException("An explicit token, owner signing key and positive requested burn amount are required.");
            if(!_expectedChainId.HasValue || _expectedChainId.Value<=0) throw new InvalidOperationException("ERC20 burning requires an explicit chain identity.");
            return await ExecuteExplicitErc20Async(new Account(request.OwnerPrivateKey,_expectedChainId),request.TokenAddress,null,request.Amount,ExplicitTokenMutation.Burn);
        }
        catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Shared EVM burn preparation failed.",cause);if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";}
        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> SendExplicitTokenAsync(ISendWeb3TokenRequest request)
    {
        var result=new OASISResult<ITransactionResponse>();
        try
        {
            if(request==null || request.Amount<=0 || (!string.IsNullOrWhiteSpace(request.FromTokenAddress) && !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(request.FromTokenAddress))
                || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(request.ToWalletAddress)
                || string.Equals(request.ToWalletAddress,"0x"+new string('0',40),StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A valid token when specified, nonzero recipient and positive transfer amount are required.");
            if(!_expectedChainId.HasValue || _expectedChainId.Value<=0) throw new InvalidOperationException("Token transfers require an explicit chain identity.");
            var keys=new List<string>();if(!string.IsNullOrWhiteSpace(request.OwnerPrivateKey)) keys.Add(request.OwnerPrivateKey);
            if(request is NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.SendWeb3TokenRequest concrete && !string.IsNullOrWhiteSpace(concrete.FromWalletPrivateKey)) keys.Add(concrete.FromWalletPrivateKey);
            var accounts=keys.Select(key=>new Account(key,_expectedChainId)).GroupBy(account=>account.Address,StringComparer.OrdinalIgnoreCase).Select(group=>group.First()).ToList();
            if(accounts.Count!=1) throw new ArgumentException("One consistent explicit transfer signing account is required.");
            var account=accounts[0];
            if(!string.IsNullOrWhiteSpace(request.FromWalletAddress) && !string.Equals(request.FromWalletAddress,account.Address,StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Explicit sender address does not match the signing key.");
            if(string.IsNullOrWhiteSpace(request.FromTokenAddress))
            {
                if(!string.IsNullOrEmpty(request.MemoText)) throw new NotSupportedException("Native on-chain memo encoding is not specified; no memo will be silently discarded or published.");
                var wei=ExactEvmTokenAmount.ToUnits(request.Amount,18);
                return await ExecuteNativeTransferAsync(account,request.ToWalletAddress,wei);
            }
            return await ExecuteExplicitErc20Async(account,request.FromTokenAddress,request.ToWalletAddress,request.Amount,ExplicitTokenMutation.Transfer);
        }
        catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Shared EVM transfer preparation failed.",cause);if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";}
        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> ExecuteNativeTransferAsync(Account account,string recipient,BigInteger wei)
    {
        var result=new OASISResult<ITransactionResponse>();TransactionReceipt receipt=null;
        try
        {
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError){OASISErrorHandling.HandleError(ref result,activation.Message,activation.Exception);result.ErrorCode=activation.ErrorCode;return result;}
            }
            var signer=_web3CoreOASIS.ForContractTransaction(account.PrivateKey,recipient,"[]");
            receipt=await signer.SendNativeAsync(wei);
            var transaction=await _web3Client.Eth.Transactions.GetTransactionByHash.SendRequestAsync(receipt.TransactionHash);
            if(transaction==null || transaction.Value?.Value!=wei || !string.Equals(transaction.From,account.Address,StringComparison.OrdinalIgnoreCase)
                || !string.Equals(transaction.To,recipient,StringComparison.OrdinalIgnoreCase) || !string.Equals(transaction.TransactionHash,receipt.TransactionHash,StringComparison.OrdinalIgnoreCase)
                || transaction.Input!="0x") throw new InvalidOperationException("Native transfer receipt does not establish the exact requested sender, recipient and wei value.");
            result.Result=new TransactionResponse{TransactionResult=receipt.TransactionHash};result.IsSaved=true;result.SavedCount=1;result.Message="Exact native-coin transfer confirmed.";
        }
        catch(Exception cause)
        {
            if(receipt!=null && cause is not INativeMutationReconciliationRequired) cause=new Web3MutationReconciliationException(receipt.TransactionHash,receipt,cause);
            OASISErrorHandling.HandleError(ref result,"Shared EVM native transfer failed.",cause);
        }
        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> ExecuteExplicitErc20Async(Account account,string tokenAddress,string recipient,decimal amount,ExplicitTokenMutation operation)
    {
        var result=new OASISResult<ITransactionResponse>();TransactionReceipt receipt=null;
        try
        {
            var burn=operation==ExplicitTokenMutation.Burn;var mint=operation==ExplicitTokenMutation.Mint;
            if(!IsProviderActivated)
            {
                var activation=await ActivateProviderAsync();
                if(activation.IsError){OASISErrorHandling.HandleError(ref result,activation.Message,activation.Exception);result.ErrorCode=activation.ErrorCode;return result;}
            }
            var contract=_web3Client.Eth.GetContract(Erc20MutationAbi,tokenAddress);
            var units=ExactEvmTokenAmount.ToUnits(amount,await contract.GetFunction("decimals").CallAsync<byte>());
            var supply=await contract.GetFunction("totalSupply").CallAsync<BigInteger>();
            var target=burn ? account.Address:recipient;
            var balance=contract.GetFunction("balanceOf");var before=await balance.CallAsync<BigInteger>(target);
            var self=!mint && !burn && string.Equals(account.Address,recipient,StringComparison.OrdinalIgnoreCase);
            var ownerBefore=mint ? BigInteger.Zero:burn || self ? before:await balance.CallAsync<BigInteger>(account.Address);
            if((!mint && ownerBefore<units) || (burn && supply<units)) throw new InvalidOperationException("Insufficient owned token balance or supply.");
            if((mint && supply+units>=(BigInteger.One<<256)) || (!burn && !self && before+units>=(BigInteger.One<<256))) throw new ArgumentOutOfRangeException(nameof(amount));
            var arguments=burn ? new object[]{units}:new object[]{recipient,units};
            await contract.GetFunction(burn ? "burn":mint ? "mint":"transfer").EstimateGasAsync(account.Address,null,null,arguments);
            var signer=_web3CoreOASIS.ForContractTransaction(account.PrivateKey,tokenAddress,Erc20MutationAbi);
            receipt=burn ? await signer.BurnErc20Async(units):mint ? await signer.MintErc20Async(recipient,units):await signer.TransferErc20Async(recipient,units);
            var supplyDelta=burn ? -units:mint ? units:BigInteger.Zero;
            var targetDelta=burn ? -units:self ? BigInteger.Zero:units;
            if(await contract.GetFunction("totalSupply").CallAsync<BigInteger>()!=supply+supplyDelta || await balance.CallAsync<BigInteger>(target)!=before+targetDelta
                || !mint && !burn && await balance.CallAsync<BigInteger>(account.Address)!=ownerBefore-(self ? BigInteger.Zero:units))
                throw new InvalidOperationException("Token receipt does not establish the requested exact supply and balance effect.");
            if(!mint && !burn) VerifyExactErc20TransferEvent(receipt,tokenAddress,account.Address,recipient,units);
            result.Result=new TransactionResponse{TransactionResult=receipt.TransactionHash};result.IsSaved=true;result.SavedCount=1;
            result.Message=burn ? "Exact requested ERC20 burn confirmed.":mint ? "Explicit exact ERC20 mint confirmed.":"Exact ERC20 transfer confirmed.";
        }
        catch(Exception cause)
        {
            if(receipt!=null && cause is not INativeMutationReconciliationRequired) cause=new Web3MutationReconciliationException(receipt.TransactionHash,receipt,cause);
            OASISErrorHandling.HandleError(ref result,"Shared EVM token mutation failed.",cause);
            if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }

    private static void VerifyExactErc20TransferEvent(TransactionReceipt receipt,string token,string sender,string recipient,BigInteger units)
    {
        var signature="0x"+Nethereum.Util.Sha3Keccack.Current.CalculateHash("Transfer(address,address,uint256)");
        var from="0x"+sender.Substring(2).PadLeft(64,'0');var to="0x"+recipient.Substring(2).PadLeft(64,'0');
        var data="0x"+Convert.ToHexString(units.ToByteArray(isUnsigned:true,isBigEndian:true)).PadLeft(64,'0');
        if(receipt.Logs==null || receipt.Logs.Count(log=>string.Equals(log.Address,token,StringComparison.OrdinalIgnoreCase)
            && log.Topics is {Length:3} topics && string.Equals(topics[0]?.ToString(),signature,StringComparison.OrdinalIgnoreCase)
            && string.Equals(topics[1]?.ToString(),from,StringComparison.OrdinalIgnoreCase) && string.Equals(topics[2]?.ToString(),to,StringComparison.OrdinalIgnoreCase)
            && string.Equals(log.Data,data,StringComparison.OrdinalIgnoreCase))!=1)
            throw new System.IO.InvalidDataException("Transfer receipt lacks exactly one matching token/sender/recipient/amount event.");
    }
}
