using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses;
using NextGenSoftware.OASIS.API.Core.Utilities;
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
    private IRadixComponentService? _componentService;
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
            
            // Initialize component service for OASIS storage operations
            if (!string.IsNullOrEmpty(_config.ComponentAddress))
            {
                _componentService = new RadixComponentService(_config, _httpClient);
            }
            
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
        _componentService = null;
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

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        string errorMessage = "Error in LoadAvatarAsync method in RadixOASIS while loading avatar. Reason: ";

        try
        {
            if (!IsProviderActivated || _componentService == null || string.IsNullOrEmpty(_config.ComponentAddress))
            {
                OASISErrorHandling.HandleError(ref result, 
                    "RadixOASIS provider is not activated or component address is not configured. Component address is required for storage operations.");
                return result;
            }

            // Calculate entity ID from GUID
            int entityIdInt = HashUtility.GetNumericHash(id.ToString());
            ulong entityId = (ulong)entityIdInt;

            // Call component method to get avatar (read-only)
            var componentResult = await _componentService.CallComponentMethodAsync(
                _config.ComponentAddress,
                "get_avatar",
                new List<object> { entityId }
            );

            if (componentResult.IsError || string.IsNullOrEmpty(componentResult.Result))
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, componentResult.Message ?? "Avatar not found on Radix blockchain"));
                return result;
            }

            // Deserialize JSON to IAvatar
            var avatar = JsonConvert.DeserializeObject<Avatar>(componentResult.Result);
            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, "Failed to deserialize avatar JSON from Radix component"));
                return result;
            }

            result.Result = avatar;
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
    {
        return LoadAvatarAsync(id, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        string errorMessage = "Error in LoadAvatarByEmailAsync method in RadixOASIS while loading avatar. Reason: ";

        try
        {
            if (!IsProviderActivated || _componentService == null || string.IsNullOrEmpty(_config.ComponentAddress))
            {
                OASISErrorHandling.HandleError(ref result,
                    "RadixOASIS provider is not activated or component address is not configured. Component address is required for storage operations.");
                return result;
            }

            if (string.IsNullOrEmpty(email))
            {
                OASISErrorHandling.HandleError(ref result, "Email is required");
                return result;
            }

            // Call component method to get avatar by email (read-only)
            var componentResult = await _componentService.CallComponentMethodAsync(
                _config.ComponentAddress,
                "get_avatar_by_email",
                new List<object> { email }
            );

            if (componentResult.IsError || string.IsNullOrEmpty(componentResult.Result))
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, componentResult.Message ?? "Avatar not found on Radix blockchain by email"));
                return result;
            }

            // Deserialize JSON to IAvatar
            var avatar = JsonConvert.DeserializeObject<Avatar>(componentResult.Result);
            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, "Failed to deserialize avatar JSON from Radix component"));
                return result;
            }

            result.Result = avatar;
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
    {
        return LoadAvatarByEmailAsync(email, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        string errorMessage = "Error in LoadAvatarByUsernameAsync method in RadixOASIS while loading avatar. Reason: ";

        try
        {
            if (!IsProviderActivated || _componentService == null || string.IsNullOrEmpty(_config.ComponentAddress))
            {
                OASISErrorHandling.HandleError(ref result,
                    "RadixOASIS provider is not activated or component address is not configured. Component address is required for storage operations.");
                return result;
            }

            if (string.IsNullOrEmpty(username))
            {
                OASISErrorHandling.HandleError(ref result, "Username is required");
                return result;
            }

            // Call component method to get avatar by username (read-only)
            var componentResult = await _componentService.CallComponentMethodAsync(
                _config.ComponentAddress,
                "get_avatar_by_username",
                new List<object> { username }
            );

            if (componentResult.IsError || string.IsNullOrEmpty(componentResult.Result))
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, componentResult.Message ?? "Avatar not found on Radix blockchain by username"));
                return result;
            }

            // Deserialize JSON to IAvatar
            var avatar = JsonConvert.DeserializeObject<Avatar>(componentResult.Result);
            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, "Failed to deserialize avatar JSON from Radix component"));
                return result;
            }

            result.Result = avatar;
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
    {
        return LoadAvatarByUsernameAsync(username, version).Result;
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
        ArgumentNullException.ThrowIfNull(avatar);

        var result = new OASISResult<IAvatar>();
        string errorMessage = "Error in SaveAvatarAsync method in RadixOASIS while saving avatar. Reason: ";

        try
        {
            if (!IsProviderActivated || _componentService == null || string.IsNullOrEmpty(_config.ComponentAddress))
            {
                OASISErrorHandling.HandleError(ref result,
                    "RadixOASIS provider is not activated or component address is not configured. Component address is required for storage operations.");
                return result;
            }

            if (string.IsNullOrEmpty(_config.PrivateKey))
            {
                OASISErrorHandling.HandleError(ref result,
                    "Private key is required for saving avatars. Configure PrivateKey in OASIS_DNA.json.");
                return result;
            }

            // Serialize avatar to JSON
            string avatarJson = JsonConvert.SerializeObject(avatar);
            
            // Calculate entity ID from GUID
            int entityIdInt = HashUtility.GetNumericHash(avatar.Id.ToString());
            ulong entityId = (ulong)entityIdInt;

            // Get username and email for indexing (required by component)
            string username = avatar.Username ?? string.Empty;
            string email = avatar.Email ?? string.Empty;

            // Call component method to create/update avatar (transaction required)
            var componentResult = await _componentService.CallComponentMethodTransactionAsync(
                _config.ComponentAddress,
                "create_avatar",
                new List<object> { entityId, avatarJson, username, email },
                _config.PrivateKey
            );

            if (componentResult.IsError)
            {
                // If avatar already exists, try update instead
                if (componentResult.Message?.Contains("already exists") == true)
                {
                    componentResult = await _componentService.CallComponentMethodTransactionAsync(
                        _config.ComponentAddress,
                        "update_avatar",
                        new List<object> { entityId, avatarJson },
                        _config.PrivateKey
                    );
                }

                if (componentResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result,
                        string.Concat(errorMessage, componentResult.Message ?? "Failed to save avatar to Radix component"));
                    return result;
                }
            }

            result.Result = avatar;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
    {
        ArgumentNullException.ThrowIfNull(avatar);

        OASISResult<IAvatar> result = new();

        try
        {
            Task<OASISResult<IAvatar>> saveAvatarTask = SaveAvatarAsync(avatar);
            saveAvatarTask.Wait();

            if (saveAvatarTask.IsCompletedSuccessfully)
                result = saveAvatarTask.Result;
            else
                OASISErrorHandling.HandleError(ref result, saveAvatarTask.Exception?.Message, saveAvatarTask.Exception);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex.Message, ex);
        }

        return result;
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

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        string errorMessage = "Error in DeleteAvatarAsync method in RadixOASIS while deleting avatar. Reason: ";

        try
        {
            if (!IsProviderActivated || _componentService == null || string.IsNullOrEmpty(_config.ComponentAddress))
            {
                OASISErrorHandling.HandleError(ref result,
                    "RadixOASIS provider is not activated or component address is not configured. Component address is required for storage operations.");
                return result;
            }

            if (string.IsNullOrEmpty(_config.PrivateKey))
            {
                OASISErrorHandling.HandleError(ref result,
                    "Private key is required for deleting avatars. Configure PrivateKey in OASIS_DNA.json.");
                return result;
            }

            // Calculate entity ID from GUID
            int entityIdInt = HashUtility.GetNumericHash(id.ToString());
            ulong entityId = (ulong)entityIdInt;

            // Call component method to delete avatar (transaction required)
            var componentResult = await _componentService.CallComponentMethodTransactionAsync(
                _config.ComponentAddress,
                "delete_avatar",
                new List<object> { entityId },
                _config.PrivateKey
            );

            if (componentResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, componentResult.Message ?? "Failed to delete avatar from Radix component"));
                return result;
            }

            result.Result = true;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
    {
        return DeleteAvatarAsync(id, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "RadixOASIS provider is not activated");
                return result;
            }

            // Load avatar by email first (once LoadAvatarByEmail is implemented)
            // For now, we need the GUID to delete, so this will need Gateway API integration
            // TODO: Once LoadAvatarByEmail is working, use it here
            var avatarResult = await LoadAvatarByEmailAsync(email);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result,
                    avatarResult.Message ?? "Avatar not found by email");
                return result;
            }

            // Delete avatar by ID
            var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            if (deleteResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                return result;
            }

            result.Result = deleteResult.Result;
            result.IsError = false;
            result.Message = "Avatar deleted successfully by email from Radix";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Radix: {ex.Message}", ex);
        }
        
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
    {
        return DeleteAvatarByEmailAsync(email, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "RadixOASIS provider is not activated");
                return result;
            }

            // Load avatar by username first (once LoadAvatarByUsername is implemented)
            // TODO: Once LoadAvatarByUsername is working via Gateway API, use it here
            var avatarResult = await LoadAvatarByUsernameAsync(username);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result,
                    avatarResult.Message ?? "Avatar not found by username");
                return result;
            }

            // Delete avatar by ID
            var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
            if (deleteResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                return result;
            }

            result.Result = deleteResult.Result;
            result.IsError = false;
            result.Message = "Avatar deleted successfully by username from Radix";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Radix: {ex.Message}", ex);
        }
        
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
    {
        return DeleteAvatarByUsernameAsync(username, softDelete).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        string errorMessage = "Error in LoadAvatarByProviderKeyAsync method in RadixOASIS while loading avatar. Reason: ";

        try
        {
            if (!IsProviderActivated || _componentService == null || string.IsNullOrEmpty(_config.ComponentAddress))
            {
                OASISErrorHandling.HandleError(ref result,
                    "RadixOASIS provider is not activated or component address is not configured. Component address is required for storage operations.");
                return result;
            }

            if (string.IsNullOrEmpty(providerKey))
            {
                OASISErrorHandling.HandleError(ref result, "Provider key is required");
                return result;
            }

            // Note: Component doesn't have get_avatar_by_provider_key method
            // We would need to either:
            // 1. Add this method to the component, OR
            // 2. Use the providerKey as entityId if it's numeric
            // For now, try to parse providerKey as entityId (ulong)
            if (ulong.TryParse(providerKey, out ulong entityId))
            {
                // Use get_avatar with parsed entity ID
                var componentResult = await _componentService.CallComponentMethodAsync(
                    _config.ComponentAddress,
                    "get_avatar",
                    new List<object> { entityId }
                );

                if (componentResult.IsError || string.IsNullOrEmpty(componentResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result,
                        string.Concat(errorMessage, componentResult.Message ?? "Avatar not found on Radix blockchain by provider key"));
                    return result;
                }

                // Deserialize JSON to IAvatar
                var avatar = JsonConvert.DeserializeObject<Avatar>(componentResult.Result);
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        string.Concat(errorMessage, "Failed to deserialize avatar JSON from Radix component"));
                    return result;
                }

                result.Result = avatar;
                result.IsError = false;
                result.IsLoaded = true;
            }
            else
            {
                OASISErrorHandling.HandleError(ref result,
                    "Provider key must be numeric (entity ID) to load avatar. Consider using LoadAvatarByEmail or LoadAvatarByUsername instead.");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "RadixOASIS provider is not activated");
                return result;
            }

            // Load avatar by provider key first (once LoadAvatarByProviderKey is implemented)
            // For now, we need the GUID to delete, so this will need Gateway API integration
            // TODO: Once LoadAvatarByProviderKey is working, use it here
            OASISErrorHandling.HandleError(ref result,
                "DeleteAvatar by providerKey requires LoadAvatarByProviderKey to be implemented first (needs Gateway API integration)");
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Radix: {ex.Message}", ex);
        }
        
        return result;
    }

    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        return DeleteAvatarAsync(providerKey, softDelete).Result;
    }

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
    {
        return Task.FromResult(new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "LoadAvatarDetailByUsername not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
    {
        return new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "LoadAvatarDetailByUsername not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
    {
        return Task.FromResult(new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "LoadAvatarDetailByEmail not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
    {
        return new OASISResult<IAvatarDetail>
        {
            IsError = true,
            Message = "LoadAvatarDetailByEmail not implemented for RadixOASIS - use for bridge operations"
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

    // Holon Operations
    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        string errorMessage = "Error in LoadHolonAsync method in RadixOASIS while loading holon. Reason: ";

        try
        {
            if (!IsProviderActivated || _componentService == null || string.IsNullOrEmpty(_config.ComponentAddress))
            {
                OASISErrorHandling.HandleError(ref result,
                    "RadixOASIS provider is not activated or component address is not configured. Component address is required for storage operations.");
                return result;
            }

            // Calculate entity ID from GUID
            int entityIdInt = HashUtility.GetNumericHash(id.ToString());
            ulong entityId = (ulong)entityIdInt;

            // Call component method to get holon (read-only)
            var componentResult = await _componentService.CallComponentMethodAsync(
                _config.ComponentAddress,
                "get_holon",
                new List<object> { entityId }
            );

            if (componentResult.IsError || string.IsNullOrEmpty(componentResult.Result))
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, componentResult.Message ?? "Holon not found on Radix blockchain"));
                return result;
            }

            // Deserialize JSON to IHolon
            var holon = JsonConvert.DeserializeObject<Holon>(componentResult.Result);
            if (holon == null)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, "Failed to deserialize holon JSON from Radix component"));
                return result;
            }

            result.Result = holon;
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        string errorMessage = "Error in LoadHolonAsync (by providerKey) method in RadixOASIS while loading holon. Reason: ";

        try
        {
            if (!IsProviderActivated || _componentService == null || string.IsNullOrEmpty(_config.ComponentAddress))
            {
                OASISErrorHandling.HandleError(ref result,
                    "RadixOASIS provider is not activated or component address is not configured. Component address is required for storage operations.");
                return result;
            }

            if (string.IsNullOrEmpty(providerKey))
            {
                OASISErrorHandling.HandleError(ref result, "Provider key is required");
                return result;
            }

            // Call component method to get holon by provider key (read-only)
            var componentResult = await _componentService.CallComponentMethodAsync(
                _config.ComponentAddress,
                "get_holon_by_provider_key",
                new List<object> { providerKey }
            );

            if (componentResult.IsError || string.IsNullOrEmpty(componentResult.Result))
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, componentResult.Message ?? "Holon not found on Radix blockchain by provider key"));
                return result;
            }

            // Deserialize JSON to IHolon
            var holon = JsonConvert.DeserializeObject<Holon>(componentResult.Result);
            if (holon == null)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, "Failed to deserialize holon JSON from Radix component"));
                return result;
            }

            result.Result = holon;
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        ArgumentNullException.ThrowIfNull(holon);

        var result = new OASISResult<IHolon>();
        string errorMessage = "Error in SaveHolonAsync method in RadixOASIS while saving holon. Reason: ";

        try
        {
            if (!IsProviderActivated || _componentService == null || string.IsNullOrEmpty(_config.ComponentAddress))
            {
                OASISErrorHandling.HandleError(ref result,
                    "RadixOASIS provider is not activated or component address is not configured. Component address is required for storage operations.");
                return result;
            }

            if (string.IsNullOrEmpty(_config.PrivateKey))
            {
                OASISErrorHandling.HandleError(ref result,
                    "Private key is required for saving holons. Configure PrivateKey in OASIS_DNA.json.");
                return result;
            }

            // Serialize holon to JSON
            string holonJson = JsonConvert.SerializeObject(holon);
            
            // Calculate entity ID from GUID
            int entityIdInt = HashUtility.GetNumericHash(holon.Id.ToString());
            ulong entityId = (ulong)entityIdInt;

            // Get provider key for indexing (use ProviderUniqueStorageKey if available, otherwise use entity ID as string)
            string providerKey = string.Empty;
            if (holon.ProviderUniqueStorageKey != null && 
                holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.RadixOASIS))
            {
                providerKey = holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.RadixOASIS];
            }
            else
            {
                providerKey = entityId.ToString(); // Fallback to entity ID as string
            }

            // Call component method to create/update holon (transaction required)
            var componentResult = await _componentService.CallComponentMethodTransactionAsync(
                _config.ComponentAddress,
                "create_holon",
                new List<object> { entityId, holonJson, providerKey },
                _config.PrivateKey
            );

            if (componentResult.IsError)
            {
                // If holon already exists, try update instead
                if (componentResult.Message?.Contains("already exists") == true)
                {
                    componentResult = await _componentService.CallComponentMethodTransactionAsync(
                        _config.ComponentAddress,
                        "update_holon",
                        new List<object> { entityId, holonJson },
                        _config.PrivateKey
                    );
                }

                if (componentResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result,
                        string.Concat(errorMessage, componentResult.Message ?? "Failed to save holon to Radix component"));
                    return result;
                }
            }

            result.Result = holon;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        ArgumentNullException.ThrowIfNull(holon);

        OASISResult<IHolon> result = new();

        try
        {
            Task<OASISResult<IHolon>> saveHolonTask = SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            saveHolonTask.Wait();

            if (saveHolonTask.IsCompletedSuccessfully)
                result = saveHolonTask.Result;
            else
                OASISErrorHandling.HandleError(ref result, saveHolonTask.Exception?.Message, saveHolonTask.Exception);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex.Message, ex);
        }

        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        ArgumentNullException.ThrowIfNull(holons);

        var result = new OASISResult<IEnumerable<IHolon>>();
        string errorMessage = "Error in SaveHolonsAsync method in RadixOASIS while saving holons. Reason: ";

        try
        {
            if (!IsProviderActivated || _componentService == null || string.IsNullOrEmpty(_config.ComponentAddress))
            {
                OASISErrorHandling.HandleError(ref result,
                    "RadixOASIS provider is not activated or component address is not configured. Component address is required for storage operations.");
                return result;
            }

            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> saveHolonResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (saveHolonResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, saveHolonResult.DetailedMessage));

                    if (!continueOnError) break;
                }
            }

            result.Result = holons;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
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

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
    {
        var result = new OASISResult<IHolon>();
        
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "RadixOASIS provider is not activated");
                return result;
            }

            // Load holon by provider key first (once Gateway API integration is complete)
            var holonResult = await LoadHolonAsync(providerKey);
            if (holonResult.IsError || holonResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result,
                    holonResult.Message ?? "Holon not found by provider key");
                return result;
            }

            // Delete holon by ID
            var deleteResult = await DeleteHolonAsync(holonResult.Result.Id);
            if (deleteResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {deleteResult.Message}");
                return result;
            }

            result.Result = deleteResult.Result;
            result.IsError = false;
            result.Message = "Holon deleted successfully by provider key from Radix";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Radix: {ex.Message}", ex);
        }
        
        return result;
    }

    public override OASISResult<IHolon> DeleteHolon(string providerKey)
    {
        return DeleteHolonAsync(providerKey).Result;
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
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadHolonsByMetaData not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadHolonsByMetaData (dictionary) not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadHolonsByMetaData (dictionary) not implemented for RadixOASIS - use for bridge operations"
        };
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return Task.FromResult(new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadAllHolons not implemented for RadixOASIS - use for bridge operations"
        });
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "LoadAllHolons not implemented for RadixOASIS - use for bridge operations"
        };
    }

    // Search Operations
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

    // Import/Export Operations
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

    #endregion

    #region IOASISNETProvider Implementation

    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
    {
        return new OASISResult<IEnumerable<IAvatar>>
        {
            IsError = true,
            Message = "GetAvatarsNearMe not implemented for RadixOASIS - geo-spatial queries not supported"
        };
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
    {
        return new OASISResult<IEnumerable<IHolon>>
        {
            IsError = true,
            Message = "GetHolonsNearMe not implemented for RadixOASIS - geo-spatial queries not supported"
        };
    }

    #endregion

    #region IOASISSmartContractProvider Implementation

    // IOASISSmartContractProvider is currently empty (no methods defined)
    // This section reserved for future smart contract operations

    #endregion

    #region IOASISBlockchainStorageProvider Implementation (Additional Methods)

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
    {
        return new OASISResult<ITransactionResponse>
        {
            IsError = true,
            Message = "SendToken not implemented for RadixOASIS - use SendTransaction for XRD transfers"
        };
    }

    public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        return Task.FromResult(new OASISResult<ITransactionResponse>
        {
            IsError = true,
            Message = "SendToken not implemented for RadixOASIS - use SendTransaction for XRD transfers"
        });
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    {
        return new OASISResult<ITransactionResponse>
        {
            IsError = true,
            Message = "MintToken not implemented for RadixOASIS - Radix uses Scrypto/Manifests for token operations"
        };
    }

    public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        return Task.FromResult(new OASISResult<ITransactionResponse>
        {
            IsError = true,
            Message = "MintToken not implemented for RadixOASIS - Radix uses Scrypto/Manifests for token operations"
        });
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
    {
        return new OASISResult<ITransactionResponse>
        {
            IsError = true,
            Message = "BurnToken not implemented for RadixOASIS - Radix uses Scrypto/Manifests for token operations"
        };
    }

    public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        return Task.FromResult(new OASISResult<ITransactionResponse>
        {
            IsError = true,
            Message = "BurnToken not implemented for RadixOASIS - Radix uses Scrypto/Manifests for token operations"
        });
    }

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
    {
        return new OASISResult<ITransactionResponse>
        {
            IsError = true,
            Message = "LockToken not implemented for RadixOASIS - Radix uses Scrypto/Manifests for token operations"
        };
    }

    public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
    {
        return Task.FromResult(new OASISResult<ITransactionResponse>
        {
            IsError = true,
            Message = "LockToken not implemented for RadixOASIS - Radix uses Scrypto/Manifests for token operations"
        });
    }

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
    {
        return new OASISResult<ITransactionResponse>
        {
            IsError = true,
            Message = "UnlockToken not implemented for RadixOASIS - Radix uses Scrypto/Manifests for token operations"
        };
    }

    public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        return Task.FromResult(new OASISResult<ITransactionResponse>
        {
            IsError = true,
            Message = "UnlockToken not implemented for RadixOASIS - Radix uses Scrypto/Manifests for token operations"
        });
    }

    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
    {
        return new OASISResult<double>
        {
            IsError = true,
            Message = "GetBalance not implemented for RadixOASIS - use GetAccountBalanceAsync for balance queries"
        };
    }

    public Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        return Task.FromResult(new OASISResult<double>
        {
            IsError = true,
            Message = "GetBalance not implemented for RadixOASIS - use GetAccountBalanceAsync for balance queries"
        });
    }

    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
    {
        return new OASISResult<IList<IWalletTransaction>>
        {
            IsError = true,
            Message = "GetTransactions not implemented for RadixOASIS - use RadixService for transaction queries"
        };
    }

    public Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
    {
        return Task.FromResult(new OASISResult<IList<IWalletTransaction>>
        {
            IsError = true,
            Message = "GetTransactions not implemented for RadixOASIS - use RadixService for transaction queries"
        });
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
    {
        return new OASISResult<IKeyPairAndWallet>
        {
            IsError = true,
            Message = "GenerateKeyPair not implemented for RadixOASIS - use CreateAccountAsync for account generation"
        };
    }

    public Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
    {
        return Task.FromResult(new OASISResult<IKeyPairAndWallet>
        {
            IsError = true,
            Message = "GenerateKeyPair not implemented for RadixOASIS - use CreateAccountAsync for account generation"
        });
    }

    #endregion
}

