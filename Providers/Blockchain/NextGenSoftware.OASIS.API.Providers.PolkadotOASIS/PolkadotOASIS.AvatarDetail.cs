using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
{
    public partial class PolkadotOASIS
    {


    // NFT-specific lock/unlock methods
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
    {
        return LockNFTAsync(request).GetAwaiter().GetResult();
    }

    public Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        =>ChangeStoredNftEscrowAsync(request?.Web3NFTId??Guid.Empty,request?.LockedByAvatarId??Guid.Empty,request?.NFTTokenAddress,false);

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
    {
        return UnlockNFTAsync(request).GetAwaiter().GetResult();
    }

    public Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        =>ChangeStoredNftEscrowAsync(request?.Web3NFTId??Guid.Empty,request?.UnlockedByAvatarId??Guid.Empty,request?.NFTTokenAddress,true);

    // NFT Bridge Methods
    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                return result;
            }

            if(string.IsNullOrWhiteSpace(_bridgePoolAddress))
                throw new InvalidOperationException("An explicit bridge pool address is required; no guessed NFT custody destination is permitted.");
            var transfer=await SendNFTAsync(new SendWeb3NFTRequest {
                FromNFTTokenAddress=nftTokenAddress,TokenAddress=nftTokenAddress,TokenId=tokenId,
                FromWalletAddress=senderAccountAddress,ToWalletAddress=_bridgePoolAddress,Amount=1,
                OwnerPublicKey=senderAccountAddress,OwnerPrivateKey=senderPrivateKey
            }).ConfigureAwait(false);
            result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IWeb3NFTTransactionResponse,BridgeTransactionResponse>(transfer);
            var unresolved=transfer.Exception as INativeMutationReconciliationRequired;
            result.Result=new BridgeTransactionResponse {
                TransactionId=unresolved?.OriginalTransactionId??transfer.Result?.TransactionResult??string.Empty,
                IsSuccessful=!transfer.IsError&&transfer.Result!=null,
                ErrorMessage=transfer.IsError?transfer.Message:null,
                Status=unresolved!=null?BridgeTransactionStatus.Pending:
                    transfer.IsError?BridgeTransactionStatus.Failed:BridgeTransactionStatus.Completed
            };
            if(!transfer.IsError&&transfer.Result!=null)
                result.Message="Original PSP34 transfer to the configured custody pool finalized; cross-chain settlement is not established by this operation.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Polkadot provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress)||string.IsNullOrWhiteSpace(tokenId))
            {
                OASISErrorHandling.HandleError(ref result, "NFT collection, native token ID and receiver address are required");
                return result;
            }

            if(string.IsNullOrWhiteSpace(_bridgePoolAddress)||string.IsNullOrWhiteSpace(_privateKey))
                throw new InvalidOperationException("An explicit configured custody pool and signing key are required for NFT release.");
            // Release the existing token; never manufacture a different NFT as a deposit.
            // Any supplied source hash must prove this same collection/token entering
            // local custody. Cross-chain authorization remains the coordinator's job.
            var released=await ExecuteOwnedNftAsync(owner=>Task.FromResult(new NftIntent("transfer",
                NativeAccount(nftTokenAddress),CanonicalNftTokenId(tokenId),NativeAccount(_bridgePoolAddress),
                NativeAccount(receiverAccountAddress),NativeAccount(_bridgePoolAddress),_bridgePoolAddress,_privateKey,null,null,
                SourceCustodyTransaction:sourceTransactionHash))).ConfigureAwait(false);
            result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IWeb3NFTTransactionResponse,BridgeTransactionResponse>(released);
            var unresolved=released.Exception as INativeMutationReconciliationRequired;
            result.Result=new BridgeTransactionResponse {
                TransactionId=unresolved?.OriginalTransactionId??released.Result?.TransactionResult??string.Empty,
                IsSuccessful=!released.IsError&&released.Result!=null,ErrorMessage=released.IsError?released.Message:null,
                Status=unresolved!=null?BridgeTransactionStatus.Pending:
                    released.IsError?BridgeTransactionStatus.Failed:BridgeTransactionStatus.Completed
            };
            if(!released.IsError&&released.Result!=null)
                result.Message="Original PSP34 custody release finalized; cross-chain settlement is separate.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }


    }
}
