using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Explicit public identity/URI projection, not off-chain uploading or URL retrieval.</summary>
public static class NftMetadataProjection
{
    public static void Validate(bool? storeOnChain, string metadataUrl)
    {
        if (storeOnChain != false) return;
        if (!Uri.TryCreate(metadataUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https" && uri.Scheme != "ipfs" && uri.Scheme != "ar")
            || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Off-chain metadata requires an absolute HTTP(S), IPFS or Arweave URI without user credentials; metadata must not be silently published.");
    }

    public static IWeb3NFT ForOnChainStorage(IWeb3NFT nft)
    {
        ArgumentNullException.ThrowIfNull(nft);
        Validate(nft.StoreNFTMetaDataOnChain, nft.JSONMetaDataURL);
        if (nft.StoreNFTMetaDataOnChain) return nft;
        // Whitelist only identity/transport fields. Exempt metadata keys are not public approval.
        return new Web3NFT
        {
            Id = nft.Id, NFTTokenAddress = nft.NFTTokenAddress,
            CollectionPublicKey = nft.CollectionPublicKey, NFTStandardType = nft.NFTStandardType,
            OnChainProvider = nft.OnChainProvider, JSONMetaDataURL = nft.JSONMetaDataURL,
            StoreNFTMetaDataOnChain = false, SendToAddressAfterMinting = nft.SendToAddressAfterMinting,
            NFTMintedUsingWalletAddress = nft.NFTMintedUsingWalletAddress,
            OASISMintWalletAddress = nft.OASISMintWalletAddress
        };
    }
}
