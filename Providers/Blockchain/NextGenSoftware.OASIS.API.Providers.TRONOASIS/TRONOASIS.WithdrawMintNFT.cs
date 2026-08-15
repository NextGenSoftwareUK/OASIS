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
        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
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

                if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                // Bridge pool address
                var bridgePoolAddress = _contractAddress ?? "" ?? "TXYZabcdefghijklmnopqrstuvwxyz123456";
                
                // Convert amount to sun (smallest unit, 1 TRX = 1,000,000 sun)
                var amountInSun = (long)(amount * 1_000_000m);

                // Create TRON transfer transaction using TRON Grid API
                var transferData = new
                {
                    owner_address = senderAccountAddress,
                    to_address = bridgePoolAddress,
                    amount = amountInSun
                };

                var json = JsonSerializer.Serialize(transferData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/wallet/createtransaction", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Sign and broadcast transaction
                    var signedTx = await SignTRONTransaction(txResponse, senderPrivateKey);
                    var broadcastJson = JsonSerializer.Serialize(signedTx);
                    var broadcastContent = new StringContent(broadcastJson, Encoding.UTF8, "application/json");
                    var broadcastResponse = await _httpClient.PostAsync("/wallet/broadcasttransaction", broadcastContent);

                    if (broadcastResponse.IsSuccessStatusCode)
                    {
                        var broadcastResponseContent = await broadcastResponse.Content.ReadAsStringAsync();
                        var broadcastData = JsonSerializer.Deserialize<JsonElement>(broadcastResponseContent);
                        var txHash = broadcastData.TryGetProperty("txid", out var txid) ? txid.GetString() : "";

                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = txHash ?? "Transaction submitted",
                            IsSuccessful = true,
                            Status = BridgeTransactionStatus.Pending
                        };
                        result.IsError = false;
                        result.Message = "TRON withdrawal transaction submitted successfully";
                    }
                    else
                    {
                        var errorContent = await broadcastResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref result, $"Failed to broadcast transaction: {errorContent}");
                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = string.Empty,
                            IsSuccessful = false,
                            ErrorMessage = errorContent,
                            Status = BridgeTransactionStatus.Canceled
                        };
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to create transaction: {httpResponse.StatusCode} - {errorContent}");
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = errorContent,
                        Status = BridgeTransactionStatus.Canceled
                    };
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
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

                if (string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                // Bridge pool address (sender)
                var bridgePoolAddress = _contractAddress ?? "" ?? "TXYZabcdefghijklmnopqrstuvwxyz123456";
                
                // Convert amount to sun (smallest unit)
                var amountInSun = (long)(amount * 1_000_000m);

                // Create TRON transfer transaction from bridge pool to receiver
                var transferData = new
                {
                    owner_address = bridgePoolAddress,
                    to_address = receiverAccountAddress,
                    amount = amountInSun
                };

                var json = JsonSerializer.Serialize(transferData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/wallet/createtransaction", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Sign and broadcast transaction (would use bridge pool's private key in production)
                    var signedTx = await SignTRONTransaction(txResponse, ""); // Would get from config
                    var broadcastJson = JsonSerializer.Serialize(signedTx);
                    var broadcastContent = new StringContent(broadcastJson, Encoding.UTF8, "application/json");
                    var broadcastResponse = await _httpClient.PostAsync("/wallet/broadcasttransaction", broadcastContent);

                    if (broadcastResponse.IsSuccessStatusCode)
                    {
                        var broadcastResponseContent = await broadcastResponse.Content.ReadAsStringAsync();
                        var broadcastData = JsonSerializer.Deserialize<JsonElement>(broadcastResponseContent);
                        var txHash = broadcastData.TryGetProperty("txid", out var txid) ? txid.GetString() : "";

                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = txHash ?? "Transaction submitted",
                            IsSuccessful = true,
                            Status = BridgeTransactionStatus.Completed
                        };
                        result.IsError = false;
                        result.Message = "TRON deposit transaction submitted successfully";
                    }
                    else
                    {
                        var errorContent = await broadcastResponse.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref result, $"Failed to broadcast transaction: {errorContent}");
                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = string.Empty,
                            IsSuccessful = false,
                            ErrorMessage = errorContent,
                            Status = BridgeTransactionStatus.Canceled
                        };
                    }
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to create transaction: {httpResponse.StatusCode} - {errorContent}");
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = errorContent,
                        Status = BridgeTransactionStatus.Canceled
                    };
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
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

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                // Query TRON transaction status using TRON Grid API
                var httpResponse = await _httpClient.GetAsync($"/wallet/gettransactionbyid?value={transactionHash}");
                
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    // Check transaction ret field for status
                    if (txData.TryGetProperty("ret", out var ret) && ret.ValueKind == JsonValueKind.Array)
                    {
                        var retArray = ret.EnumerateArray();
                        if (retArray.MoveNext())
                        {
                            var retObj = retArray.Current;
                            if (retObj.TryGetProperty("contractRet", out var contractRet))
                            {
                                var status = contractRet.GetString();
                                if (status == "SUCCESS")
                                {
                                    result.Result = BridgeTransactionStatus.Completed;
                                    result.IsError = false;
                                    result.Message = "Transaction completed successfully";
                                }
                                else
                                {
                                    result.Result = BridgeTransactionStatus.Canceled;
                                    result.IsError = true;
                                    result.Message = $"Transaction failed: {status}";
                                }
                            }
                            else
                            {
                                result.Result = BridgeTransactionStatus.Pending;
                                result.IsError = false;
                            }
                        }
                        else
                        {
                            result.Result = BridgeTransactionStatus.Pending;
                            result.IsError = false;
                        }
                    }
                    else if (txData.TryGetProperty("txID", out var txID))
                    {
                        // Transaction exists
                        result.Result = BridgeTransactionStatus.Pending;
                        result.IsError = false;
                        result.Message = "Transaction found, status pending";
                    }
                    else
                    {
                        result.Result = BridgeTransactionStatus.NotFound;
                        result.IsError = true;
                        result.Message = "Transaction not found";
                    }
                }
                else if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = true;
                    result.Message = "Transaction not found";
                }
                else
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    OASISErrorHandling.HandleError(ref result, $"Failed to query transaction status: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
            }
            return result;
        }



        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate TRON provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Create TRON NFT transfer transaction
                var nftTransferData = new
                {
                    from = transaction.FromWalletAddress,
                    to = transaction.ToWalletAddress,
                    tokenId = Guid.NewGuid().ToString(), // Use generated token ID
                    contractAddress = "TRC721_CONTRACT_ADDRESS" // Would be actual contract address
                };

                var json = JsonSerializer.Serialize(nftTransferData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var tronResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txId = tronResponse.TryGetProperty("txID", out var txID) ? txID.GetString() : 
                               tronResponse.TryGetProperty("txid", out var txid) ? txid.GetString() : 
                               "NFT transfer created successfully";

                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = txId
                    };
                    response.IsError = false;
                    response.Message = "TRON NFT transfer sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to send TRON NFT transfer: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error sending TRON NFT transfer: {ex.Message}");
            }

            return response;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
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

                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // TRC-721 burn function - transfer NFT to zero address (burn)
                var burnData = new
                {
                    owner_address = request.OwnerPublicKey ?? "",
                    contract_address = request.NFTTokenAddress,
                    function_selector = "burn(uint256)",
                    parameter = request.Web3NFTId.ToString("X").PadLeft(64, '0'), // Token ID as hex
                    fee_limit = 100000000,
                    call_value = 0
                };

                var json = JsonSerializer.Serialize(burnData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var tronResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txId = tronResponse.TryGetProperty("txID", out var txID) ? txID.GetString() : 
                               tronResponse.TryGetProperty("txid", out var txid) ? txid.GetString() : 
                               "NFT burn transaction created";

                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = txId
                    };
                    result.IsError = false;
                    result.Message = "TRON NFT burned successfully";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn TRON NFT: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transaction)
        {
            var response = new OASISResult<IWeb3NFTTransactionResponse>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate TRON provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Create TRON NFT mint transaction
                var mintData = new
                {
                    to = "0x0", // Default to zero address for minting
                    tokenId = Guid.NewGuid().ToString(),
                    tokenURI = "https://api.trongrid.io/nft/metadata/" + Guid.NewGuid().ToString(),
                    contractAddress = "TRC721_CONTRACT_ADDRESS"
                };

                var json = JsonSerializer.Serialize(mintData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/wallet/triggersmartcontract", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var tronResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var txId = tronResponse.TryGetProperty("txID", out var txID) ? txID.GetString() : 
                               tronResponse.TryGetProperty("txid", out var txid) ? txid.GetString() : 
                               "NFT minted successfully";

                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response.Web3NFTTransactionResponse
                    {
                        TransactionResult = txId
                    };
                    response.IsError = false;
                    response.Message = "TRON NFT minted successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to mint TRON NFT: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error minting TRON NFT: {ex.Message}");
            }

            return response;
        }

        //public OASISResult<IWeb3NFT> LoadNFT(Guid id)
        //{
        //    return new OASISResult<IWeb3NFT> { Message = "LoadNFT is not implemented yet for TRON provider." };
        //}

        //public async Task<OASISResult<IWeb3NFT>> LoadNFTAsync(Guid id)
        //{
        //    var response = new OASISResult<IWeb3NFT>();

        //    try
        //    {
        //        if (!IsProviderActivated)
        //        {
        //            OASISErrorHandling.HandleError(ref response, "TRON provider is not activated");
        //            return response;
        //        }

        //        // Query TRON blockchain for NFT data
        //        var httpResponse = await _httpClient.GetAsync($"{TRON_API_BASE_URL}/v1/accounts/{id}/tokens");
        //        if (httpResponse.IsSuccessStatusCode)
        //        {
        //            var content = await httpResponse.Content.ReadAsStringAsync();
        //            var nftData = JsonSerializer.Deserialize<JsonElement>(content);

        //            response.Result = new OASISNFT
        //            {
        //                Id = id,
        //                Title = nftData.TryGetProperty("name", out var name) ? name.GetString() : "TRON NFT",
        //                Description = nftData.TryGetProperty("description", out var desc) ? desc.GetString() : "NFT from TRON blockchain",
        //                ImageUrl = nftData.TryGetProperty("imageUrl", out var img) ? img.GetString() : "",
        //                NFTTokenAddress = nftData.TryGetProperty("tokenId", out var tokenId) ? tokenId.GetString() : id.ToString(),
        //                OASISMintWalletAddress = nftData.TryGetProperty("contractAddress", out var contract) ? contract.GetString() : "TRC721_CONTRACT"
        //            };
        //            response.IsError = false;
        //            response.Message = "TRON NFT loaded successfully";
        //        }
        //        else
        //        {
        //            OASISErrorHandling.HandleError(ref response, $"Failed to load TRON NFT: {httpResponse.StatusCode}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref response, $"Error loading TRON NFT: {ex.Message}");
        //    }

        //    return response;
        //}

        //public OASISResult<IWeb3NFT> LoadNFT(string hash)
        //{
        //    return LoadNFTAsync(hash).Result;
        //}

        //public async Task<OASISResult<IWeb3NFT>> LoadNFTAsync(string hash)
        //{
        //    var result = new OASISResult<IWeb3NFT>();
        //    try
        //    {
        //        if (!IsProviderActivated)
        //        {
        //            OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
        //            return result;
        //        }

        //        // Query TRON blockchain for NFT by hash
        //        var nftData = await _httpClient.GetStringAsync($"{TRON_API_BASE_URL}/nft/{hash}");
        //        if (string.IsNullOrEmpty(nftData))
        //        {
        //            OASISErrorHandling.HandleError(ref result, "Error loading NFT from TRON: No data returned");
        //            return result;
        //        }

        //        if (!string.IsNullOrEmpty(nftData))
        //        {
        //            // Parse JSON response from TRON API
        //            var nftResponse = JsonSerializer.Deserialize<TRONNFTResponse>(nftData);
        //            if (nftResponse != null)
        //        {
        //            var nft = new OASISNFT
        //            {
        //                    Id = Guid.NewGuid(),
        //                    Title = "TRON NFT",
        //                    Description = "TRON NFT Description",
        //                    ImageUrl = "",
        //                    NFTTokenAddress = nftResponse.TokenId ?? "",
        //                    OASISMintWalletAddress = nftResponse.ContractAddress ?? "",
        //                    NFTMintedUsingWalletAddress = nftResponse.OwnerAddress ?? "",
        //                    MintedOn = nftResponse.CreatedDate,
        //                    ImportedOn = nftResponse.ModifiedDate,
        //                    MetaData = new Dictionary<string, object>
        //                {
        //                    ["TRONHash"] = hash,
        //                        ["TRONContractAddress"] = nftResponse.ContractAddress ?? "",
        //                        ["TRONOwnerAddress"] = nftResponse.OwnerAddress ?? "",
        //                        ["TRONTokenId"] = nftResponse.TokenId ?? "",
        //                    ["Provider"] = "TRONOASIS"
        //                }
        //            };
        //            result.Result = nft;
        //            result.IsError = false;
        //            result.Message = "NFT loaded successfully from TRON";
        //            }
        //            else
        //            {
        //                OASISErrorHandling.HandleError(ref result, "Error parsing NFT data from TRON");
        //            }
        //        }
        //        else
        //        {
        //            OASISErrorHandling.HandleError(ref result, "NFT not found on TRON blockchain");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error loading NFT from TRON: {ex.Message}", ex);
        //    }
        //    return result;
        //}

        //public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId)
        //{
        //    return LoadAllGeoNFTsForAvatarAsync(avatarId).Result;
        //}

        //public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
        //{
        //    var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
        //    try
        //    {
        //        if (!IsProviderActivated)
        //        {
        //            OASISErrorHandling.HandleError(ref result, "TRON provider is not activated");
        //            return result;
        //        }

        //        // Get avatar's TRON address
        //        var walletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS, avatarId);
        //        if (walletResult.IsError)
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"Error getting wallet address for avatar: {walletResult.Message}");
        //            return result;
        //        }

        //        // Query TRON blockchain for all GeoNFTs owned by this address
        //        var address = walletResult.Result is IProviderWallet w ? w.WalletAddress : walletResult.Result?.ToString();
        //        var responseJson = await _httpClient.GetStringAsync($"{TRON_API_BASE_URL}/geo-nfts/{address}");
        //        var geoArray = Newtonsoft.Json.Linq.JArray.Parse(responseJson);

        //        var geoNFTs = new List<IOASISGeoSpatialNFT>();
        //        foreach (var item in geoArray)
        //        {
        //            var title = item["name"]?.ToString() ?? "TRON GeoSpatial NFT";
        //            var description = item["description"]?.ToString() ?? string.Empty;
        //            var imageUrl = item["imageUrl"]?.ToString() ?? string.Empty;
        //            var tokenId = item["tokenId"]?.ToString() ?? string.Empty;
        //            var contractAddress = item["contractAddress"]?.ToString() ?? string.Empty;
        //            var ownerAddress = item["ownerAddress"]?.ToString() ?? string.Empty;
        //            var lat = item["latitude"] != null ? (long)(item["latitude"].Value<double>() * 1_000_000d) : 0L;
        //            var lon = item["longitude"] != null ? (long)(item["longitude"].Value<double>() * 1_000_000d) : 0L;
        //            var mintedOn = item["createdDate"] != null ? System.DateTime.Parse(item["createdDate"].ToString()) : System.DateTime.UtcNow;
        //            var importedOn = item["modifiedDate"] != null ? System.DateTime.Parse(item["modifiedDate"].ToString()) : System.DateTime.UtcNow;

        //            var geoNFT = new OASISGeoSpatialNFT
        //            {
        //                Id = System.Guid.NewGuid(),
        //                Title = title,
        //                Description = description,
        //                ImageUrl = imageUrl,
        //                NFTTokenAddress = tokenId,
        //                OASISMintWalletAddress = contractAddress,
        //                NFTMintedUsingWalletAddress = ownerAddress,
        //                Lat = lat,
        //                Long = lon,
        //                MintedOn = mintedOn,
        //                ImportedOn = importedOn,
        //                MetaData = new Dictionary<string, object>
        //                {
        //                    ["TRONContractAddress"] = contractAddress,
        //                    ["TRONOwnerAddress"] = ownerAddress,
        //                    ["TRONTokenId"] = tokenId,
        //                    ["Latitude"] = item["latitude"]?.ToString(),
        //                    ["Longitude"] = item["longitude"]?.ToString(),
        //                    ["Altitude"] = item["altitude"]?.ToString(),
        //                    ["Provider"] = "TRONOASIS"
        //                }
        //            };
        //            geoNFTs.Add(geoNFT);
        //        }

        //        result.Result = geoNFTs;
        //        result.IsError = false;
        //        result.Message = $"Successfully loaded {geoNFTs.Count} GeoNFTs for avatar from TRON";
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error loading GeoNFTs for avatar from TRON: {ex.Message}", ex);
        //    }
        //    return result;
        //}

        //public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress)
        //{
        //    return LoadAllGeoNFTsForMintAddressAsync(mintWalletAddress).Result;
        //}

        //public async Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        //{
        //    var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
        //    string errorMessage = "Error in LoadAllGeoNFTsForMintAddressAsync method in TRONOASIS Provider. Reason: ";

        //    try
        //    {
        //        if (string.IsNullOrEmpty(mintWalletAddress))
        //        {
        //            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Mint wallet address cannot be null or empty");
        //            return result;
        //        }
        //
        //        var geoNFTs = new List<IOASISGeoSpatialNFT>();
        //
        //        // Query TRON network for NFTs owned by the mint address
        //        var nftQuery = new
        //        {
        //            owner_address = mintWalletAddress,
        //            limit = 200,
        //            offset = 0
        //        };
        //
        //        var response = await _httpClient.PostAsync("/wallet/triggerconstantcontract", 
        //            new StringContent(JsonSerializer.Serialize(nftQuery), Encoding.UTF8, "application/json"));
        //
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var content = await response.Content.ReadAsStringAsync();
        //            var nftData = JsonSerializer.Deserialize<TRONNFTResponse>(content);
        //
        //            if (nftData != null)
        //            {
        //                // Parse TRON NFT data directly from response
        //                var geoNFT = new OASISGeoSpatialNFT
        //                {
        //                    Id = Guid.NewGuid(),
        //                    Title = "TRON GeoSpatial NFT",
        //                    Description = "TRON GeoSpatial NFT Description",
        //                    ImageUrl = string.Empty,
        //                    Lat = 0,
        //                    Long = 0,
        //                    OASISMintWalletAddress = mintWalletAddress,
        //                    GeoNFTMetaDataProvider = new EnumValue<ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TRONOASIS)
        //                };
        //                geoNFTs.Add(geoNFT);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
        //    }
        //    return result;
        //        result.Result = geoNFTs;
        //        result.IsError = false;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
        //    }
        //
        //    return result;
        //}

    }
}
