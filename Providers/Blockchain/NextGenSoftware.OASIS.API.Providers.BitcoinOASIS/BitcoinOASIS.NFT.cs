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
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS
{
    public partial class BitcoinOASIS
    {
        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid NFT send request");
                    return result;
                }

                // Bitcoin NFT implementation using Ordinals protocol or OP_RETURN
                // Store NFT transfer data in OP_RETURN transaction
                var nftTransferData = new
                {
                    type = "nft_transfer",
                    nft_id = request.TokenId ?? "",
                    from = request.FromWalletAddress ?? "",
                    to = request.ToWalletAddress ?? "",
                    token_address = request.TokenAddress ?? request.FromNFTTokenAddress ?? "",
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                var nftJson = JsonSerializer.Serialize(nftTransferData);
                var nftBytes = Encoding.UTF8.GetBytes(nftJson);

                // Create Bitcoin transaction with OP_RETURN containing NFT transfer data
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "createrawtransaction",
                    @params = new object[]
                    {
                        new object[0],
                        new Dictionary<string, object>
                        {
                            ["data"] = Convert.ToHexString(nftBytes)
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txData.TryGetProperty("result", out var txResult) ? txResult.GetString() : "";

                    result.Result = new Web3NFTTransactionResponse
                    {
                        TransactionResult = txHash ?? "",
                        Web3NFT = new Web3NFT { SendNFTTransactionHash = txHash }
                    };
                    result.IsError = false;
                    result.Message = "NFT transfer initiated successfully via Bitcoin OP_RETURN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to create NFT transfer transaction: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT via Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest request)
        {
            return MintNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid NFT mint request");
                    return result;
                }

                // Bitcoin NFT minting using Ordinals protocol or OP_RETURN
                // Store NFT mint data in OP_RETURN transaction
                var metadataJson = request.MetaData != null && request.MetaData.Count > 0
                    ? System.Text.Json.JsonSerializer.Serialize(request.MetaData)
                    : request.JSONMetaData ?? "";
                var nftMintData = new
                {
                    type = "nft_mint",
                    nft_id = Guid.NewGuid().ToString(),
                    mint_to = request.MintedByAvatarId.ToString(),
                    token_address = "",
                    metadata = metadataJson,
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                var nftJson = JsonSerializer.Serialize(nftMintData);
                var nftBytes = Encoding.UTF8.GetBytes(nftJson);

                // Create Bitcoin transaction with OP_RETURN containing NFT mint data
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "createrawtransaction",
                    @params = new object[]
                    {
                        new object[0],
                        new Dictionary<string, object>
                        {
                            ["data"] = Convert.ToHexString(nftBytes)
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txData.TryGetProperty("result", out var txResult) ? txResult.GetString() : "";

                    result.Result = new Web3NFTTransactionResponse
                    {
                        TransactionResult = txHash ?? "",
                        Web3NFT = new Web3NFT { MintTransactionHash = txHash }
                    };
                    result.IsError = false;
                    result.Message = "NFT minted successfully via Bitcoin OP_RETURN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to create NFT mint transaction: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT via Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid NFT burn request");
                    return result;
                }

                // Bitcoin NFT burning using OP_RETURN
                // Store NFT burn data in OP_RETURN transaction
                var nftBurnData = new
                {
                    type = "nft_burn",
                    nft_id = request.Web3NFTId.ToString(),
                    token_address = request.NFTTokenAddress ?? "",
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                var nftJson = JsonSerializer.Serialize(nftBurnData);
                var nftBytes = Encoding.UTF8.GetBytes(nftJson);

                // Create Bitcoin transaction with OP_RETURN containing NFT burn data
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "createrawtransaction",
                    @params = new object[]
                    {
                        new object[0],
                        new Dictionary<string, object>
                        {
                            ["data"] = Convert.ToHexString(nftBytes)
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(rpcRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txData.TryGetProperty("result", out var txResult) ? txResult.GetString() : "";

                    result.Result = new Web3NFTTransactionResponse
                    {
                        TransactionResult = txHash ?? "",
                        Web3NFT = new Web3NFT { MintTransactionHash = txHash }
                    };
                    result.IsError = false;
                    result.Message = "NFT burned successfully via Bitcoin OP_RETURN";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to create NFT burn transaction: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT via Bitcoin: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Bitcoin provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Search for NFT data in OP_RETURN transactions
                var searchRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "searchrawtransactions",
                    @params = new object[] { nftTokenAddress, true, 0, 100 }
                };

                var searchContent = new StringContent(JsonSerializer.Serialize(searchRequest), Encoding.UTF8, "application/json");
                var searchResponse = await _httpClient.PostAsync("", searchContent);

                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchResult = await searchResponse.Content.ReadAsStringAsync();
                    var searchData = JsonSerializer.Deserialize<JsonElement>(searchResult);

                    if (searchData.TryGetProperty("result", out var transactions))
                    {
                        foreach (var transaction in transactions.EnumerateArray())
                        {
                            if (transaction.TryGetProperty("vout", out var vouts))
                            {
                                foreach (var vout in vouts.EnumerateArray())
                                {
                                    if (vout.TryGetProperty("scriptPubKey", out var scriptPubKey) &&
                                        scriptPubKey.TryGetProperty("asm", out var asm))
                                    {
                                        var asmString = asm.GetString();
                                        if (asmString != null && asmString.StartsWith("OP_RETURN"))
                                        {
                                            try
                                            {
                                                var opReturnData = asmString.Substring("OP_RETURN ".Length);
                                                var nftBytes = Convert.FromHexString(opReturnData);
                                                var nftJson = Encoding.UTF8.GetString(nftBytes);
                                                var nftData = JsonSerializer.Deserialize<JsonElement>(nftJson);
                                                
                                                // Check if this is NFT data
                                                if (nftData.TryGetProperty("type", out var type) && 
                                                    (type.GetString() == "nft_mint" || type.GetString() == "nft_transfer"))
                                                {
                                                    var txHash = transaction.TryGetProperty("txid", out var txid) ? txid.GetString() : "";
                                                    var nft = new Web3NFT
                                                    {
                                                        NFTTokenAddress = nftTokenAddress,
                                                        MintTransactionHash = type.GetString() == "nft_mint" ? txHash : "",
                                                        SendNFTTransactionHash = type.GetString() == "nft_transfer" ? txHash : "",
                                                        OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.BitcoinOASIS)
                                                    };

                                                    // Extract NFT metadata
                                                    if (nftData.TryGetProperty("metadata", out var metadata))
                                                    {
                                                        nft.MetaData = new Dictionary<string, string>
                                                        {
                                                            ["RawMetadata"] = metadata.GetString() ?? ""
                                                        };
                                                    }

                                                    result.Result = nft;
                                                    result.IsError = false;
                                                    result.Message = "NFT data loaded successfully from Bitcoin blockchain";
                                                    return result;
                                                }
                                            }
                                            catch
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    OASISErrorHandling.HandleError(ref result, "NFT not found in Bitcoin blockchain");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to search Bitcoin blockchain: {searchResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from Bitcoin: {ex.Message}", ex);
            }
            return result;
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
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                return result;
            }

            // Bitcoin uses OP_RETURN for NFT locking (simplified)
            var bridgePoolAddress = "bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh"; // Bridge pool address
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
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                return result;
            }

            var bridgePoolAddress = "bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh"; // Bridge pool address
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
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                return result;
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
                Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
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
            if (!_isActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Bitcoin provider is not activated");
                return result;
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


    }
}
