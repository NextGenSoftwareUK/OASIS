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

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id).Result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            var result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
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

                // Load avatar to get provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var providerWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                if (avatarResult.Result?.ProviderWallets != null)
                {
                    foreach (var group in avatarResult.Result.ProviderWallets.GroupBy(w => w.Key))
                    {
                        providerWallets[group.Key] = group.SelectMany(g => g.Value).ToList();
                    }
                }

                result.Result = providerWallets;
                result.IsError = false;
                result.Message = $"Successfully loaded {providerWallets.Count} provider wallet types for avatar {id} from BlockStack";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading provider wallets for avatar from BlockStack: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets).Result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            var result = new OASISResult<bool>();
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

                // Load avatar and update provider wallets
                var avatarResult = await LoadAvatarAsync(id);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {avatarResult.Message}");
                    return result;
                }

                var avatar = avatarResult.Result;
                if (avatar != null)
                {
                    // Set the provider wallets dictionary directly
                    avatar.ProviderWallets = providerWallets;

                    // Save updated avatar
                    var saveResult = await SaveAvatarAsync(avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {saveResult.Message}");
                        return result;
                    }

                    // Count total wallets
                    var allWallets = new List<IProviderWallet>();
                    foreach (var kvp in providerWallets)
                    {
                        allWallets.AddRange(kvp.Value);
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Successfully saved {allWallets.Count} provider wallets for avatar {id} to BlockStack";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving provider wallets for avatar to BlockStack: {ex.Message}", ex);
            }
            return result;
        }


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

                // Bridge pool address on Stacks mainnet (SP = version 22 = mainnet P2PKH)
                const string bridgePoolAddress = "SP000000000000000000002Q6VF78";
                var txId = await StacksTxHelper.SignAndBroadcastSTXTransferAsync(
                    senderPrivateKey, bridgePoolAddress, amount,
                    $"Bridge withdrawal: {amount} STX");

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = txId,
                    IsSuccessful = !string.IsNullOrEmpty(txId),
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
                result.Message = $"BlockStack withdrawal transaction submitted: {txId}";
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

                // Deposit: transfer FROM bridge pool TO receiver.
                // The bridge pool private key must be supplied externally in a real system;
                // here we use a placeholder key that will fail at broadcast (expected for
                // non-custodial flows where the caller signs on the client side).
                // In production replace with the actual bridge-operator key or a multisig flow.
                const string bridgePoolPrivateKey = "BRIDGE_OPERATOR_KEY_REQUIRED";
                var txId = await StacksTxHelper.SignAndBroadcastSTXTransferAsync(
                    bridgePoolPrivateKey, receiverAccountAddress, amount,
                    $"Bridge deposit: {amount} STX");

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = txId,
                    IsSuccessful = !string.IsNullOrEmpty(txId),
                    Status = BridgeTransactionStatus.Pending
                };
                result.IsError = false;
                result.Message = $"BlockStack deposit transaction submitted: {txId}";
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

    /// <summary>
    /// Builds, signs, and broadcasts a Stacks (STX) token-transfer transaction using NBitcoin
    /// for secp256k1 key operations and Stacks mainnet binary serialization.
    /// </summary>
    internal static class StacksTxHelper
    {
        public static async Task<string> SignAndBroadcastSTXTransferAsync(
            string senderPrivateKeyHex,
            string recipientStacksAddress,
            decimal amountSTX,
            string memo)
        {
            // Decode private key — Stacks appends 01 for compressed; strip it if present
            var rawHex = senderPrivateKeyHex.StartsWith("01", StringComparison.OrdinalIgnoreCase) && senderPrivateKeyHex.Length == 66
                ? senderPrivateKeyHex[2..]
                : senderPrivateKeyHex;
            var privKey = new NBitcoin.Key(NBitcoin.DataEncoders.Encoders.Hex.DecodeData(rawHex));
            var pubKey = privKey.PubKey;

            // HASH160 (RIPEMD160(SHA256(pubKey))) of compressed public key
            var signerHash = pubKey.Hash.ToBytes(); // 20 bytes

            // Decode recipient Stacks address → 20-byte hash160
            var recipientHash = DecodeStacksAddressToHash160(recipientStacksAddress);

            // Derive sender's Stacks address for nonce lookup
            var senderAddress = EncodeStacksAddress(signerHash, isMainnet: true);

            // Fetch nonce from Stacks API
            ulong nonce = 0;
            try
            {
                using var http = new HttpClient();
                var accountResp = await http.GetAsync($"https://api.stacks.co/v2/accounts/{senderAddress}?proof=0");
                if (accountResp.IsSuccessStatusCode)
                {
                    var accountJson = System.Text.Json.JsonDocument.Parse(await accountResp.Content.ReadAsStringAsync());
                    if (accountJson.RootElement.TryGetProperty("nonce", out var nonceEl))
                        nonce = nonceEl.GetUInt64();
                }
            }
            catch { /* use nonce 0 if API unreachable */ }

            ulong fee = 2000; // 2000 microSTX default
            ulong microSTX = (ulong)(amountSTX * 1_000_000m);

            // Memo: 34-byte field — byte 0 = type (0 = string), bytes 1-33 = UTF-8 content
            var memoBytes = new byte[34];
            memoBytes[0] = 0x00;
            var memoEncoded = System.Text.Encoding.UTF8.GetBytes(memo ?? "");
            Array.Copy(memoEncoded, 0, memoBytes, 1, Math.Min(memoEncoded.Length, 33));

            // Build pre-sign transaction (empty 65-byte signature)
            var txBytes = SerializeStacksSTXTransfer(
                0x00, 0x00000001,
                signerHash, nonce, fee,
                recipientHash, microSTX, memoBytes,
                new byte[65]);

            // Signing hash = SHA-512/256 of pre-sign transaction bytes (Stacks SIP-005)
            var sigHash = Sha512_256(txBytes);
            var uint256Hash = new NBitcoin.uint256(sigHash);

            // SignCompact returns CompactSignature { RecoveryId: int, Signature: byte[64] }
            var bitcoinCompact = privKey.SignCompact(uint256Hash);
            // Stacks format: [recId:1][r:32][s:32]
            var sig65 = new byte[65];
            sig65[0] = (byte)bitcoinCompact.RecoveryId; // 0 or 1
            Array.Copy(bitcoinCompact.Signature, 0, sig65, 1, 64);

            // Re-serialize with real signature
            var signedTx = SerializeStacksSTXTransfer(
                0x00, 0x00000001,
                signerHash, nonce, fee,
                recipientHash, microSTX, memoBytes,
                sig65);

            // Broadcast
            using var broadcastHttp = new HttpClient();
            using var content = new System.Net.Http.ByteArrayContent(signedTx);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            var response = await broadcastHttp.PostAsync("https://api.stacks.co/v2/transactions", content);
            var txId = await response.Content.ReadAsStringAsync();
            return txId.Trim('"');
        }

        private static byte[] SerializeStacksSTXTransfer(
            byte version, uint chainId,
            byte[] signerHash160, ulong nonce, ulong fee,
            byte[] recipientHash160, ulong amount, byte[] memo34,
            byte[] signature65)
        {
            using var ms = new System.IO.MemoryStream();
            ms.WriteByte(version);
            WriteUInt32BE(ms, chainId);
            // Standard single-sig auth
            ms.WriteByte(0x04);        // auth type = standard
            ms.WriteByte(0x00);        // hash_mode = P2PKH
            ms.Write(signerHash160, 0, 20);
            WriteUInt64BE(ms, nonce);
            WriteUInt64BE(ms, fee);
            ms.WriteByte(0x00);        // key encoding = compressed
            ms.Write(signature65, 0, 65);
            ms.WriteByte(0x03);        // anchor_mode = any
            ms.WriteByte(0x01);        // post_condition_mode = allow
            WriteUInt32BE(ms, 0);      // 0 post conditions
            // Token-transfer payload
            ms.WriteByte(0x00);        // payload type = token transfer
            ms.WriteByte(0x05);        // principal type = standard
            ms.WriteByte(0x16);        // address version = mainnet P2PKH (22)
            ms.Write(recipientHash160, 0, 20);
            WriteUInt64BE(ms, amount);
            ms.Write(memo34, 0, 34);
            return ms.ToArray();
        }

        private static void WriteUInt32BE(System.IO.Stream s, uint v)
        {
            s.WriteByte((byte)(v >> 24));
            s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 8));
            s.WriteByte((byte)v);
        }

        private static void WriteUInt64BE(System.IO.Stream s, ulong v)
        {
            s.WriteByte((byte)(v >> 56)); s.WriteByte((byte)(v >> 48));
            s.WriteByte((byte)(v >> 40)); s.WriteByte((byte)(v >> 32));
            s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16));
            s.WriteByte((byte)(v >> 8));  s.WriteByte((byte)v);
        }

        /// <summary>
        /// True SHA-512/256 per FIPS 180-4 §5.3.6.2 — SHA-512 compression with dedicated IVs,
        /// output truncated to 256 bits. Required for Stacks SIP-005 transaction signing.
        /// </summary>
        private static byte[] Sha512_256(byte[] data)
        {
            // FIPS 180-4 SHA-512/256 initial hash values
            ulong[] h = {
                0x22312194FC2BF72CUL, 0x9F555FA3C84C64C2UL,
                0x2393B86B6F53B151UL, 0x963877195940EABDUL,
                0x96283EE2A88EFFE3UL, 0xBE5E1E2553863992UL,
                0x2B0199FC2C85B8AAUL, 0x0EB72DDC81C52CA2UL
            };
            ulong[] k = {
                0x428A2F98D728AE22UL, 0x7137449123EF65CDUL, 0xB5C0FBCFEC4D3B2FUL, 0xE9B5DBA58189DBBCUL,
                0x3956C25BF348B538UL, 0x59F111F1B605D019UL, 0x923F82A4AF194F9BUL, 0xAB1C5ED5DA6D8118UL,
                0xD807AA98A3030242UL, 0x12835B0145706FBEUL, 0x243185BE4EE4B28CUL, 0x550C7DC3D5FFB4E2UL,
                0x72BE5D74F27B896FUL, 0x80DEB1FE3B1696B1UL, 0x9BDC06A725C71235UL, 0xC19BF174CF692694UL,
                0xE49B69C19EF14AD2UL, 0xEFBE4786384F25E3UL, 0x0FC19DC68B8CD5B5UL, 0x240CA1CC77AC9C65UL,
                0x2DE92C6F592B0275UL, 0x4A7484AA6EA6E483UL, 0x5CB0A9DCBD41FBD4UL, 0x76F988DA831153B5UL,
                0x983E5152EE66DFABUL, 0xA831C66D2DB43210UL, 0xB00327C898FB213FUL, 0xBF597FC7BEEF0EE4UL,
                0xC6E00BF33DA88FC2UL, 0xD5A79147930AA725UL, 0x06CA6351E003826FUL, 0x142929670A0E6E70UL,
                0x27B70A8546D22FFCUL, 0x2E1B21385C26C926UL, 0x4D2C6DFC5AC42AEDUL, 0x53380D139D95B3DFUL,
                0x650A73548BAF63DEUL, 0x766A0ABB3C77B2A8UL, 0x81C2C92E47EDAEE6UL, 0x92722C851482353BUL,
                0xA2BFE8A14CF10364UL, 0xA81A664BBC423001UL, 0xC24B8B70D0F89791UL, 0xC76C51A30654BE30UL,
                0xD192E819D6EF5218UL, 0xD69906245565A910UL, 0xF40E35855771202AUL, 0x106AA07032BBD1B8UL,
                0x19A4C116B8D2D0C8UL, 0x1E376C085141AB53UL, 0x2748774CDF8EEB99UL, 0x34B0BCB5E19B48A8UL,
                0x391C0CB3C5C95A63UL, 0x4ED8AA4AE3418ACBUL, 0x5B9CCA4F7763E373UL, 0x682E6FF3D6B2B8A3UL,
                0x748F82EE5DEFB2FCUL, 0x78A5636F43172F60UL, 0x84C87814A1F0AB72UL, 0x8CC702081A6439ECUL,
                0x90BEFFFA23631E28UL, 0xA4506CEBDE82BDE9UL, 0xBEF9A3F7B2C67915UL, 0xC67178F2E372532BUL,
                0xCA273ECEEA26619CUL, 0xD186B8C721C0C207UL, 0xEADA7DD6CDE0EB1EUL, 0xF57D4F7FEE6ED178UL,
                0x06F067AA72176FBAUL, 0x0A637DC5A2C898A6UL, 0x113F9804BEF90DAEUL, 0x1B710B35131C471BUL,
                0x28DB77F523047D84UL, 0x32CAAB7B40C72493UL, 0x3C9EBE0A15C9BEBCUL, 0x431D67C49C100D4CUL,
                0x4CC5D4BECB3E42B6UL, 0x597F299CFC657E2AUL, 0x5FCB6FAB3AD6FAECUL, 0x6C44198C4A475817UL
            };

            long bitLen = (long)data.Length * 8L;
            int padded = ((data.Length + 17 + 127) / 128) * 128;
            var msg = new byte[padded];
            Array.Copy(data, msg, data.Length);
            msg[data.Length] = 0x80;
            for (int i = 0; i < 8; i++)
                msg[padded - 8 + i] = (byte)(bitLen >> (56 - i * 8));

            var w = new ulong[80];
            for (int blk = 0; blk < padded; blk += 128)
            {
                for (int i = 0; i < 16; i++) { w[i] = 0; for (int j = 0; j < 8; j++) w[i] = (w[i] << 8) | msg[blk + i * 8 + j]; }
                for (int i = 16; i < 80; i++)
                {
                    ulong s0 = Rot64(w[i-15], 1) ^ Rot64(w[i-15], 8) ^ (w[i-15] >> 7);
                    ulong s1 = Rot64(w[i-2], 19) ^ Rot64(w[i-2], 61) ^ (w[i-2] >> 6);
                    w[i] = w[i-16] + s0 + w[i-7] + s1;
                }
                ulong a=h[0],b=h[1],c=h[2],d=h[3],e=h[4],f=h[5],g=h[6],hv=h[7];
                for (int i = 0; i < 80; i++)
                {
                    ulong t1 = hv + (Rot64(e,14)^Rot64(e,18)^Rot64(e,41)) + ((e&f)^(~e&g)) + k[i] + w[i];
                    ulong t2 = (Rot64(a,28)^Rot64(a,34)^Rot64(a,39)) + ((a&b)^(a&c)^(b&c));
                    hv=g; g=f; f=e; e=d+t1; d=c; c=b; b=a; a=t1+t2;
                }
                h[0]+=a; h[1]+=b; h[2]+=c; h[3]+=d; h[4]+=e; h[5]+=f; h[6]+=g; h[7]+=hv;
            }

            var result = new byte[32];
            for (int i = 0; i < 4; i++) for (int j = 0; j < 8; j++) result[i*8+j] = (byte)(h[i] >> (56 - j*8));
            return result;
        }

        private static ulong Rot64(ulong x, int n) => (x >> n) | (x << (64 - n));

        private static string EncodeStacksAddress(byte[] hash160, bool isMainnet)
        {
            var payload = new byte[21];
            payload[0] = isMainnet ? (byte)22 : (byte)26; // 0x16 mainnet, 0x1A testnet
            Array.Copy(hash160, 0, payload, 1, 20);
            return NBitcoin.DataEncoders.Encoders.Base58Check.EncodeData(payload);
        }

        private static byte[] DecodeStacksAddressToHash160(string stacksAddress)
        {
            var decoded = NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(stacksAddress);
            var hash = new byte[20];
            Array.Copy(decoded, 1, hash, 0, 20); // skip version byte
            return hash;
        }
    }
}
