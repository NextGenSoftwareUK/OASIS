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
        /// <summary>Queries one explicit enumerable TRC721 collection. This is not cross-collection discovery.</summary>
        public async Task<OASISResult<List<IWeb3NFT>>> LoadNFTsForCollectionOwnerAsync(string collectionAddress, string ownerAddress)
        {
            var result = new OASISResult<List<IWeb3NFT>>();
            try
            {
                if (string.IsNullOrWhiteSpace(collectionAddress) || string.IsNullOrWhiteSpace(ownerAddress))
                    throw new ArgumentException("Explicit collection and owner addresses are required.");
                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError) throw new InvalidOperationException(activation.Message);
                }
                result.Result = await ExecuteRuntimeAsync(async client =>
                {
                    var ids = await client.GetNftIdsForOwnerAsync(collectionAddress, ownerAddress);
                    var items = new List<IWeb3NFT>();
                    foreach (var id in ids)
                        items.Add(new Web3NFT
                        {
                            NFTTokenAddress = collectionAddress + "/" + id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            JSONMetaDataURL = await client.GetNftMetadataUriAsync(collectionAddress, id),
                            OnChainProvider = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS)
                        });
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
            string errorMessage = "Error in LoadAllNFTsForAvatarAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (avatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar ID cannot be empty");
                    return result;
                }

                // Get wallet address for the avatar
                var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, avatarId);
                if (walletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Failed to get wallet address: {walletResult.Message}");
                    return result;
                }

                var nfts = new List<IWeb3NFT>();

                // Query TRON network for NFTs owned by the avatar's wallet
                var nftQuery = new
                {
                    owner_address = walletResult.Result,
                    limit = 200,
                    offset = 0
                };

                var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
                    new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var nftArray = Newtonsoft.Json.Linq.JArray.Parse(content);
                    if (nftArray != null)
                    {
                        foreach (var nft in nftArray)
                        {
                            var oasisNFT = new Web3NFT
                            {
                                Id = Guid.NewGuid(),
                                Title = nft["name"]?.ToString() ?? "TRON NFT",
                                Description = nft["description"]?.ToString() ?? string.Empty,
                                ImageUrl = nft["imageUrl"]?.ToString() ?? string.Empty,
                                NFTTokenAddress = nft["tokenId"]?.ToString() ?? string.Empty,
                                OASISMintWalletAddress = walletResult.Result?.ToString(),
                                OnChainProvider = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS)
                            };
                            nfts.Add(oasisNFT);
                        }
                    }
                }

                result.Result = nfts;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<List<IWeb3NFT>> LoadAllNFTsForMintAddress(string mintWalletAddress)
        {
            return LoadAllNFTsForMintAddressAsync(mintWalletAddress).Result;
        }

        public async Task<OASISResult<List<IWeb3NFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
        {
            var result = new OASISResult<List<IWeb3NFT>>();
            string errorMessage = "Error in LoadAllNFTsForMintAddressAsync method in TRONOASIS Provider. Reason: ";

            try
            {
                if (string.IsNullOrEmpty(mintWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mint wallet address cannot be null or empty");
                    return result;
                }

                var nfts = new List<IWeb3NFT>();

                // Query TRON network for NFTs owned by the mint address
                var nftQuery = new
                {
                    owner_address = mintWalletAddress,
                    limit = 200,
                    offset = 0
                };

                var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
                    new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var nftArray = Newtonsoft.Json.Linq.JArray.Parse(content);
                    if (nftArray != null)
                    {
                        foreach (var nft in nftArray)
                        {
                            var oasisNFT = new Web3NFT
                            {
                                Id = Guid.NewGuid(),
                                Title = nft["name"]?.ToString() ?? "TRON NFT",
                                Description = nft["description"]?.ToString() ?? string.Empty,
                                ImageUrl = nft["imageUrl"]?.ToString() ?? string.Empty,
                                NFTTokenAddress = nft["tokenId"]?.ToString() ?? string.Empty,
                                OASISMintWalletAddress = mintWalletAddress,
                                OnChainProvider = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS)
                            };
                            nfts.Add(oasisNFT);
                        }
                    }
                }

                result.Result = nfts;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
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
                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError) throw new InvalidOperationException(activation.Message);
                }
                var uri = await ExecuteRuntimeAsync(client => client.GetNftMetadataUriAsync(asset.Contract, asset.TokenId));
                response.Result = new Web3NFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    JSONMetaDataURL = uri,
                    OnChainProvider = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS)
                };
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
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (avatarId == Guid.Empty) throw new ArgumentException("An avatar ID is required for NFT depositor signing.");
                TronNativeTransferClient.ParseNftAddress(nftAddress);
                if (string.IsNullOrWhiteSpace(_escrowContractAddress))
                    throw new InvalidOperationException("A separate EscrowContractAddress with NFT escrow support must be configured.");
                var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(avatarId,
                    Core.Enums.ProviderType.TRONOASIS, showPrivateKeys: true);
                if (wallet.IsError || wallet.Result == null || string.IsNullOrWhiteSpace(wallet.Result.PrivateKey))
                    throw new InvalidOperationException("Could not resolve the avatar's TRON depositor signing wallet.");
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                var hash = await ExecuteRuntimeAsync(client => client.ExecuteNftEscrowAsync(wallet.Result.WalletAddress,
                    _escrowContractAddress, nftAddress, wallet.Result.PrivateKey, unlock));
                result.Result.TransactionResult = hash;
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                var hash = await ExecuteRuntimeAsync(async client =>
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
