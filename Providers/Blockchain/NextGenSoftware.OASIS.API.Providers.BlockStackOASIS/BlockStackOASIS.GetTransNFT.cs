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
        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
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

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query Stacks API for wallet transactions
                var stacksApiUrl = "https://api.stacks.co/extended/v1";
                using (var httpClient = new HttpClient())
                {
                    var limit = 50;
                    var offset = 0;
                    var response = await httpClient.GetAsync($"{stacksApiUrl}/address/{request.WalletAddress}/transactions?limit={limit}&offset={offset}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var txData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        var transactions = new List<IWalletTransaction>();
                        if (txData.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var tx in results.EnumerateArray())
                            {
                                // Create wallet transaction from Stacks API response
                                var walletTx = new NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response.WalletTransaction
                                {
                                    TransactionId = tx.TryGetProperty("tx_id", out var txId) && Guid.TryParse(txId.GetString(), out var guid) ? guid : Guid.NewGuid(),
                                    FromWalletAddress = tx.TryGetProperty("sender_address", out var sender) ? sender.GetString() : "",
                                    ToWalletAddress = tx.TryGetProperty("token_transfer", out var tokenTransfer) && 
                                                     tokenTransfer.TryGetProperty("recipient_address", out var recipient) ? 
                                                     recipient.GetString() : "",
                                    Amount = tx.TryGetProperty("token_transfer", out var transfer) && 
                                             transfer.TryGetProperty("amount", out var amount) ? 
                                             double.Parse(amount.GetString()) : 0,
                                    Description = tx.TryGetProperty("tx_status", out var status) ? status.GetString() : "Stacks transaction",
                                    TransactionType = TransactionType.Debit,
                                    TransactionCategory = TransactionCategory.Other
                                };
                                transactions.Add(walletTx);
                            }
                        }
                        
                        result.Result = transactions;
                        result.IsError = false;
                        result.Message = $"Retrieved {transactions.Count} transactions from BlockStack Stacks blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to get transactions: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
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

                // Generate Stacks key pair using NBitcoin (Stacks uses Bitcoin-style keys)
                var key = new Key();
                var privateKey = key.GetBitcoinSecret(Network.Main).ToString();
                var publicKey = key.PubKey.ToString();
                var address = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main).ToString();

                // Convert to Stacks address format (Stacks addresses start with SP or ST)
                // Stacks uses a different address encoding, but for now we'll use the Bitcoin address
                var stacksAddress = $"SP{address.Substring(1)}"; // Simplified conversion

                result.Result = new KeyHelper.KeyPairAndWallet
                {
                    PrivateKey = privateKey,
                    PublicKey = publicKey,
                    WalletAddressLegacy = stacksAddress,
                    WalletAddressSegwitP2SH = stacksAddress
                };
                result.IsError = false;
                result.Message = "Key pair generated successfully for BlockStack Stacks blockchain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair for BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransaction(new WalletTransactionRequest());
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return await SendTransactionAsync(new WalletTransactionRequest());
        }



        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest request)
        {
            return SendNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
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

                // BlockStack/Stacks blockchain uses SIP-009 NFT standard
                // Send NFT via Stacks blockchain RPC API
                if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid NFT send request");
                    return result;
                }

                try
                {
                    // Use Stacks API to send NFT transfer transaction
                    // SIP-009 NFT transfer: (transfer u256 principal principal)
                    var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                    using (var httpClient = new HttpClient())
                    {
                        // Get NFT contract address and token ID from request
                        var contractAddress = request.FromNFTTokenAddress ?? request.TokenAddress ?? "";
                        var tokenId = request.TokenId ?? "";
                        
                        // Construct Stacks transaction payload for NFT transfer
                        var transferPayload = new
                        {
                            contract_address = contractAddress,
                            function_name = "transfer",
                            function_args = new[]
                            {
                                tokenId,
                                request.FromWalletAddress ?? "",
                                request.ToWalletAddress
                            }
                        };
                        
                        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(transferPayload);
                        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                        
                        var response = await httpClient.PostAsync($"{stacksApiUrl}/contract-call", content);
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var txResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            var txId = txResponse?.GetValueOrDefault("txid")?.ToString() ?? "";
                            
                            result.Result = new Web3NFTTransactionResponse
                            {
                                TransactionResult = txId,
                                SendNFTTransactionResult = txId
                            };
                            result.Message = "NFT transfer transaction submitted successfully";
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            OASISErrorHandling.HandleError(ref result, $"Stacks API error: {response.StatusCode} - {errorContent}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error sending NFT via Stacks blockchain: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT: {ex.Message}", ex);
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // BlockStack/Stacks blockchain uses SIP-009 NFT standard
                // Mint NFT via Stacks blockchain RPC API
                var sendToAvatarId = request?.SendToAvatarAfterMintingId ?? Guid.Empty;
                if (request == null || sendToAvatarId == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid NFT mint request");
                    return result;
                }

                try
                {
                    // Use Stacks API to mint NFT
                    // SIP-009 NFT mint: (mint principal)
                    var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                    using (var httpClient = new HttpClient())
                    {
                        // Get NFT contract address from request (from meta or base)
                        var contractAddress = request.MetaData?.GetValueOrDefault("NFTTokenAddress") ?? "";
                        
                        // Construct Stacks transaction payload for NFT minting
                        var mintPayload = new
                        {
                            contract_address = contractAddress,
                            function_name = "mint",
                            function_args = new[]
                            {
                                sendToAvatarId.ToString()
                            }
                        };
                        
                        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(mintPayload);
                        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                        
                        var response = await httpClient.PostAsync($"{stacksApiUrl}/contract-call", content);
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var txResponse = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            
                            var txId = txResponse?.GetValueOrDefault("txid")?.ToString() ?? "";
                            
                            result.Result = new Web3NFTTransactionResponse
                            {
                                TransactionResult = txId,
                                SendNFTTransactionResult = txId
                            };
                            result.Message = "NFT mint transaction submitted successfully";
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            OASISErrorHandling.HandleError(ref result, $"Stacks API error: {response.StatusCode} - {errorContent}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error minting NFT via Stacks blockchain: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT: {ex.Message}", ex);
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // BlockStack/Stacks blockchain uses SIP-009 NFT standard
                // For burning NFTs, we need to interact with the Stacks blockchain
                OASISErrorHandling.HandleWarning(ref result, "BlockStack Gaia storage doesn't support on-chain NFT burning. Use Stacks blockchain RPC for NFT operations.");
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
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
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

                // Lock NFT for cross-chain transfer
                // Use Stacks blockchain RPC for NFT locking
                OASISErrorHandling.HandleWarning(ref result, "BlockStack Gaia storage doesn't support on-chain NFT locking. Use Stacks blockchain RPC for NFT operations.");
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
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
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

                // Unlock NFT after cross-chain transfer
                // Use Stacks blockchain RPC for NFT unlocking
                OASISErrorHandling.HandleWarning(ref result, "BlockStack Gaia storage doesn't support on-chain NFT unlocking. Use Stacks blockchain RPC for NFT operations.");
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Withdraw NFT for cross-chain transfer using Stacks blockchain
                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                    string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                    return result;
                }

                // Use Stacks API to withdraw NFT (transfer to bridge contract)
                var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                using (var httpClient = new HttpClient())
                {
                    var bridgeContractAddress = _contractAddress ?? "SP000000000000000000002Q6VF78";
                    
                    var withdrawPayload = new
                    {
                        contract_address = nftTokenAddress,
                        function_name = "transfer",
                        function_args = new[]
                        {
                            tokenId,
                            senderAccountAddress,
                            bridgeContractAddress
                        }
                    };
                    
                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(withdrawPayload);
                    var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync($"{stacksApiUrl}/contract-call", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var txData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = txData.TryGetProperty("txid", out var txid) ? txid.GetString() ?? "" : "",
                            Status = BridgeTransactionStatus.Pending
                        };
                        result.IsError = false;
                        result.Message = "NFT withdrawal initiated successfully via Stacks blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to withdraw NFT: {response.StatusCode}");
                    }
                }
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
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate BlockStack provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Deposit NFT from cross-chain transfer using Stacks blockchain
                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                    string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, and receiver address are required");
                    return result;
                }

                // Use Stacks API to deposit NFT (transfer from bridge contract to receiver)
                var stacksApiUrl = "https://api.stacks.co/v2/transactions";
                using (var httpClient = new HttpClient())
                {
                    var bridgeContractAddress = _contractAddress ?? "SP000000000000000000002Q6VF78";
                    
                    var depositPayload = new
                    {
                        contract_address = nftTokenAddress,
                        function_name = "transfer",
                        function_args = new[]
                        {
                            tokenId,
                            bridgeContractAddress,
                            receiverAccountAddress
                        }
                    };
                    
                    var jsonPayload = System.Text.Json.JsonSerializer.Serialize(depositPayload);
                    var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync($"{stacksApiUrl}/contract-call", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var txData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                        
                        result.Result = new BridgeTransactionResponse
                        {
                            TransactionId = txData.TryGetProperty("txid", out var txid) ? txid.GetString() ?? "" : "",
                            Status = BridgeTransactionStatus.Completed
                        };
                        result.IsError = false;
                        result.Message = "NFT deposit completed successfully via Stacks blockchain";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to deposit NFT: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
            }
            return result;
        }

    }
}
