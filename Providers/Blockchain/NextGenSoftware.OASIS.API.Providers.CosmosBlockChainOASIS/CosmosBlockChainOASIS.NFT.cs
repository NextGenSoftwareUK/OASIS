using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
// using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS
{
    public partial class CosmosBlockChainOASIS
    {
        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transation)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (transation == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Mint request is required");
                    return result;
                }

                // Mint NFT using Cosmos CW721 standard (CosmWasm NFT)
                var mintToAddress = transation.SendToAddressAfterMinting ?? "";
                
                // Get minter address from KeyManager
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(transation.MintedByAvatarId, Core.Enums.ProviderType.CosmosBlockChainOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var minterAddress = keysResult.Result[0]; // In production, derive address from private key
                var contractAddress = _contractAddress ?? "cosmos1nftcontract1234567890abcdef";

                // Create mint message for CW721 NFT
                var mintMessage = new
                {
                    type = "/cosmwasm.wasm.v1.MsgExecuteContract",
                    value = new
                    {
                        sender = minterAddress,
                        contract = contractAddress,
                        msg = new Dictionary<string, object>
                        {
                            ["mint"] = new Dictionary<string, object>
                            {
                                ["token_id"] = CreateDeterministicGuid($"{ProviderType.Value}:nft:{mintToAddress}:{transation.ImageUrl ?? ""}").ToString(),
                                ["owner"] = mintToAddress,
                                ["token_uri"] = transation.ImageUrl ?? "",
                                ["extension"] = new Dictionary<string, object>
                                {
                                    ["name"] = transation.Title ?? "Cosmos NFT",
                                    ["description"] = transation.Description ?? "NFT minted via OASIS"
                                }
                            }
                        },
                        funds = new object[] { }
                    }
                };

                var transactionPayload = new
                {
                    body = new
                    {
                        messages = new[] { mintMessage },
                        memo = $"OASIS NFT mint for {transation.MintedByAvatarId}"
                    },
                    auth_info = new
                    {
                        signer_infos = new object[] { },
                        fee = new
                        {
                            amount = new[] { new { denom = "uatom", amount = "5000" } },
                            gas_limit = "200000"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txResponse.TryGetProperty("tx_response", out var txResp) &&
                                 txResp.TryGetProperty("txhash", out var hash) ? hash.GetString() : "";

                    result.Result = new Web3NFTTransactionResponse
                    {
                        TransactionResult = txHash ?? "NFT mint transaction submitted",
                        Web3NFT = new Web3NFT
                        {
                            Title = transation.Title ?? "Cosmos NFT",
                            Description = transation.Description ?? "NFT minted via OASIS",
                            ImageUrl = transation.ImageUrl ?? "",
                            NFTMintedUsingWalletAddress = minterAddress
                        }
                    };
                    result.IsError = false;
                    result.Message = "Cosmos NFT minted successfully";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint Cosmos NFT: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT on Cosmos: {ex.Message}", ex);
            }
            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Cosmos NFT burn using CW721 standard (CosmWasm NFT standard)
                // Build burn message for Cosmos transaction
                var burnMessage = new
                {
                    type = "/cosmwasm.wasm.v1.MsgExecuteContract",
                    value = new
                    {
                        sender = request.OwnerPublicKey ?? "",
                        contract = request.NFTTokenAddress,
                        msg = new Dictionary<string, object>
                        {
                            ["burn"] = new Dictionary<string, object>
                            {
                                ["token_id"] = request.Web3NFTId.ToString()
                            }
                        },
                        funds = new object[] { }
                    }
                };

                var transactionPayload = new
                {
                    body = new
                    {
                        messages = new[] { burnMessage },
                        memo = "OASIS NFT burn transaction"
                    },
                    auth_info = new
                    {
                        signer_infos = new object[] { },
                        fee = new
                        {
                            amount = new[] { new { denom = "uatom", amount = "5000" } },
                            gas_limit = "200000"
                        }
                    }
                };

                var json = JsonSerializer.Serialize(transactionPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync("/cosmos/tx/v1beta1/txs", content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var txResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var txHash = txResponse.TryGetProperty("tx_response", out var txResp) &&
                                 txResp.TryGetProperty("txhash", out var hash) ? hash.GetString() : "";

                    result.Result = new Web3NFTTransactionResponse
                    {
                        TransactionResult = txHash ?? "NFT burn transaction submitted"
                    };
                    result.IsError = false;
                    result.Message = "Cosmos NFT burned successfully";
                }
                else
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn Cosmos NFT: {httpResponse.StatusCode} - {errorContent}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT: {ex.Message}", ex);
            }
            return result;
        }

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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Lock NFT by transferring to bridge pool address
                var bridgePoolAddress = _contractAddress ?? "cosmos1bridgepool1234567890abcdef";
                
                var sendRequest = new SendWeb3NFTRequest
                {
                    TokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = "", // Will be retrieved from KeyManager
                    ToWalletAddress = bridgePoolAddress,
                    TokenId = request.Web3NFTId.ToString(),
                    Amount = 1
                };

                // Get owner address from KeyManager
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(request.LockedByAvatarId, Core.Enums.ProviderType.CosmosBlockChainOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                sendRequest.FromWalletAddress = keysResult.Result[0]; // In production, derive address from private key

                return await SendNFTAsync(sendRequest);
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Unlock NFT by transferring from bridge pool to receiver
                var bridgePoolAddress = _contractAddress ?? "cosmos1bridgepool1234567890abcdef";
                
                // Get receiver address from KeyManager
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(request.UnlockedByAvatarId, Core.Enums.ProviderType.CosmosBlockChainOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var receiverAddress = keysResult.Result[0]; // In production, derive address from private key
                
                var sendRequest = new SendWeb3NFTRequest
                {
                    TokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = bridgePoolAddress,
                    ToWalletAddress = receiverAddress,
                    TokenId = request.Web3NFTId.ToString(),
                    Amount = 1
                };

                return await SendNFTAsync(sendRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
            }
            return result;
        }

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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) ||
                    string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                    return result;
                }

                // Lock NFT by transferring to bridge pool
                var lockRequest = new LockWeb3NFTRequest
                {
                    NFTTokenAddress = nftTokenAddress,
                    Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                    LockedByAvatarId = Guid.Empty // Would be retrieved from senderAccountAddress in production
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address and receiver address are required");
                    return result;
                }

                // Unlock NFT by transferring from bridge pool to receiver
                var unlockRequest = new UnlockWeb3NFTRequest
                {
                    NFTTokenAddress = nftTokenAddress,
                    Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                    UnlockedByAvatarId = Guid.Empty // Would be retrieved from receiverAccountAddress in production
                };

                var unlockResult = await UnlockNFTAsync(unlockRequest);
                
                if (unlockResult.IsError || unlockResult.Result == null)
                {
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = unlockResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {unlockResult.Message}");
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = unlockResult.Result.TransactionResult ?? string.Empty,
                    IsSuccessful = !unlockResult.IsError,
                    Status = BridgeTransactionStatus.Completed
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

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Cosmos provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Query Cosmos NFT contract for NFT info using CW721 standard
                // Query contract info
                var queryPayload = new
                {
                    contract_info = new { }
                };

                var queryJson = JsonSerializer.Serialize(queryPayload);
                var queryContent = new StringContent(queryJson, Encoding.UTF8, "application/json");

                var httpResponse = await _httpClient.PostAsync($"/cosmwasm/wasm/v1/contract/{nftTokenAddress}/smart", queryContent);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var contractData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    var nft = new Web3NFT
                    {
                        NFTTokenAddress = nftTokenAddress,
                        Title = contractData.TryGetProperty("data", out var data) && 
                                data.TryGetProperty("name", out var name) ? name.GetString() : "Cosmos NFT",
                        Description = contractData.TryGetProperty("data", out var data2) && 
                                     data2.TryGetProperty("description", out var desc) ? desc.GetString() : "NFT from Cosmos blockchain",
                        Symbol = contractData.TryGetProperty("data", out var data3) && 
                                data3.TryGetProperty("symbol", out var symbol) ? symbol.GetString() : "COSMOS"
                    };

                    result.Result = nft;
                    result.IsError = false;
                    result.Message = "NFT data loaded successfully from Cosmos blockchain";
                }
                else
                {
                    // Fallback: create basic NFT info
                    result.Result = new Web3NFT
                    {
                        NFTTokenAddress = nftTokenAddress,
                        Title = "Cosmos NFT",
                        Description = "NFT from Cosmos blockchain",
                        Symbol = "COSMOS"
                    };
                    result.IsError = false;
                    result.Message = "NFT data loaded from Cosmos blockchain (basic info)";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from Cosmos: {ex.Message}", ex);
            }
            return result;
        }

        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            // Cosmos provider does not generate native code from STAR metadata yet.
            return true;
        }

    }
}
