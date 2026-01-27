using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.DTOs;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Extensions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

/// <summary>
/// OASIS Provider for Radix DLT blockchain with cross-chain bridge support and first-party oracle capabilities.
/// Inspired by API3's Airnode - Radix can run their own oracle node with no middleware.
/// </summary>
public class RadixOASIS : OASISStorageProviderBase, IOASISStorageProvider, 
    IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNETProvider
{
    private IRadixService? _radixService;
    private readonly RadixOASISConfig _config;
    private readonly HttpClient _httpClient;
    private RadixOracleNode? _oracleNode;
    private RadixChainObserver? _chainObserver;

    /// <summary>
    /// Gets the Radix bridge service for cross-chain operations
    /// </summary>
    public IRadixService? RadixBridgeService => _radixService;

    /// <summary>
    /// Gets the Radix first-party oracle node - allows Radix to run their own oracle with no middleware
    /// </summary>
    public RadixOracleNode? OracleNode => _oracleNode;

    /// <summary>
    /// Gets the Radix chain observer for oracle operations
    /// </summary>
    public RadixChainObserver? ChainObserver => _chainObserver;

    public RadixOASIS(string hostUri, byte networkId, string accountAddress, string privateKey)
    {
        this.ProviderName = nameof(RadixOASIS);
        this.ProviderDescription = "Radix DLT Blockchain Provider with Bridge Support";
        this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.RadixOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

        _config = new RadixOASISConfig
        {
            HostUri = hostUri,
            NetworkId = networkId,
            AccountAddress = accountAddress,
            PrivateKey = privateKey
        };

        _httpClient = new HttpClient();
    }

    public RadixOASIS(RadixOASISConfig config)
    {
        this.ProviderName = nameof(RadixOASIS);
        this.ProviderDescription = "Radix DLT Blockchain Provider with Bridge Support";
        this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.RadixOASIS);
        this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = new HttpClient();
    }

    #region Provider Activation/Deactivation

    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        var result = new OASISResult<bool>();

        try
        {
            _radixService = new RadixService(_config, _httpClient);
            
            // Test connection by getting a dummy balance
            var testResult = await _radixService.GetAccountBalanceAsync(_config.AccountAddress);
            
            if (testResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result,
                    $"Failed to activate RadixOASIS provider: {testResult.Message}");
                return result;
            }

            // Initialize first-party oracle node (Airnode-style, no middleware)
            _chainObserver = new RadixChainObserver(_radixService, _config);
            _oracleNode = new RadixOracleNode(_radixService, _config);

            result.Result = true;
            IsProviderActivated = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result,
                $"Error activating RadixOASIS provider: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<bool> ActivateProvider()
    {
        return ActivateProviderAsync().Result;
    }

    public override async Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        // Stop oracle node if running
        if (_oracleNode?.IsRunning == true)
        {
            await _oracleNode.StopAsync();
        }

        _radixService = null;
        _oracleNode = null;
        _chainObserver = null;
        IsProviderActivated = false;
        return await Task.FromResult(new OASISResult<bool>(true));
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        // Stop oracle node if running
        if (_oracleNode?.IsRunning == true)
        {
            _oracleNode.StopAsync().Wait();
        }

        _radixService = null;
        _oracleNode = null;
        _chainObserver = null;
        IsProviderActivated = false;
        return new OASISResult<bool>(true);
    }

    #endregion

    #region IOASISBlockchainStorageProvider Implementation

    /// <summary>
    /// Sends a transaction on the Radix network
    /// </summary>
    public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
    }

    /// <summary>
    /// Sends a transaction on the Radix network asynchronously
    /// </summary>
    public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        var result = new OASISResult<ITransactionResponse>();
        
        try
        {
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            // Use DepositAsync to send XRD from config account to destination
            // Note: For full implementation, we'd need to support sending from any address
            // For now, we use the configured account as the sender
            var depositResult = await _radixService.DepositAsync(amount, toWalletAddress);
            
            if (depositResult.IsError || depositResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, 
                    depositResult.Message ?? "Failed to send transaction");
                return result;
            }

            // Create transaction response
            result.Result = new TransactionResponse
            {
                TransactionResult = depositResult.Result?.TransactionId ?? "Unknown"
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<ITransactionResponse>(ref result,
                $"Error sending transaction: {ex.Message}", ex);
            return result;
        }
    }

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
    {
        return SendTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.ToWalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "To wallet address is required");
                return result;
            }

            // Use DepositAsync to send tokens
            var depositResult = await _radixService.DepositAsync(request.Amount, request.ToWalletAddress);
            
            if (depositResult.IsError || depositResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, 
                    depositResult.Message ?? "Failed to send token");
                return result;
            }

            // Create transaction response
            result.Result = new TransactionResponse
            {
                TransactionResult = depositResult.Result.TransactionId ?? depositResult.Result.DuplicateTransactionId ?? "Unknown"
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError<ITransactionResponse>(ref result,
                $"Error sending token: {ex.Message}", ex);
            return result;
        }
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    {
        return MintTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        result.IsError = true;
        result.Message = "MintToken not yet implemented for RadixOASIS";
        return await Task.FromResult(result);
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
    {
        return BurnTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        result.IsError = true;
        result.Message = "BurnToken not yet implemented for RadixOASIS";
        return await Task.FromResult(result);
    }

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
    {
        return LockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        result.IsError = true;
        result.Message = "LockToken not yet implemented for RadixOASIS";
        return await Task.FromResult(result);
    }

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
    {
        return UnlockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        result.IsError = true;
        result.Message = "UnlockToken not yet implemented for RadixOASIS";
        return await Task.FromResult(result);
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
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            var balanceResult = await _radixService.GetAccountBalanceAsync(request.WalletAddress);
            if (balanceResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, balanceResult.Message ?? "Failed to get balance");
                return result;
            }

            result.Result = (double)balanceResult.Result;
            result.IsError = false;
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
        result.IsError = true;
        result.Message = "GetTransactions not yet implemented for RadixOASIS";
        return await Task.FromResult(result);
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
            if (_radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix service is not initialized. Activate provider first.");
                return result;
            }

            // Generate Radix key pair
            var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            if (keyPair != null)
            {
                // Radix uses Ed25519 keys - generate using cryptographic RNG
                using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
                var privateKeyBytes = new byte[32];
                rng.GetBytes(privateKeyBytes);
                
                keyPair.PrivateKey = Convert.ToBase64String(privateKeyBytes);
                // Derive public key (simplified - in production use proper Ed25519 library)
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var publicKeyBytes = sha256.ComputeHash(privateKeyBytes);
                keyPair.PublicKey = Convert.ToBase64String(publicKeyBytes);
                keyPair.WalletAddressLegacy = keyPair.PublicKey;
            }

            result.Result = keyPair;
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
        }
        return result;
    }

    #endregion

    #region Not Implemented (Future Storage Features)

    // These methods would be implemented for full OASIS storage provider support
    // For now, the RadixOASIS provider focuses on blockchain transactions and bridge operations

    public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IAvatar>>
        {
            IsError = true,
            Message = "LoadAllAvatars not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
    {
        return new OASISResult<IEnumerable<IAvatar>>
        {
            IsError = true,
            Message = "LoadAllAvatars not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        return Task.FromResult(new OASISResult<IAvatar>
        {
            IsError = true,
            Message = "LoadAvatar not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
    {
        return new OASISResult<IAvatar>
        {
            IsError = true,
            Message = "LoadAvatar not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
    {
        return Task.FromResult(new OASISResult<IAvatar>
        {
            IsError = true,
            Message = "LoadAvatarByEmail not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
    {
        return new OASISResult<IAvatar>
        {
            IsError = true,
            Message = "LoadAvatarByEmail not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
    {
        return Task.FromResult(new OASISResult<IAvatar>
        {
            IsError = true,
            Message = "LoadAvatarByUsername not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
    {
        return new OASISResult<IAvatar>
        {
            IsError = true,
            Message = "LoadAvatarByUsername not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
    {
        return Task.FromResult(new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "LoadAvatarDetail not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
    {
        return new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "LoadAvatarDetail not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated || _radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                return result;
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await AvatarManager.Instance.SaveAvatarAsync(avatar);
            }

            // Serialize avatar to JSON
            string avatarInfo = System.Text.Json.JsonSerializer.Serialize(avatar);
            string avatarId = avatar.Id.ToString();

            // Get wallet for signing
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for avatar");
                return result;
            }

            // Build transaction manifest calling OASIS blueprint's create_avatar function
            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = _config.OasisBlueprintAddress,
                        methodName = "create_avatar",
                        args = new[]
                        {
                            new { kind = "String", value = avatarId },
                            new { kind = "String", value = avatarInfo }
                        }
                    }
                },
                blobs = new object[0]
            };

            // Get construction metadata for transaction header
            var metadataResult = await _httpClient.GetConstructionMetadataAsync(_config);
            if (metadataResult == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get Radix construction metadata");
                return result;
            }

            // Build transaction header
            var transactionHeader = new
            {
                networkId = _config.NetworkId,
                startEpochInclusive = metadataResult.CurrentEpoch,
                endEpochExclusive = metadataResult.CurrentEpoch + 50,
                nonce = new Random().Next(),
                notaryPublicKey = walletResult.Result.PublicKey,
                notaryIsSignatory = true,
                tipPercentage = 0
            };

            // Build complete transaction
            var transactionData = new
            {
                network = network,
                manifest = System.Text.Json.JsonSerializer.Serialize(manifest),
                header = transactionHeader,
                message = new { kind = "None" }
            };

            // Submit transaction via Radix Gateway API
            var submitResult = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                _httpClient,
                $"{_config.HostUri}/core/lts/transaction/submit",
                transactionData);

            if (submitResult.IsError || submitResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit Radix transaction: {submitResult.Message}");
                return result;
            }

            // Store transaction hash
            if (!string.IsNullOrEmpty(submitResult.Result.TransactionHash))
            {
                avatar.ProviderUniqueStorageKey[ProviderType.Value] = submitResult.Result.TransactionHash;
            }

            result.Result = avatar;
            result.IsError = false;
            result.IsSaved = true;
            result.Message = "Avatar saved to Radix blueprint successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
    {
        return SaveAvatarAsync(avatar).Result;
    }

    public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
    {
        return Task.FromResult(new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "SaveAvatarDetail not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
    {
        return new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "SaveAvatarDetail not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        return Task.FromResult(new OASISResult<bool>
        {
            IsError = true,
            Message = "DeleteAvatar not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
    {
        return new OASISResult<bool>
        {
            IsError = true,
            Message = "DeleteAvatar not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
    {
        return Task.FromResult(new OASISResult<bool>
        {
            IsError = true,
            Message = "DeleteAvatarByEmail not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
    {
        return new OASISResult<bool>
        {
            IsError = true,
            Message = "DeleteAvatarByEmail not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
    {
        return Task.FromResult(new OASISResult<bool>
        {
            IsError = true,
            Message = "DeleteAvatarByUsername not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
    {
        return new OASISResult<bool>
        {
            IsError = true,
            Message = "DeleteAvatarByUsername not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
    {
        return Task.FromResult(new OASISResult<bool>
        {
            IsError = true,
            Message = "DeleteAvatar not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        return new OASISResult<bool>
        {
            IsError = true,
            Message = "DeleteAvatar not implemented for RadixOASIS - use for bridge operations"
        };
    }

    #endregion

    #region Bridge Methods (IOASISBlockchainStorageProvider)

    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        try
        {
            if (!IsProviderActivated || _radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

            return await _radixService.GetAccountBalanceAsync(accountAddress, token);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
            return result;
        }
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
        try
        {
            if (!IsProviderActivated || _radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

            return await _radixService.CreateAccountAsync(token);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error creating account: {ex.Message}", ex);
            return result;
        }
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey)>();
        try
        {
            if (!IsProviderActivated || _radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

            return await _radixService.RestoreAccountAsync(seedPhrase, token);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
            return result;
        }
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

            return await _radixService.WithdrawAsync(amount, senderAccountAddress, senderPrivateKey);
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
            return result;
        }
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

            return await _radixService.DepositAsync(amount, receiverAccountAddress);
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
            return result;
        }
    }

    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        try
        {
            if (!IsProviderActivated || _radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

            return await _radixService.GetTransactionStatusAsync(transactionHash, token);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
            return result;
        }
    }

    #endregion

    #region IOASISNETProvider Implementation

    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

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
            OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

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
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near me: {ex.Message}", ex);
        }
        return result;
    }

    #endregion

    #region OASISStorageProviderBase Abstract Methods Implementation

    public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        return Task.FromResult(new OASISResult<ISearchResults>
        {
            IsError = true,
            Message = "Search not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        return new OASISResult<ISearchResults>
        {
            IsError = true,
            Message = "Search not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return Task.FromResult(new OASISResult<IHolon>
        {
            IsError = true,
            Message = "LoadHolon not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return new OASISResult<IHolon>
        {
            IsError = true,
            Message = "LoadHolon not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return Task.FromResult(new OASISResult<IHolon>
        {
            IsError = true,
            Message = "LoadHolon by providerKey not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return new OASISResult<IHolon>
        {
            IsError = true,
            Message = "LoadHolon by providerKey not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadHolonsForParent not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadHolonsForParent not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadHolonsForParent by providerKey not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadHolonsForParent by providerKey not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadHolonsByMetaData not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadHolonsByMetaData not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadHolonsByMetaData not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        // RadixOASIS focuses on bridge operations - delegate storage to ProviderManager
        return HolonManager.Instance.LoadHolonsByMetaData(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        // RadixOASIS focuses on bridge operations - delegate storage to ProviderManager
        return HolonManager.Instance.LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        // RadixOASIS focuses on bridge operations - delegate storage to ProviderManager
        return HolonManager.Instance.LoadAllHolons(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
    }

    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated || _radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

            if (holon == null)
            {
                OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                return result;
            }

            // Check if OASIS blueprint is configured
            if (string.IsNullOrEmpty(_config.OasisBlueprintAddress))
            {
                // No blueprint configured - delegate to ProviderManager as fallback
                return await HolonManager.Instance.SaveHolonAsync(holon, Guid.Empty, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            }

            // Serialize holon to JSON
            string holonInfo = System.Text.Json.JsonSerializer.Serialize(holon);
            string holonId = holon.Id.ToString();

            // Get wallet for signing (use creator's wallet or holon's wallet)
            Guid creatorId = holon.CreatedByAvatarId != Guid.Empty ? holon.CreatedByAvatarId : holon.Id;
            var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(creatorId, ProviderType.Value);
            if (walletResult.IsError || walletResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet for holon creator");
                return result;
            }

            // Build transaction manifest calling OASIS blueprint's create_holon function
            var network = _config.NetworkId == 1 ? "mainnet" : "stokenet";
            var manifest = new
            {
                instructions = new[]
                {
                    new
                    {
                        kind = "CallMethod",
                        componentAddress = _config.OasisBlueprintAddress,
                        methodName = "create_holon",
                        args = new[]
                        {
                            new { kind = "String", value = holonId },
                            new { kind = "String", value = holonInfo }
                        }
                    }
                },
                blobs = new object[0]
            };

            // Get construction metadata for transaction header
            var metadataResult = await _httpClient.GetConstructionMetadataAsync(_config);
            if (metadataResult == null)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get Radix construction metadata");
                return result;
            }

            // Build transaction header
            var transactionHeader = new
            {
                networkId = _config.NetworkId,
                startEpochInclusive = metadataResult.CurrentEpoch,
                endEpochExclusive = metadataResult.CurrentEpoch + 50,
                nonce = new Random().Next(),
                notaryPublicKey = walletResult.Result.PublicKey,
                notaryIsSignatory = true,
                tipPercentage = 0
            };

            // Build complete transaction
            var transactionData = new
            {
                network = network,
                manifest = System.Text.Json.JsonSerializer.Serialize(manifest),
                header = transactionHeader,
                message = new { kind = "None" }
            };

            // Submit transaction via Radix Gateway API
            var submitResult = await HttpClientHelper.PostAsync<object, TransactionSubmitResponse>(
                _httpClient,
                $"{_config.HostUri}/core/lts/transaction/submit",
                transactionData);

            if (submitResult.IsError || submitResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to submit Radix transaction: {submitResult.Message}");
                return result;
            }

            // Store transaction hash
            if (!string.IsNullOrEmpty(submitResult.Result.TransactionHash))
            {
                holon.ProviderUniqueStorageKey[ProviderType.Value] = submitResult.Result.TransactionHash;
            }

            // Save child holons recursively if requested
            if (saveChildren && holon.Children != null && holon.Children.Any())
            {
                foreach (var child in holon.Children)
                {
                    await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth > 0 ? maxChildDepth - 1 : 0, continueOnError, saveChildrenOnProvider);
                }
            }

            result.Result = holon;
            result.IsError = false;
            result.IsSaved = true;
            result.Message = "Holon saved to Radix blueprint successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving holon to Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated || _radixService == null)
            {
                OASISErrorHandling.HandleError(ref result, "Radix provider is not activated");
                return result;
            }

            if (holons == null)
            {
                OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                return result;
            }

            var savedHolons = new List<IHolon>();
            var errors = new List<string>();

            foreach (var holon in holons)
            {
                var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                
                if (saveResult.IsError)
                {
                    errors.Add($"Failed to save holon {holon.Id}: {saveResult.Message}");
                    if (!continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Join("; ", errors));
                        return result;
                    }
                }
                else if (saveResult.Result != null)
                {
                    savedHolons.Add(saveResult.Result);
                }
            }

            result.Result = savedHolons;
            result.IsError = errors.Any();
            result.Message = errors.Any() ? string.Join("; ", errors) : $"Successfully saved {savedHolons.Count} holons to Radix";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving holons to Radix: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "SaveHolons not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
    {
        return Task.FromResult(new OASISResult<IHolon>
        {
            IsError = true,
            Message = "DeleteHolon not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IHolon> DeleteHolon(Guid id)
    {
        return new OASISResult<IHolon>
        {
            IsError = true,
            Message = "DeleteHolon not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
    {
        return Task.FromResult(new OASISResult<IHolon>
        {
            IsError = true,
            Message = "DeleteHolon by providerKey not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IHolon> DeleteHolon(string providerKey)
    {
        return new OASISResult<IHolon>
        {
            IsError = true,
            Message = "DeleteHolon by providerKey not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
    {
        return Task.FromResult(new OASISResult<bool>
        {
            IsError = true,
            Message = "Import not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        return new OASISResult<bool>
        {
            IsError = true,
            Message = "Import not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "ExportAllDataForAvatarById not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "ExportAllDataForAvatarById not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "ExportAllDataForAvatarByUsername not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "ExportAllDataForAvatarByUsername not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "ExportAllDataForAvatarByEmail not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "ExportAllDataForAvatarByEmail not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "ExportAll not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "ExportAll not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        return Task.FromResult(new OASISResult<IAvatar>
        {
            IsError = true,
            Message = "LoadAvatarByProviderKey not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        return new OASISResult<IAvatar>
        {
            IsError = true,
            Message = "LoadAvatarByProviderKey not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
    {
        return Task.FromResult(new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "LoadAvatarDetailByEmail not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
    {
        return new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "LoadAvatarDetailByEmail not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
    {
        return Task.FromResult(new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "LoadAvatarDetailByUsername not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
    {
        return new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "LoadAvatarDetailByUsername not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IAvatarDetail>>
        {
            IsError = true,
            Message = "LoadAllAvatarDetails not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
    {
        return new OASISResult<IEnumerable<IAvatarDetail>>
        {
            IsError = true,
            Message = "LoadAllAvatarDetails not implemented for RadixOASIS - use for bridge operations"
        };
    }

    #endregion
}

