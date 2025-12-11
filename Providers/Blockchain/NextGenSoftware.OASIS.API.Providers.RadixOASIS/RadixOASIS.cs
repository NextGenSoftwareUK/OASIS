using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Oracle;

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
        this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

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
    public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
    }

    /// <summary>
    /// Sends a transaction on the Radix network asynchronously
    /// </summary>
    public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        var result = new OASISResult<ITransactionRespone>();
        
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
            result.Result = new TransactionRespone
            {
                TransactionResult = depositResult.Result.TransactionHash ?? depositResult.Result.IntentHash ?? "Unknown"
            };
            
            result.IsError = false;
            return result;
        }
        catch (Exception ex)
        {
            return OASISErrorHandling.HandleError<ITransactionRespone>(ref result,
                $"Error sending transaction: {ex.Message}", ex);
        }
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

    public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
    {
        return Task.FromResult(new OASISResult<IAvatar>
        {
            IsError = true,
            Message = "SaveAvatar not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
    {
        return new OASISResult<IAvatar>
        {
            IsError = true,
            Message = "SaveAvatar not implemented for RadixOASIS - use for bridge operations"
        };
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

    #endregion
}

