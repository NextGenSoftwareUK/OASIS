using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                int removingDays = OASISDNA.OASIS.Security.RemoveOldRefreshTokensAfterXDays;
                int removeQty = avatar.RefreshTokens.RemoveAll(token => (DateTime.Today - token.Created).TotalDays > removingDays);

                result = await SaveAvatarForProviderAsync(avatar, result, SaveMode.FirstSaveAttempt, providerType);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && (autoFailOverMode == AutoFailOverMode.UseGlobalDefaultInOASISDNA && ProviderManager.IsAutoFailOverEnabled) || autoReplicationMode == AutoReplicationMode.True)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await SaveAvatarForProviderAsync(avatar, result, SaveMode.AutoFailOver, type.Value);

                            if (!result.IsError && result.Result != null)
                            {
                                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar ", avatar.Name, " with id ", avatar.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Saved.";

                    //TODO: Even if all providers failed above, we should still attempt again in a background thread for a fixed number of attempts (default 3) every X seconds (default 5) configured in OASISDNA.json.
                    //TODO: Auto-Failover should also re-try in a background thread after reporting the intial error above and then report after the retries either failed or succeeded later...
                    if ((autoReplicationMode == AutoReplicationMode.UseGlobalDefaultInOASISDNA && ProviderManager.IsAutoReplicationEnabled) || autoReplicationMode == AutoReplicationMode.True)
                        result = await AutoReplicateAvatarAsync(avatar, result, previousProviderType, waitForAutoReplicationResult);
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatar> SaveAvatar(IAvatar avatar, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                int removingDays = OASISDNA.OASIS.Security.RemoveOldRefreshTokensAfterXDays;
                int removeQty = avatar.RefreshTokens.RemoveAll(token => (DateTime.Today - token.Created).TotalDays > removingDays);
                //Dictionary<ProviderType, List<IProviderWallet>> wallets = WalletManager.Instance.CopyProviderWallets(avatar.ProviderWallets);

                result = SaveAvatarForProvider(avatar, result, SaveMode.FirstSaveAttempt, providerType);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && (autoFailOverMode == AutoFailOverMode.UseGlobalDefaultInOASISDNA && ProviderManager.IsAutoFailOverEnabled) || autoReplicationMode == AutoReplicationMode.True)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            //avatar.ProviderWallets = WalletManager.Instance.CopyProviderWallets(wallets);
                            result = SaveAvatarForProvider(avatar, result, SaveMode.AutoFailOver, type.Value);

                            if (!result.IsError && result.Result != null)
                            {
                                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar ", avatar.Name, " with id ", avatar.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Saved.";

                    //TODO: Need to move into background thread ASAP!
                    //TODO: Even if all providers failed above, we should still attempt again in a background thread for a fixed number of attempts (default 3) every X seconds (default 5) configured in OASISDNA.json.
                    //TODO: Auto-Failover should also re-try in a background thread after reporting the intial error above and then report after the retries either failed or succeeded later...
                    if ((autoReplicationMode == AutoReplicationMode.UseGlobalDefaultInOASISDNA && ProviderManager.IsAutoReplicationEnabled) || autoReplicationMode == AutoReplicationMode.True)
                        result = AutoReplicateAvatar(avatar, result, previousProviderType, waitForAutoReplicationResult);
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = SaveAvatarDetailForProvider(avatar, result, SaveMode.FirstSaveAttempt, providerType);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && (autoFailOverMode == AutoFailOverMode.UseGlobalDefaultInOASISDNA && ProviderManager.IsAutoFailOverEnabled) || autoReplicationMode == AutoReplicationMode.True)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = SaveAvatarDetailForProvider(avatar, result, SaveMode.AutoFailOver, type.Value);

                            if (!result.IsError && result.Result != null)
                            {
                                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar detail ", avatar.Name, " with id ", avatar.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", previousProviderType, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Saved.";

                    //TODO: Need to move into background thread ASAP!
                    if ((autoReplicationMode == AutoReplicationMode.UseGlobalDefaultInOASISDNA && ProviderManager.IsAutoReplicationEnabled) || autoReplicationMode == AutoReplicationMode.True)
                        result = AutoReplicateAvatarDetail(avatar, result, previousProviderType, waitForAutoReplicationResult);
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar detail ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar, AutoReplicationMode autoReplicationMode = AutoReplicationMode.UseGlobalDefaultInOASISDNA, AutoFailOverMode autoFailOverMode = AutoFailOverMode.UseGlobalDefaultInOASISDNA, AutoLoadBalanceMode autoLoadBalanceMode = AutoLoadBalanceMode.UseGlobalDefaultInOASISDNA, bool waitForAutoReplicationResult = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await SaveAvatarDetailForProviderAsync(avatar, result, SaveMode.FirstSaveAttempt, providerType);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && (autoFailOverMode == AutoFailOverMode.UseGlobalDefaultInOASISDNA && ProviderManager.IsAutoFailOverEnabled) || autoReplicationMode == AutoReplicationMode.True)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await SaveAvatarDetailForProviderAsync(avatar, result, SaveMode.AutoFailOver, type.Value);

                            if (!result.IsError && result.Result != null)
                            {
                                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save the avatar detail ", avatar.Name, " with id ", avatar.Id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail ", avatar.Name, " with id ", avatar.Id, " successfully saved for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Saved.";

                    //TODO: Need to move into background thread ASAP!
                    if ((autoReplicationMode == AutoReplicationMode.UseGlobalDefaultInOASISDNA && ProviderManager.IsAutoReplicationEnabled) || autoReplicationMode == AutoReplicationMode.True)
                        result = await AutoReplicateAvatarDetailAsync(avatar, result, previousProviderType, waitForAutoReplicationResult);
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured saving avatar detail ", avatar.Name, " with id ", avatar.Id, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }
    }
}