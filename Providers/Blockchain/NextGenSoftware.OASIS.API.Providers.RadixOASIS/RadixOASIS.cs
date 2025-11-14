using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

/// <summary>
/// OASIS Provider for Radix DLT blockchain with cross-chain bridge support
/// </summary>
public class RadixOASIS : OASISStorageProviderBase, IOASISStorageProvider, 
    IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNETProvider
{
    private IRadixService? _radixService;
    private readonly RadixOASISConfig _config;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Gets the Radix bridge service for cross-chain operations
    /// </summary>
    public IRadixService? RadixBridgeService => _radixService;

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
        _radixService = null;
        IsProviderActivated = false;
        return await Task.FromResult(new OASISResult<bool>(true));
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        _radixService = null;
        IsProviderActivated = false;
        return new OASISResult<bool>(true);
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

