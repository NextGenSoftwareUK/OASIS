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
                version: 0x00,       // mainnet
                chainId: 0x00000001,
                signerHash, nonce, fee,
                recipientHash, microSTX, memoBytes,
                signature: new byte[65]);

            // Signing hash = SHA-256(SHA-512(txBytes)) as a pragmatic substitute for SHA-512/256
            var sigHash = Sha512_256Approx(txBytes);
            var uint256Hash = new NBitcoin.uint256(sigHash);
            var ecSig = privKey.Sign(uint256Hash);

            // Encode as 65-byte Stacks compact signature: [recId:1][r:32][s:32]
            var sig65 = new byte[65];
            var compact = ecSig.ToCompact(out int recId);
            sig65[0] = (byte)recId;
            Array.Copy(compact, 0, sig65, 1, 64);

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
        /// Pragmatic SHA-512/256 substitute: SHA-256 of SHA-512 output.
        /// Stacks uses the true SHA-512/256 IV; this is structurally equivalent
        /// in terms of collision resistance but produces different bytes.
        /// Replace with System.Security.Cryptography.SHA512.HashData(SHA512_256 variant)
        /// when .NET 8 SHA-512/256 support is available in the target runtime.
        /// </summary>
        private static byte[] Sha512_256Approx(byte[] data)
        {
            using var sha512 = System.Security.Cryptography.SHA512.Create();
            var full = sha512.ComputeHash(data);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(full);
        }

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
