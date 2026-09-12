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
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS
{
    public partial class SuiOASIS
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
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "WalletAddress is required");
                    return result;
                }

                // Get Sui transactions via RPC
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "sui_getTransactions",
                    @params = new object[] { request.WalletAddress, 10 } // Default to 10 transactions
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("", content);

                var transactions = new List<IWalletTransaction>();
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (responseData.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tx in resultProp.EnumerateArray())
                        {
                            // Extract transaction digest for deterministic GUID
                            var txDigest = tx.TryGetProperty("digest", out var digestProp) ? digestProp.GetString() : null;
                            Guid txGuid;
                            if (!string.IsNullOrWhiteSpace(txDigest))
                            {
                                using var sha256 = System.Security.Cryptography.SHA256.Create();
                                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(txDigest));
                                txGuid = new Guid(hashBytes.Take(16).ToArray());
                            }
                            else
                            {
                                // Fallback: use deterministic GUID from transaction data
                                var txData = $"{request.WalletAddress}:{tx.GetRawText()}";
                                txGuid = CreateDeterministicGuid($"{ProviderType.Value}:tx:{txData}");
                            }
                            
                            var walletTx = new WalletTransaction
                            {
                                TransactionId = txGuid,
                                FromWalletAddress = tx.TryGetProperty("from", out var from) ? from.GetString() : string.Empty,
                                ToWalletAddress = tx.TryGetProperty("to", out var to) ? to.GetString() : string.Empty,
                                Amount = tx.TryGetProperty("amount", out var amt) ? amt.GetString() != null ? double.Parse(amt.GetString()) / 1_000_000_000.0 : 0.0 : 0.0,
                                Description = txDigest != null ? $"Sui transaction: {txDigest}" : "Sui transaction"
                            };
                            transactions.Add(walletTx);
                        }
                    }
                }

                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} Sui transactions";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
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
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                    return result;
                }

                // Generate Sui Ed25519 key pair (Sui uses Ed25519).
                // The private key seed is 32 bytes; public key is derived deterministically from it.
                var privateKeySeed = new byte[32];
                using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(privateKeySeed);
                }

                byte[] publicKeyBytes;
                byte[] expandedPrivateKey;
                Chaos.NaCl.Ed25519.KeyPairFromSeed(out publicKeyBytes, out expandedPrivateKey, privateKeySeed);

                var privateKey = Convert.ToBase64String(privateKeySeed);
                var publicKey = Convert.ToBase64String(publicKeyBytes);

                // Sui address derivation: blake2b-256( schemeFlag || publicKey )
                var address = DeriveSuiAddress(publicKeyBytes);

                // Create KeyPairAndWallet using KeyHelper but override with Sui-specific values
                //var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                //if (keyPair != null)
                //{
                //    keyPair.PrivateKey = privateKey;
                //    keyPair.PublicKey = publicKey;
                //    keyPair.WalletAddressLegacy = address; // Sui address
                //}

                result.Result = new KeyPairAndWallet()
                {
                    PrivateKey = privateKey,
                    PublicKey = publicKey,
                    WalletAddressLegacy = address
                };
                result.IsError = false;
                result.Message = "Sui Ed25519 key pair generated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }

        /// <summary>
        /// Derives Sui address from public key
        /// Sui uses a specific address format derived from Ed25519 public keys
        /// </summary>
        private string DeriveSuiAddress(byte[] publicKeyBytes)
        {
            try
            {
                const byte ed25519SchemeFlag = 0x00;
                var data = new byte[1 + publicKeyBytes.Length];
                data[0] = ed25519SchemeFlag;
                Buffer.BlockCopy(publicKeyBytes, 0, data, 1, publicKeyBytes.Length);

                // Sui uses Blake2b-256 (32 bytes) over scheme flag + public key
                var config = new Isopoh.Cryptography.Blake2b.Blake2BConfig { OutputSizeInBytes = 32 };
                var hash = Isopoh.Cryptography.Blake2b.Blake2B.ComputeHash(data, config, Isopoh.Cryptography.SecureArray.SecureArray.DefaultCall);

                return "0x" + Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch
            {
                // Fallback to hex representation
                return "0x" + BitConverter.ToString(publicKeyBytes).Replace("-", "").ToLowerInvariant();
            }
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
                OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                return result;
            }

            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
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
                OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                return result;
            }

            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
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
                OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
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
                OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
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


        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!_isActivated || _httpClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Sui provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                // Call Sui RPC API to get account balance
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "suix_getBalance",
                    @params = new object[] { accountAddress }
                };

                var response = await _httpClient.PostAsJsonAsync("", rpcRequest, token);
                var content = await response.Content.ReadAsStringAsync(token);
                var jsonDoc = JsonDocument.Parse(content);

                if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement) &&
                    resultElement.TryGetProperty("totalBalance", out var totalBalanceElement))
                {
                    var balanceStr = totalBalanceElement.GetString();
                    if (ulong.TryParse(balanceStr, out var balance))
                    {
                        // Sui amounts are in MIST (1 SUI = 10^9 MIST)
                        result.Result = balance / 1_000_000_000m;
                        result.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse balance");
                    }
                }
                else
                {
                    result.Result = 0m;
                    result.IsError = false;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Sui account balance: {ex.Message}", ex);
            }
            return result;
        }

    }
}
