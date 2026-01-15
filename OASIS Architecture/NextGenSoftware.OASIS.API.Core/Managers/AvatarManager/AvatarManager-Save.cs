using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            // HyperDrive v2 routing with safe fallback to legacy
            try
            {
                var dna = OASISDNAManager.OASISDNA;
                if (dna?.OASIS?.HyperDriveMode == "OASISHyperDrive2")
                {
                    var hyperDrive = new NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive();
                    var request = new StorageOperationRequest
                    {
                        Operation = "SaveAvatar",
                        AvatarId = avatar.Id,
                        Payload = avatar,
                        PreferredProvider = providerType
                    };

                    var hdResult = await hyperDrive.RouteRequestAsync<IAvatar>(request, LoadBalancingStrategy.Auto);
                    if (hdResult != null && !hdResult.IsError && hdResult.Result != null)
                        return hdResult;
                }
            }
            catch { /* fallback to legacy */ }
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                //Make sure the password is not blank before saving!
                if (string.IsNullOrEmpty(avatar.Password))
                {
                    OASISResult<IAvatar> avatarResult = await LoadAvatarAsync(avatar.Id, false, false, providerType);

                    if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                        avatar = avatarResult.Result;
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error occured in SaveAvatarAsync loading avatar. Reason: {avatarResult.Message}");
                        return result;
                    }
                }

                avatar.ModifiedDate = DateTime.Now;
                avatar.ModifiedByAvatarId = avatar.Id;

                int removingDays = OASISDNA.OASIS.Security.RemoveOldRefreshTokensAfterXDays;
                int removeQty = 0;

                if (avatar.RefreshTokens != null)
                    removeQty = avatar.RefreshTokens.RemoveAll(token => (DateTime.Today - token.Created).TotalDays > removingDays);

                result = await SaveAvatarForProviderAsync(avatar, result, SaveMode.FirstSaveAttempt, providerType);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && (autoFailOverMode == AutoFailOverMode.UseGlobalDefaultInOASISDNA && ProviderManager.Instance.IsAutoFailOverEnabled) || autoReplicationMode == AutoReplicationMode.True)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await SaveAvatarForProviderAsync(avatar, result, SaveMode.AutoFailOver, type.Value);

                            if (!result.IsError && result.Result != null)
                            {
                                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar ", avatar.Name, " with id ", avatar.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Saved.";

                    //TODO: Even if all providers failed above, we should still attempt again in a background thread for a fixed number of attempts (default 3) every X seconds (default 5) configured in OASISDNA.json.
                    //TODO: Auto-Failover should also re-try in a background thread after reporting the intial error above and then report after the retries either failed or succeeded later...
                    if ((autoReplicationMode == AutoReplicationMode.UseGlobalDefaultInOASISDNA && ProviderManager.Instance.IsAutoReplicationEnabled) || autoReplicationMode == AutoReplicationMode.True)
                        result = await AutoReplicateAvatarAsync(avatar, result, previousProviderType, waitForAutoReplicationResult);
                }

                await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatar> SaveAvatar(IAvatar avatar, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                //Make sure the password is not blank before saving!
                if (string.IsNullOrEmpty(avatar.Password))
                {
                    OASISResult<IAvatar> avatarResult = LoadAvatar(avatar.Id, false, false, providerType);

                    if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                        avatar = avatarResult.Result;
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error occured in SaveAvatarAsync loading avatar. Reason: {avatarResult.Message}");
                        return result;
                    }
                }

                avatar.ModifiedDate = DateTime.Now;
                avatar.ModifiedByAvatarId = avatar.Id;

                int removingDays = OASISDNA.OASIS.Security.RemoveOldRefreshTokensAfterXDays;
                int removeQty = 0;

                if (avatar.RefreshTokens != null)
                    removeQty = avatar.RefreshTokens.RemoveAll(token => (DateTime.Today - token.Created).TotalDays > removingDays);

                result = SaveAvatarForProvider(avatar, result, SaveMode.FirstSaveAttempt, providerType);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && (autoFailOverMode == AutoFailOverMode.UseGlobalDefaultInOASISDNA && ProviderManager.Instance.IsAutoFailOverEnabled) || autoReplicationMode == AutoReplicationMode.True)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            //avatar.ProviderWallets = WalletManager.Instance.CopyProviderWallets(wallets);
                            result = SaveAvatarForProvider(avatar, result, SaveMode.AutoFailOver, type.Value);

                            if (!result.IsError && result.Result != null)
                            {
                                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar ", avatar.Name, " with id ", avatar.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Saved.";

                    //TODO: Need to move into background thread ASAP!
                    //TODO: Even if all providers failed above, we should still attempt again in a background thread for a fixed number of attempts (default 3) every X seconds (default 5) configured in OASISDNA.json.
                    //TODO: Auto-Failover should also re-try in a background thread after reporting the intial error above and then report after the retries either failed or succeeded later...
                    if ((autoReplicationMode == AutoReplicationMode.UseGlobalDefaultInOASISDNA && ProviderManager.Instance.IsAutoReplicationEnabled) || autoReplicationMode == AutoReplicationMode.True)
                        result = AutoReplicateAvatar(avatar, result, previousProviderType, waitForAutoReplicationResult);

                    // Auto-Load Balancing: Distribute load across multiple providers
                    if ((autoLoadBalanceMode == AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA && ProviderManager.Instance.IsAutoLoadBalanceEnabled) || autoLoadBalanceMode == AutoLoadBalanceMode.True)
                    {
                        var loadBalancedProvider = ProviderManager.Instance.SelectOptimalProviderForLoadBalancing();
                        if (loadBalancedProvider.Value != currentProviderType)
                        {
                            // Save to load-balanced provider for better performance distribution
                            var loadBalanceResult = SaveAvatarForProvider(avatar, new OASISResult<IAvatar>(), SaveMode.AutoLoadBalance, loadBalancedProvider.Value);
                            
                            if (!loadBalanceResult.IsError && loadBalanceResult.Result != null)
                            {
                                result.InnerMessages.Add($"Auto-load balanced to {loadBalancedProvider.Name} provider for optimal performance");
                            }
                        }
                    }
                }

                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                avatar.ModifiedDate = DateTime.Now;
                avatar.ModifiedByAvatarId = avatar.Id;

                result = SaveAvatarDetailForProvider(avatar, result, SaveMode.FirstSaveAttempt, providerType);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && (autoFailOverMode == AutoFailOverMode.UseGlobalDefaultInOASISDNA && ProviderManager.Instance.IsAutoFailOverEnabled) || autoReplicationMode == AutoReplicationMode.True)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = SaveAvatarDetailForProvider(avatar, result, SaveMode.AutoFailOver, type.Value);

                            if (!result.IsError && result.Result != null)
                            {
                                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar detail ", avatar.Name, " with id ", avatar.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", previousProviderType, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Saved.";

                    //TODO: Need to move into background thread ASAP!
                    if ((autoReplicationMode == AutoReplicationMode.UseGlobalDefaultInOASISDNA && ProviderManager.Instance.IsAutoReplicationEnabled) || autoReplicationMode == AutoReplicationMode.True)
                        result = AutoReplicateAvatarDetail(avatar, result, previousProviderType, waitForAutoReplicationResult);
                }

                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar detail ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                avatar.ModifiedDate = DateTime.Now;
                avatar.ModifiedByAvatarId = avatar.Id;

                result = await SaveAvatarDetailForProviderAsync(avatar, result, SaveMode.FirstSaveAttempt, providerType);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && (autoFailOverMode == AutoFailOverMode.UseGlobalDefaultInOASISDNA && ProviderManager.Instance.IsAutoFailOverEnabled) || autoReplicationMode == AutoReplicationMode.True)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await SaveAvatarDetailForProviderAsync(avatar, result, SaveMode.AutoFailOver, type.Value);

                            if (!result.IsError && result.Result != null)
                            {
                                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar detail ", avatar.Name, " with id ", avatar.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Saved.";

                    //TODO: Need to move into background thread ASAP!
                    if ((autoReplicationMode == AutoReplicationMode.UseGlobalDefaultInOASISDNA && ProviderManager.Instance.IsAutoReplicationEnabled) || autoReplicationMode == AutoReplicationMode.True)
                        result = await AutoReplicateAvatarDetailAsync(avatar, result, previousProviderType, waitForAutoReplicationResult);
                }

                await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar detail ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        /// <summary>
        /// Background retry logic for failed avatar save operations
        /// </summary>
        private async Task RetryAvatarSaveInBackgroundAsync(IAvatar avatar, AutoReplicationMode autoReplicationMode, AutoFailOverMode autoFailOverMode, AutoLoadBalanceMode autoLoadBalanceMode, ProviderType providerType)
        {
            try
            {
                var config = OASISDNAManager.OASISDNA;
                var maxRetryAttempts = 3; // config?.RetrySettings?.MaxRetryAttempts ?? 3;
                var retryIntervalSeconds = 5; // config?.RetrySettings?.RetryIntervalSeconds ?? 5;

                for (int attempt = 1; attempt <= maxRetryAttempts; attempt++)
                {
                    await Task.Delay(retryIntervalSeconds * 1000);

                    try
                    {
                        var retryResult = await SaveAvatarAsync(avatar, autoReplicationMode, autoFailOverMode, autoLoadBalanceMode, false, providerType);
                        
                        if (!retryResult.IsError)
                        {
                            Console.WriteLine($"Background retry succeeded for avatar {avatar.Name} on attempt {attempt}");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Background retry attempt {attempt} failed for avatar {avatar.Name}: {ex.Message}");
                    }
                }

                Console.WriteLine($"All background retry attempts failed for avatar {avatar.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background retry process failed for avatar {avatar.Name}: {ex.Message}");
            }
        }
    }
}