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
    private const string Erc20MutationAbi="""
    [{"type":"function","name":"decimals","inputs":[],"outputs":[{"type":"uint8"}],"stateMutability":"view"},
    {"type":"function","name":"totalSupply","inputs":[],"outputs":[{"type":"uint256"}],"stateMutability":"view"},
    {"type":"function","name":"balanceOf","inputs":[{"type":"address"}],"outputs":[{"type":"uint256"}],"stateMutability":"view"},
    {"type":"function","name":"mint","inputs":[{"type":"address"},{"type":"uint256"}],"outputs":[],"stateMutability":"nonpayable"},
    {"type":"function","name":"burn","inputs":[{"type":"uint256"}],"outputs":[],"stateMutability":"nonpayable"}]
    """;

    protected virtual OASISResult<List<string>> ResolveErc20MintSigningKeys(Guid avatarId)
        =>KeyManager.Instance.GetProviderPrivateKeysForAvatarById(avatarId,ProviderType.Value);

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
            if(keys.Result==null || keys.Result.Count==0 || keys.Result.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("An authorized minter key is required.");
            var accounts=keys.Result.Select(key=>new Account(key,_expectedChainId)).GroupBy(account=>account.Address,StringComparer.OrdinalIgnoreCase).Select(group=>group.First()).ToList();
            if(accounts.Count!=1) throw new InvalidOperationException("Shared ERC20 minting requires one unambiguous authorized account.");
            return await ExecuteExplicitErc20Async(accounts[0],request.TokenAddress,request.ToWalletAddress,request.Amount,false);
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
            return await ExecuteExplicitErc20Async(new Account(request.OwnerPrivateKey,_expectedChainId),request.TokenAddress,null,request.Amount,true);
        }
        catch(Exception cause){OASISErrorHandling.HandleError(ref result,"Shared EVM burn preparation failed.",cause);if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";}
        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> ExecuteExplicitErc20Async(Account account,string tokenAddress,string recipient,decimal amount,bool burn)
    {
        var result=new OASISResult<ITransactionResponse>();TransactionReceipt receipt=null;
        try
        {
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
            if(burn && (before<units || supply<units)) throw new InvalidOperationException("Insufficient owned token balance or supply for the requested burn.");
            if(!burn && (supply+units>=(BigInteger.One<<256) || before+units>=(BigInteger.One<<256))) throw new ArgumentOutOfRangeException(nameof(amount));
            var arguments=burn ? new object[]{units}:new object[]{recipient,units};
            await contract.GetFunction(burn ? "burn":"mint").EstimateGasAsync(account.Address,null,null,arguments);
            var signer=_web3CoreOASIS.ForContractTransaction(account.PrivateKey,tokenAddress,Erc20MutationAbi);
            receipt=burn ? await signer.BurnErc20Async(units):await signer.MintErc20Async(recipient,units);
            var delta=burn ? -units:units;
            if(await contract.GetFunction("totalSupply").CallAsync<BigInteger>()!=supply+delta || await balance.CallAsync<BigInteger>(target)!=before+delta)
                throw new InvalidOperationException("Token receipt does not establish the requested exact supply and balance effect.");
            result.Result=new TransactionResponse{TransactionResult=receipt.TransactionHash};result.IsSaved=true;result.SavedCount=1;
            result.Message=burn ? "Exact requested ERC20 burn confirmed.":"Explicit exact ERC20 mint confirmed.";
        }
        catch(Exception cause)
        {
            if(receipt!=null && cause is not INativeMutationReconciliationRequired) cause=new Web3MutationReconciliationException(receipt.TransactionHash,receipt,cause);
            OASISErrorHandling.HandleError(ref result,"Shared EVM token mutation failed.",cause);
            if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}
