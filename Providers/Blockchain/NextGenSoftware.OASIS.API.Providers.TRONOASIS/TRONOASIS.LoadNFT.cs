using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public partial class TRONOASIS
    {
        // Identifies this adapter's expected NFT ABI, not a full contract-conformance audit.
        private static Web3NFT NativeNftDescriptor(string nativeAddress,string metadataUri)=>new()
        {
            NFTTokenAddress=nativeAddress,JSONMetaDataURL=metadataUri,
            OnChainProvider=new EnumValue<ProviderType>(Core.Enums.ProviderType.TRONOASIS),
            NFTStandardType=new EnumValue<NFTStandardType>(Core.Enums.NFTStandardType.TRC721)
        };
        /// <summary>Queries one explicit enumerable TRC721 collection. This is not cross-collection discovery.</summary>
        public async Task<OASISResult<List<IWeb3NFT>>> LoadNFTsForCollectionOwnerAsync(string collectionAddress, string ownerAddress)
        {
            var result = new OASISResult<List<IWeb3NFT>>();
            try
            {
                if (string.IsNullOrWhiteSpace(collectionAddress) || string.IsNullOrWhiteSpace(ownerAddress))
                    throw new ArgumentException("Explicit collection and owner addresses are required.");
                result.Result = await ExecuteNativeRuntimeAsync(async client =>
                {
                    var ids = await client.GetNftIdsForOwnerAsync(collectionAddress, ownerAddress);
                    var items = new List<IWeb3NFT>();
                    foreach (var id in ids)
                        items.Add(NativeNftDescriptor(collectionAddress + "/" + id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            await client.GetNftMetadataUriAsync(collectionAddress, id)));
                    return items;
                });
                result.Message = "Native NFT identities and metadata URIs queried for the explicit collection and owner; no OASIS IDs or off-chain metadata are fabricated.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying TRON collection inventory: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<List<IWeb3NFT>> LoadAllNFTsForAvatar(Guid avatarId)
        {
            return LoadAllNFTsForAvatarAsync(avatarId).Result;
        }

        public async Task<OASISResult<List<IWeb3NFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
        {
            var result = new OASISResult<List<IWeb3NFT>>();
            try
            {
                if (avatarId == Guid.Empty) throw new ArgumentException("Avatar ID is required.");
                var wallet = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager,
                    Core.Enums.ProviderType.TRONOASIS, avatarId);
                if (wallet.IsError || string.IsNullOrWhiteSpace(wallet.Result))
                    throw new InvalidOperationException("Could not resolve the avatar's TRON wallet.");
                return await LoadAllNFTsForMintAddressAsync(wallet.Result);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading TRON avatar inventory: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<List<IWeb3NFT>> LoadAllNFTsForMintAddress(string mintWalletAddress)
        {
            return LoadAllNFTsForMintAddressAsync(mintWalletAddress).GetAwaiter().GetResult();
        }

        public async Task<OASISResult<List<IWeb3NFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
        {
            var result = new OASISResult<List<IWeb3NFT>>();
            try
            {
                if (string.IsNullOrWhiteSpace(mintWalletAddress)) throw new ArgumentException("A TRON owner wallet is required.");
                result.Result = await ExecuteNativeRuntimeAsync(async client =>
                {
                    var history = await new TronGridTokenHistoryClient(_httpClient, client.ExpectedGenesisBlockId)
                        .ReadAsync(mintWalletAddress, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    var addresses = await client.GetIndexedOwnedNftAddressesAsync(mintWalletAddress,
                        history.Select(record => record.GetProperty("transaction_id").GetString()));
                    var items = new List<IWeb3NFT>();
                    foreach (var address in addresses)
                    {
                        var asset = TronNativeTransferClient.ParseNftAddress(address);
                        items.Add(NativeNftDescriptor(address,await client.GetNftMetadataUriAsync(asset.Contract, asset.TokenId)));
                    }
                    return items;
                });
                result.Message = "Native NFT identities replayed from solidified receipt order and checked against current ownership and collection balances; indexer coverage and block-pinned completeness are not guaranteed.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading TRON indexed inventory: {ex.Message}", ex);
            }
            return result;
        }

        // PlaceGeoNFT methods commented out - IPlaceGeoSpatialNFTRequest and IOASISGeoSpatialNFT interfaces not found
        // public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request)
        // {
        //     return PlaceGeoNFTAsync(request).Result;
        // }

        // public async Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        // {
        //     var result = new OASISResult<IOASISGeoSpatialNFT>();
        //     string errorMessage = "Error in PlaceGeoNFTAsync method in TRONOASIS Provider. Reason: ";
        //
        //     try
        //     {
        //         if (request == null)
        //         {
        //             OASISErrorHandling.HandleError(ref result, $"{errorMessage} Request cannot be null");
        //             return result;
        //         }
        //
        //         ... (entire method body commented out - IPlaceGeoSpatialNFTRequest and IOASISGeoSpatialNFT interfaces not found)
        //     }
        //     catch (Exception ex)
        //     {
        //         OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
        //     }
        //
        //     return result;
        // }

        // MintAndPlaceGeoNFT methods commented out - interfaces not found
        // public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request)
        // {
        //     return MintAndPlaceGeoNFTAsync(request).Result;
        // }

        // public async Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
        // {
        //     var result = new OASISResult<IOASISGeoSpatialNFT>();
        //     string errorMessage = "Error in MintAndPlaceGeoNFTAsync method in TRONOASIS Provider. Reason: ";
        //
        //     try
        //     {
        //         ... (method body commented out - IMintAndPlaceGeoSpatialNFTRequest interface not found)
        //     }
        //     catch (Exception ex)
        //     {
        //         OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
        //     }
        //
        //     return result;
        // }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
            => LoadOnChainNFTDataAsync(nftTokenAddress).GetAwaiter().GetResult();

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IWeb3NFT>();
            try
            {
                var asset = TronNativeTransferClient.ParseNftAddress(nftTokenAddress);
                var uri = await ExecuteNativeRuntimeAsync(client => client.GetNftMetadataUriAsync(asset.Contract, asset.TokenId));
                response.Result = NativeNftDescriptor(nftTokenAddress,uri);
                response.Message = "TRC721 existence and metadata URI queried through the verified SDK node; off-chain metadata is not fetched.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from TRON: {ex.Message}", ex);
            }
            return response;
        }

        // NFT-specific lock/unlock methods
        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        {
            return await ExecuteOwnedNftEscrowAsync(request?.LockedByAvatarId ?? Guid.Empty, request?.NFTTokenAddress, false);
        }

        private async Task<OASISResult<IWeb3NFTTransactionResponse>> ExecuteOwnedNftEscrowAsync(Guid avatarId, string nftAddress, bool unlock)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (avatarId == Guid.Empty) throw new ArgumentException("An avatar ID is required for NFT depositor signing.");
                TronNativeTransferClient.ParseNftAddress(nftAddress);
                if (string.IsNullOrWhiteSpace(_escrowContractAddress))
                    throw new InvalidOperationException("A separate EscrowContractAddress with NFT escrow support must be configured.");
                var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(avatarId,
                    Core.Enums.ProviderType.TRONOASIS, showPrivateKeys: true);
                if (wallet.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IProviderWallet,IWeb3NFTTransactionResponse>(wallet);
                if (wallet.Result == null || wallet.Result.AvatarId != avatarId || string.IsNullOrWhiteSpace(wallet.Result.PrivateKey))
                    throw new InvalidOperationException("Could not resolve the avatar's TRON depositor signing wallet.");
                var hash = await ExecuteNativeRuntimeAsync(client => client.ExecuteNftEscrowAsync(wallet.Result.WalletAddress,
                    _escrowContractAddress, nftAddress, wallet.Result.PrivateKey, unlock));
                result.Result = new Web3NFTTransactionResponse {TransactionResult=hash};
                result.Message = "Depositor-owned NFT escrow operation confirmed by solidified contract receipt.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error executing owned NFT escrow: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            return await ExecuteOwnedNftEscrowAsync(request?.UnlockedByAvatarId ?? Guid.Empty, request?.NFTTokenAddress, true);
        }

        // NFT Bridge Methods
        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(senderAccountAddress)
                    || string.IsNullOrWhiteSpace(senderPrivateKey))
                    throw new ArgumentException("NFT contract, sender and private signing key are required.");
                if (!System.Text.RegularExpressions.Regex.IsMatch(tokenId ?? "", "\\A[0-9]+\\z")
                    || !System.Numerics.BigInteger.TryParse(tokenId, out var nativeId)
                    || nativeId >= (System.Numerics.BigInteger.One << 256))
                    throw new ArgumentException("NFT token ID must be an explicit unsigned uint256 decimal value.");
                if (string.IsNullOrWhiteSpace(_bridgePoolWalletAddress))
                    throw new InvalidOperationException("BridgePoolWalletAddress must identify the NFT custody wallet separately from token contracts.");
                var transfer = await SendNFTAsync(new SendWeb3NFTRequest
                {
                    TokenAddress = nftTokenAddress, TokenId = tokenId,
                    FromWalletAddress = senderAccountAddress, ToWalletAddress = _bridgePoolWalletAddress,
                    OwnerPrivateKey = senderPrivateKey, Amount = 1
                });
                if (transfer == null || transfer.IsError || string.IsNullOrWhiteSpace(transfer.Result?.TransactionResult))
                {
                    OASISErrorHandling.HandleError(ref result, transfer?.Message ?? "NFT custody transfer returned no confirmed receipt.", transfer?.Exception);
                    return result;
                }
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transfer.Result.TransactionResult,
                    IsSuccessful = true, Status = BridgeTransactionStatus.Completed
                };
                result.Message = "Original TRC721 NFT transfer into custody confirmed; cross-chain settlement is separate.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress))
                    throw new ArgumentException("NFT contract and explicit receiver address are required.");
                if (!System.Text.RegularExpressions.Regex.IsMatch(tokenId ?? "", "\\A[0-9]+\\z")
                    || !System.Numerics.BigInteger.TryParse(tokenId, out var nativeId)
                    || nativeId >= (System.Numerics.BigInteger.One << 256))
                    throw new ArgumentException("NFT token ID must be an explicit unsigned uint256 decimal value.");
                if (!string.IsNullOrWhiteSpace(sourceTransactionHash))
                    throw new ArgumentException("Receipt-bound NFT release requires source-chain proof validation; this native custody operation cannot ignore a supplied receipt.");
                if (string.IsNullOrWhiteSpace(_bridgePoolWalletAddress))
                    throw new InvalidOperationException("BridgePoolWalletAddress must identify the NFT custody wallet separately from token contracts.");
                var hash = await ExecuteNativeRuntimeAsync(async client =>
                {
                    if (_nativeSigningKeyResolver == null)
                        throw new InvalidOperationException("NFT custody signing credentials are not configured.");
                    var signingKey = await _nativeSigningKeyResolver(_bridgePoolWalletAddress);
                    return await client.SendNftAsync(_bridgePoolWalletAddress, receiverAccountAddress,
                        nftTokenAddress, nativeId, signingKey);
                });

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = hash,
                    IsSuccessful = true,
                    Status = BridgeTransactionStatus.Completed
                };
                result.Message = "Original TRC721 NFT transfer out of custody confirmed; coordinator authorization and cross-chain settlement are separate.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
            }
            return result;
        }



    }
}
