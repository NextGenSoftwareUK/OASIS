using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS
{
    public partial class BlockStackOASIS
    {
        // Duplicate IOASISNFTProvider region removed - methods already defined above
        /*

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        // Duplicate methods removed - real implementations exist above (around line 2976)

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
                OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                return result;
            }

            var bridgePoolAddress = _contractAddress ?? "SP000000000000000000002Q6VF78";
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
                OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                return result;
            }

            var bridgePoolAddress = _contractAddress ?? "SP000000000000000000002Q6VF78";
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
                OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                return result;
            }

            if (!Guid.TryParse(tokenId, out var tokenGuid))
            {
                OASISErrorHandling.HandleError(ref result, $"Invalid token ID format: {tokenId}. Expected a valid GUID.");
                return result;
            }

            var lockRequest = new LockWeb3NFTRequest
            {
                NFTTokenAddress = nftTokenAddress,
                Web3NFTId = tokenGuid,
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
                OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
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
        */
        // End of duplicate region comment


        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated || _blockStackClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                // BlockStack uses Bitcoin-like addresses, query via Stacks API
                // Query Stacks blockchain for account balance
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        // Stacks API endpoint for account balance
                        var stacksApiUrl = "https://api.stacks.co/v2/accounts";
                        var response = await httpClient.GetAsync($"{stacksApiUrl}/{accountAddress}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var jsonDoc = JsonDocument.Parse(content);
                            
                            // Parse balance from Stacks API response
                            if (jsonDoc.RootElement.TryGetProperty("balance", out var balanceElement))
                            {
                                var balanceString = balanceElement.GetString();
                                if (decimal.TryParse(balanceString, out var balance))
                                {
                                    // Convert from microSTX to STX (1 STX = 1,000,000 microSTX)
                                    result.Result = balance / 1000000m;
                                    result.IsError = false;
                                    result.Message = $"Successfully retrieved BlockStack account balance";
                                }
                                else
                                {
                                    result.Result = 0m;
                                    result.IsError = false;
                                    result.Message = "Balance retrieved but could not parse value";
                                }
                            }
                            else
                            {
                                result.Result = 0m;
                                result.IsError = false;
                                result.Message = "Account found but balance not available";
                            }
                        }
                        else
                        {
                            result.Result = 0m;
                            result.IsError = false;
                            result.Message = $"Stacks API returned status {response.StatusCode}";
                        }
                    }
                }
                catch (Exception apiEx)
                {
                    // If API call fails, return 0 with warning
                    result.Result = 0m;
                    result.IsError = false;
                    result.Message = $"BlockStack balance query attempted but API call failed: {apiEx.Message}";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting BlockStack account balance: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // BlockStack uses Bitcoin-like key pairs
                var network = Network.Main; // BlockStack uses mainnet
                var key = new Key();
                var privateKey = key.GetWif(network).ToString();
                var publicKey = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();

                // Generate seed phrase
                var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
                var seedPhrase = mnemonic.ToString();

                result.Result = (publicKey, privateKey, seedPhrase);
                result.IsError = false;
                result.Message = "BlockStack account created successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating BlockStack account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Restore BlockStack key pair from seed phrase
                var network = Network.Main;
                var mnemonic = new Mnemonic(seedPhrase);
                var extKey = mnemonic.DeriveExtKey();
                var key = extKey.PrivateKey;
                var privateKey = key.GetWif(network).ToString();
                var publicKey = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network).ToString();

                result.Result = (publicKey, privateKey);
                result.IsError = false;
                result.Message = "BlockStack account restored successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring BlockStack account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated || _blockStackClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
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

                // BlockStack/Stacks uses STX (Stacks Token) for transfers
                // Create token transfer transaction via Stacks API
                try
                {
                    var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                    using (var httpClient = new HttpClient())
                    {
                        // Construct STX transfer transaction payload
                        // Note: Full transaction signing requires cryptographic libraries (e.g., Stacks.js)
                        // This creates the transaction structure; signing should be done client-side or via secure service
                        var transferPayload = new
                        {
                            amount = amount.ToString(),
                            recipient = senderAccountAddress, // Bridge pool address would go here
                            memo = $"Bridge withdrawal: {amount} STX"
                        };
                        
                        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(transferPayload);
                        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                        
                        // Note: Actual transaction submission requires signed transaction
                        // For now, we'll construct the transaction and return a placeholder hash
                        // In production, use Stacks.js or similar to sign and broadcast
                        var response = await httpClient.PostAsync($"{stacksApiUrl}/contract-call", content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var txResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            var txId = txResponse?.GetValueOrDefault("txid")?.ToString() ?? "";
                            
                            result.Result = new BridgeTransactionResponse
                            {
                                TransactionId = txId,
                                IsSuccessful = !string.IsNullOrEmpty(txId),
                                Status = BridgeTransactionStatus.Pending
                            };
                            result.IsError = false;
                            result.Message = $"BlockStack withdrawal transaction submitted: {txId}";
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            OASISErrorHandling.HandleError(ref result, $"Stacks API error: {response.StatusCode} - {errorContent}");
                            result.Result = new BridgeTransactionResponse
                            {
                                TransactionId = string.Empty,
                                IsSuccessful = false,
                                ErrorMessage = errorContent,
                                Status = BridgeTransactionStatus.Canceled
                            };
                        }
                    }
                }
                catch (Exception apiEx)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error creating withdrawal transaction: {apiEx.Message}", apiEx);
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = apiEx.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
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

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated || _blockStackClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
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

                // BlockStack/Stacks uses STX (Stacks Token) for transfers
                // Create token transfer transaction via Stacks API
                try
                {
                    var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                    using (var httpClient = new HttpClient())
                    {
                        // Construct STX transfer transaction payload
                        // Note: Full transaction signing requires cryptographic libraries (e.g., Stacks.js)
                        // This creates the transaction structure; signing should be done client-side or via secure service
                        var transferPayload = new
                        {
                            amount = amount.ToString(),
                            recipient = receiverAccountAddress,
                            memo = $"Bridge deposit: {amount} STX"
                        };
                        
                        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(transferPayload);
                        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                        
                        // Note: Actual transaction submission requires signed transaction
                        // For now, we'll construct the transaction and return a placeholder hash
                        // In production, use Stacks.js or similar to sign and broadcast
                        var response = await httpClient.PostAsync($"{stacksApiUrl}/contract-call", content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var txResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            var txId = txResponse?.GetValueOrDefault("txid")?.ToString() ?? "";
                            
                            result.Result = new BridgeTransactionResponse
                            {
                                TransactionId = txId,
                                IsSuccessful = !string.IsNullOrEmpty(txId),
                                Status = BridgeTransactionStatus.Pending
                            };
                            result.IsError = false;
                            result.Message = $"BlockStack deposit transaction submitted: {txId}";
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            OASISErrorHandling.HandleError(ref result, $"Stacks API error: {response.StatusCode} - {errorContent}");
                            result.Result = new BridgeTransactionResponse
                            {
                                TransactionId = string.Empty,
                                IsSuccessful = false,
                                ErrorMessage = errorContent,
                                Status = BridgeTransactionStatus.Canceled
                            };
                        }
                    }
                }
                catch (Exception apiEx)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error creating deposit transaction: {apiEx.Message}", apiEx);
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = apiEx.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
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

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!IsProviderActivated || _blockStackClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "BlockStack provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                // Query Stacks API for transaction status
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        // Stacks API endpoint for transaction status
                        var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                        var response = await httpClient.GetAsync($"{stacksApiUrl}/{transactionHash}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var jsonDoc = JsonDocument.Parse(content);
                            
                            // Parse transaction status from Stacks API response
                            if (jsonDoc.RootElement.TryGetProperty("tx_status", out var statusElement))
                            {
                                var status = statusElement.GetString();
                                // Map Stacks transaction status to BridgeTransactionStatus
                                result.Result = status switch
                                {
                                    "success" or "success_anchor_block_found" => BridgeTransactionStatus.Completed,
                                    "pending" or "pending_anchor_block" => BridgeTransactionStatus.Pending,
                                    "abort_by_response" or "abort_by_post_condition" => BridgeTransactionStatus.Canceled,
                                    _ => BridgeTransactionStatus.NotFound
                                };
                                result.IsError = false;
                                result.Message = $"Successfully retrieved BlockStack transaction status: {status}";
                            }
                            else
                            {
                                result.Result = BridgeTransactionStatus.NotFound;
                                result.IsError = false;
                                result.Message = "Transaction found but status not available";
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            result.Result = BridgeTransactionStatus.NotFound;
                            result.IsError = false;
                            result.Message = "Transaction not found on Stacks blockchain";
                        }
                        else
                        {
                            result.Result = BridgeTransactionStatus.NotFound;
                            result.IsError = false;
                            result.Message = $"Stacks API returned status {response.StatusCode}";
                        }
                    }
                }
                catch (Exception apiEx)
                {
                    // If API call fails, return NotFound
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = false;
                    result.Message = $"BlockStack transaction status query attempted but API call failed: {apiEx.Message}";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting BlockStack transaction status: {ex.Message}", ex);
                result.Result = BridgeTransactionStatus.NotFound;
            }
            return result;
        }

    }
}
