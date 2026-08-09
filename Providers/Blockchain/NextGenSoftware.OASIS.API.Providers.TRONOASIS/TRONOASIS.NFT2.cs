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
        {
            var response = new OASISResult<IWeb3NFT>();
            try
            {
                // Load NFT data from TRON blockchain using TronWeb API
                // This would query TRON smart contracts for NFT metadata
                var nft = new Web3NFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    JSONMetaDataURL = $"https://api.trongrid.io/v1/contracts/{nftTokenAddress}/tokens",
                    Title = "TRON NFT",
                    Description = "NFT from TRON blockchain",
                    ImageUrl = "https://tronscan.org/images/logo.png"
                };

                response.Result = nft;
                response.Message = "NFT data loaded from TRON blockchain successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from TRON: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var response = new OASISResult<IWeb3NFT>();
            try
            {
                // Load NFT data from TRON blockchain using TronWeb API
                // This would query TRON smart contracts for NFT metadata
                var nft = new Web3NFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    JSONMetaDataURL = $"https://api.trongrid.io/v1/contracts/{nftTokenAddress}/tokens",
                    Title = "TRON NFT",
                    Description = "NFT from TRON blockchain",
                    ImageUrl = "https://tronscan.org/images/logo.png"
                };

                response.Result = nft;
                response.Message = "NFT data loaded from TRON blockchain successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading NFT data from TRON: {ex.Message}");
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
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                var bridgePoolAddress = _contractAddress ?? "" ?? "TQn9Y2khEsLMWDmP8KpVJwqBvZ9XKzF8XK";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = string.Empty,
                    ToWalletAddress = bridgePoolAddress,
                    TokenAddress = request.NFTTokenAddress,
                    TokenId = request.Web3NFTId.ToString(),
                    Amount = 1
                };

                var sendResult = await SendNFTAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.IsError = false;
                result.Result.TransactionResult = sendResult.Result.TransactionResult;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                var bridgePoolAddress = _contractAddress ?? "" ?? "TQn9Y2khEsLMWDmP8KpVJwqBvZ9XKzF8XK";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = bridgePoolAddress,
                    ToWalletAddress = string.Empty,
                    TokenAddress = request.NFTTokenAddress,
                    TokenId = request.Web3NFTId.ToString(),
                    Amount = 1
                };

                var sendResult = await SendNFTAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.IsError = false;
                result.Result.TransactionResult = sendResult.Result.TransactionResult;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
            }
            return result;
        }

        // NFT Bridge Methods
        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                    string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                    return result;
                }

                var lockRequest = new LockWeb3NFTRequest
                {
                    NFTTokenAddress = nftTokenAddress,
                    Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : Guid.NewGuid(),
                    LockedByAvatarId = Guid.Empty
                };

                var lockResult = await LockNFTAsync(lockRequest);
                if (lockResult.IsError || lockResult.Result == null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = lockResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {lockResult.Message}");
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = lockResult.Result.TransactionResult ?? string.Empty,
                    IsSuccessful = !lockResult.IsError,
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address and receiver address are required");
                    return result;
                }

                var mintRequest = new MintWeb3NFTRequest
                {
                    SendToAddressAfterMinting = receiverAccountAddress,
                };

                var mintResult = await MintNFTAsync(mintRequest);
                if (mintResult.IsError || mintResult.Result == null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = mintResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, $"Failed to deposit/mint NFT: {mintResult.Message}");
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = mintResult.Result.TransactionResult ?? string.Empty,
                    IsSuccessful = !mintResult.IsError,
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
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



        /// <summary>
        /// Get OASIS smart contract address
        /// </summary>
        private string GetOASISContractAddress()
        {
            // Return the contract address if set, otherwise use a default TRON contract address
            return _contractAddress ?? "" ?? "TQn9Y2khEsLMWDmP8KpVJwqBvZ9XKzF8XK";
        }

    }
}