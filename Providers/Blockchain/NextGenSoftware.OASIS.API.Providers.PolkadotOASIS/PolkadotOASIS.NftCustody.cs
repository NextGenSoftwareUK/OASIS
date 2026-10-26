using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Helpers.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    // Resolve the application's GUID through its authoritative stored NFT; it
    // is never a native PSP34 token ID, nor proof that this avatar owns it.
    protected virtual async Task<OASISResult<IWeb3NFT>> ResolveStoredWeb3NftAsync(Guid id)
    {
        var loaded=await HolonManager.Instance.LoadHolonAsync(id,loadChildren:false).ConfigureAwait(false);
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IWeb3NFT>(loaded);
        if(loaded.IsError||loaded.Result==null)return result;
        if(loaded.Result.Id!=id||loaded.Result.HolonType!=HolonType.Web3NFT||loaded.Result.IsDeleted)
            throw new InvalidOperationException("Stored NFT holon identity/type/lifecycle differs from the requested record.");
        result.Result=Web3NftMetadata.Deserialize(loaded.Result);
        return result;
    }

    private Task<OASISResult<IWeb3NFTTransactionResponse>> LockStoredNftAsync(Guid id,Guid avatar,string collection)
        =>ExecuteOwnedNftAsync(async owner=>{
            if(id==Guid.Empty||avatar==Guid.Empty)throw new ArgumentException("Stored NFT ID and authenticated locking avatar are required.");
            if(string.IsNullOrWhiteSpace(_bridgePoolAddress))throw new InvalidOperationException("An explicit NFT custody pool is required.");
            var requestedCollection=NftAccount(collection);
            var loaded=await ResolveStoredWeb3NftAsync(id).ConfigureAwait(false);
            owner.Token.ThrowIfCancellationRequested();
            if(loaded.IsError||loaded.Result==null)throw loaded.Exception??new InvalidOperationException(loaded.Message);
            var nft=loaded.Result;
            if(nft.Id!=id||nft.OnChainProvider?.Value!=Core.Enums.ProviderType.PolkadotOASIS
                ||nft.NFTStandardType?.Value!=NFTStandardType.PSP34)
                throw new InvalidOperationException("Stored NFT does not identify the requested Polkadot PSP34 record.");
            // Freeze the stored locator before awaiting the signing-wallet lookup.
            var contract=NftAccount(nft.NFTTokenAddress);var token=CanonicalNftTokenId(nft.TokenId);
            var wallet=await ResolveAssetMintWalletAsync(avatar).ConfigureAwait(false);
            owner.Token.ThrowIfCancellationRequested();
            if(wallet.IsError||wallet.Result==null)throw wallet.Exception??new InvalidOperationException(wallet.Message);
            if(wallet.Result.AvatarId!=avatar||wallet.Result.ProviderType!=Core.Enums.ProviderType.PolkadotOASIS)
                throw new InvalidOperationException("Locking wallet does not belong to the requested avatar/provider.");
            var sender=NftAccount(wallet.Result.WalletAddress);
            // The SDK additionally proves key/address agreement and actual owner
            // before signing; the finalized event must prove the exact custody leg.
            return new NftIntent("transfer",contract,token,sender,NftAccount(_bridgePoolAddress),sender,
                wallet.Result.PublicKey,wallet.Result.PrivateKey,null,null,ContractAlias:requestedCollection);
        });
}
