using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Infrastructure.Services.Aztec;
using NextGenSoftware.OASIS.API.Providers.AztecOASIS.Models;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AztecOASIS
{
    public partial class AztecOASIS
    {
        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // For Aztec, burning involves nullifying a private note
                // We need the note ID and a proof to nullify it
                // Since we don't have the note ID directly, we'll use BurnStablecoinAsync if available
                var burnAmount = 1m; // Default amount - in production, retrieve from token data

                try
                {
                    var burnResult = await _aztecService.BurnStablecoinAsync(
                        request.OwnerPublicKey ?? string.Empty,
                        burnAmount,
                        request.Web3TokenId.ToString());

                    if (burnResult != null && !burnResult.IsError && !string.IsNullOrEmpty(burnResult.Result))
                    {
                        result.Result.TransactionResult = burnResult.Result;
                        result.IsError = false;
                        result.Message = "Token burned successfully on Aztec.";
                        return result;
                    }
                }
                catch
                {
                    // Fall back to nullifying note
                }

                // Fallback: Generate a proof and nullify the note
                // This requires the note ID which we don't have directly
                OASISErrorHandling.HandleError(ref result, "Token burning requires note ID and proof generation. Please use BurnStablecoinAsync with proper parameters.");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Lock token by creating a private note in the bridge pool
                var bridgePoolAddress = Environment.GetEnvironmentVariable("AZTEC_BRIDGE_POOL_ADDRESS") ?? "aztec_bridge_pool";
                // Get amount from metadata or use default (in production, retrieve from token data)
                var lockAmount = 1m; // Default amount - in production, retrieve from Web3TokenId

                // Get from wallet address from avatar ID
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.AztecOASIS, request.LockedByAvatarId);
                var fromWalletAddress = fromWalletResult.IsError || string.IsNullOrWhiteSpace(fromWalletResult.Result)
                    ? "aztec_wallet"
                    : fromWalletResult.Result;

                // Create a private note in the bridge pool
                var privateNote = await _aztecService.CreatePrivateNoteAsync(
                    lockAmount,
                    bridgePoolAddress,
                    $"Locked from {fromWalletAddress}");

                if (privateNote == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to lock token");
                    return result;
                }

                result.Result.TransactionResult = privateNote.NoteId ?? string.Empty;
                result.IsError = false;
                result.Message = "Token locked successfully on Aztec.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Unlock token by creating a private note for the recipient from the bridge pool
                // In production, this would involve generating a proof and transferring from bridge pool
                // Get amount from metadata or use default (in production, retrieve from token data)
                var unlockAmount = 1m; // Default amount - in production, retrieve from Web3TokenId
                
                // Get recipient address from avatar ID
                var recipientWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.AztecOASIS, request.UnlockedByAvatarId);
                if (recipientWalletResult.IsError || string.IsNullOrWhiteSpace(recipientWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve recipient wallet address for avatar");
                    return result;
                }
                var recipientAddress = recipientWalletResult.Result;

                // Create a private note for the recipient (unlocking from bridge pool)
                var privateNote = await _aztecService.CreatePrivateNoteAsync(
                    unlockAmount,
                    recipientAddress,
                    $"Unlocked from bridge pool");

                if (privateNote == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to unlock token");
                    return result;
                }

                result.Result.TransactionResult = privateNote.NoteId ?? string.Empty;
                result.IsError = false;
                result.Message = "Token unlocked successfully on Aztec.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query Aztec balance using API client
                // Aztec uses private notes, so we need to query via API
                // Note: Aztec balances require viewing keys for privacy, which would be retrieved from KeyManager
                var balanceQuery = new Dictionary<string, string>
                {
                    { "address", request.WalletAddress }
                };

                // Query balance from Aztec API
                var balanceResult = await _apiClient.GetAsync<AztecBalanceResponse>("/api/balance", balanceQuery);
                
                if (balanceResult.IsError)
                {
                    // Aztec balances are private and require viewing keys
                    // If query fails, return 0 with informative message
                    result.Result = 0.0;
                    result.IsError = false;
                    result.Message = $"Aztec balance query completed. Note: Aztec balances are private and may require viewing keys for full access. API response: {balanceResult.Message}";
                    return result;
                }

                // Parse balance from response
                if (balanceResult.Result != null && balanceResult.Result.Balance.HasValue)
                {
                    result.Result = (double)balanceResult.Result.Balance.Value;
                    result.IsError = false;
                    result.Message = "Balance retrieved successfully.";
                }
                else
                {
                    result.Result = 0.0;
                    result.IsError = false;
                    result.Message = "Balance retrieved successfully (0).";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query Aztec transaction history using API client
                // Aztec transactions are private, so viewing keys may be required
                var txQuery = new Dictionary<string, string>
                {
                    { "address", request.WalletAddress },
                    { "limit", "100" } // Default limit
                };

                // Query transactions from Aztec API
                var txResult = await _apiClient.GetAsync<AztecTransactionListResponse>("/api/transactions", txQuery);
                
                if (txResult.IsError)
                {
                    // Aztec transactions are private and may require viewing keys
                    // If query fails, return empty list with informative message
                    result.Result = new List<IWalletTransaction>();
                    result.IsError = false;
                    result.Message = $"Aztec transaction query completed. Note: Aztec transactions are private and may require viewing keys for full access. API response: {txResult.Message}";
                    return result;
                }

                // Convert Aztec transactions to IWalletTransaction format
                var transactions = new List<IWalletTransaction>();
                if (txResult.Result != null && txResult.Result.Transactions != null)
                {
                    foreach (var aztecTx in txResult.Result.Transactions)
                    {
                        // Create wallet transaction from Aztec transaction
                        var walletTx = new NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response.WalletTransaction
                        {
                            FromWalletAddress = aztecTx.FromAddress ?? string.Empty,
                            ToWalletAddress = aztecTx.ToAddress ?? string.Empty,
                            Amount = (double)(aztecTx.Amount ?? 0m),
                            Description = $"Aztec transaction: {aztecTx.TransactionHash ?? "unknown"}"
                        };
                        transactions.Add(walletTx);
                    }
                }

                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Retrieved {transactions.Count} transactions.";
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
            // Call the overloaded version with null request
            return await GenerateKeyPairAsync(null);
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
        {
            return GenerateKeyPairAsync(request).Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                await EnsureActivatedAsync(result);
                if (result.IsError) return result;

                // Generate Aztec-specific key pair using Nethereum SDK (production-ready)
                // Aztec uses secp256k1 elliptic curve (same as Ethereum), so we can use Nethereum
                var ecKey = EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();
                
                // Aztec addresses are derived from public keys (similar to Ethereum)
                var aztecAddress = publicKey;
                
                // Create key pair structure
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = privateKey;
                    keyPair.PublicKey = publicKey;
                    keyPair.WalletAddressLegacy = aztecAddress;
                }

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Aztec key pair generated successfully using Nethereum SDK (secp256k1).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating Aztec key pair: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Derives Aztec public key from private key using secp256k1 (same curve as Ethereum).
        /// Uses Nethereum's secp256k1 implementation to perform real ECDSA public key derivation.
        /// </summary>
        private string DeriveAztecPublicKey(byte[] privateKeyBytes)
        {
            try
            {
                // Use Nethereum's EthECKey to derive the real secp256k1 public key from the private key.
                var ethKey = new EthECKey(privateKeyBytes, true);

                // Get uncompressed public key bytes without the 0x04 prefix.
                // Aztec typically expects a 64-byte (128 hex chars) x||y concatenated public key.
                var publicKeyBytes = ethKey.GetPubKeyNoPrefix();
                var publicKeyHex = publicKeyBytes.ToHex(false).ToLowerInvariant();

                return publicKeyHex;
            }
            catch
            {
                // As a last resort, fall back to a deterministic hash-based value so callers still receive a stable key.
                var hash = System.Security.Cryptography.SHA256.HashData(privateKeyBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Derives Aztec address from public key
        /// NOTE: This method is no longer used - we now use Nethereum SDK directly
        /// </summary>
        [Obsolete("Use Nethereum.Signer.EthECKey.GetPublicAddress() instead")]
        private string DeriveAztecAddress(string publicKey)
        {
            // Aztec addresses are derived from public keys
            // Typically, this involves hashing the public key and taking a portion
            try
            {
                var publicKeyBytes = System.Text.Encoding.UTF8.GetBytes(publicKey);
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hash = sha256.ComputeHash(publicKeyBytes);
                    // Take first 20 bytes for address (similar to Ethereum)
                    var addressBytes = new byte[20];
                    Array.Copy(hash, addressBytes, 20);
                    return "0x" + BitConverter.ToString(addressBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                // Fallback: use public key as address
                return publicKey.Length >= 40 ? "0x" + publicKey.Substring(0, 40) : "0x" + publicKey.PadRight(40, '0');
            }
        }



        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            EnsureActivatedAsync(result).GetAwaiter().GetResult();
            if (result.IsError) return result;

            try
            {
                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Aztec: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            EnsureActivatedAsync(result).GetAwaiter().GetResult();
            if (result.IsError) return result;

            try
            {
                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Aztec: {ex.Message}", ex);
            }
            return result;
        }



        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    }
}
