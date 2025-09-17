using System;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        public OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                OASISResult<IAvatar> avatarResult = LoadAvatar(id, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.DeletedDate == DateTime.MinValue)
                    {
                        result = DeleteAvatarForProvider(id, result, SaveMode.FirstSaveAttempt, softDelete, providerType);
                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                        if ((!result.Result || result.IsError) && ProviderManager.Instance.IsAutoFailOverEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                            {
                                if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    result = DeleteAvatarForProvider(id, result, SaveMode.AutoFailOver, softDelete, type.Value);

                                    if (!result.IsError && result.Result)
                                    {
                                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                        break;
                                    }
                                }
                            }
                        }

                        if (result.IsError || !result.Result)
                            OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                        else
                        {
                            result.IsSaved = true;

                            if (result.WarningCount > 0)
                                OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with id ", id, " was successfully deleted for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                            else
                                result.Message = "Avatar Successfully Deleted.";

                            //TODO: Need to move into background thread ASAP!
                            if (ProviderManager.Instance.IsAutoReplicationEnabled)
                            {
                                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                                {
                                    if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                        result = DeleteAvatarForProvider(id, result, SaveMode.AutoReplication, softDelete, type.Value);
                                }

                                if (result.WarningCount > 0)
                                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with id ", id, " was successfully deleted for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                                else
                                    LoggingManager.Log("Avatar Successfully Deleted/Replicated", LogType.Info, ref result, true, false);
                            }
                        }

                        ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"The avatar with id {id} failed to load in DeleteAvatar method in AvatarManagaer. Reason: {avatarResult.Message}.", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with id ", id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                OASISResult<IAvatar> avatarResult = await LoadAvatarAsync(id, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.DeletedDate == DateTime.MinValue)
                    {
                        result = await DeleteAvatarForProviderAsync(id, result, SaveMode.FirstSaveAttempt, softDelete, providerType);
                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                        if ((!result.Result || result.IsError) && ProviderManager.Instance.IsAutoFailOverEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                            {
                                if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    result = await DeleteAvatarForProviderAsync(id, result, SaveMode.AutoFailOver, softDelete, type.Value);

                                    if (!result.IsError && result.Result)
                                    {
                                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                        break;
                                    }
                                }
                            }
                        }

                        if (result.IsError || !result.Result)
                            OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                        else
                        {
                            result.IsSaved = true;

                            if (result.WarningCount > 0)
                                OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with id ", id, " was successfully deleted for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                            else
                                result.Message = "Avatar Successfully Deleted.";

                            //TODO: Need to move into background thread ASAP!
                            if (ProviderManager.Instance.IsAutoReplicationEnabled)
                            {
                                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                                {
                                    if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                        result = await DeleteAvatarForProviderAsync(id, result, SaveMode.AutoReplication, softDelete, type.Value);
                                }

                                if (result.WarningCount > 0)
                                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with id ", id, " was successfully deleted for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                                else
                                    LoggingManager.Log("Avatar Successfully Deleted/Replicated", LogType.Info, ref result, true, false);
                            }
                        }

                        await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"The avatar with id {id} failed to load in DeleteAvatarAsync method in AvatarManagaer. Reason: {avatarResult.Message}.", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with id ", id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string userName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                OASISResult<IAvatar> avatarResult = await LoadAvatarAsync(userName, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.DeletedDate == DateTime.MinValue)
                    {
                        result = await DeleteAvatarByUsernameForProviderAsync(userName, result, SaveMode.FirstSaveAttempt, softDelete, providerType);
                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                        if ((!result.Result || result.IsError) && ProviderManager.Instance.IsAutoFailOverEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                            {
                                if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    result = await DeleteAvatarByUsernameForProviderAsync(userName, result, SaveMode.AutoReplication, softDelete, type.Value);

                                    if (!result.IsError && result.Result)
                                    {
                                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                        break;
                                    }
                                }
                            }
                        }

                        if (result.IsError || !result.Result)
                            OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with username ", userName, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                        else
                        {
                            result.IsSaved = true;

                            if (result.WarningCount > 0)
                                OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with username ", userName, " was successfully deleted for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                            else
                                result.Message = "Avatar Successfully Deleted.";

                            //TODO: Need to move into background thread ASAP!
                            if (ProviderManager.Instance.IsAutoReplicationEnabled)
                            {
                                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                                {
                                    if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                        result = await DeleteAvatarByUsernameForProviderAsync(userName, result, SaveMode.AutoReplication, softDelete, type.Value);
                                }

                                if (result.WarningCount > 0)
                                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with username ", userName, " was successfully deleted for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                                else
                                    LoggingManager.Log("Avatar Successfully Deleted/Replicated", LogType.Info, ref result, true, false);
                            }
                        }

                        await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"The avatar with username {userName} failed to load in DeleteAvatarByUsernameAsync method in AvatarManagaer. Reason: {avatarResult.Message}.", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with userName ", userName, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }

        public OASISResult<bool> DeleteAvatarByUsername(string userName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                OASISResult<IAvatar> avatarResult = LoadAvatar(userName, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.DeletedDate == DateTime.MinValue)
                    {
                        result = DeleteAvatarByUsernameForProvider(userName, result, SaveMode.FirstSaveAttempt, softDelete, providerType);
                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                        if ((!result.Result || result.IsError) && ProviderManager.Instance.IsAutoFailOverEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                            {
                                if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    result = DeleteAvatarByUsernameForProvider(userName, result, SaveMode.AutoReplication, softDelete, type.Value);

                                    if (!result.IsError && result.Result)
                                    {
                                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                        break;
                                    }
                                }
                            }
                        }

                        if (result.IsError || !result.Result)
                            OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with username ", userName, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                        else
                        {
                            result.IsSaved = true;

                            if (result.WarningCount > 0)
                                OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with username ", userName, " was successfully deleted for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                            else
                                result.Message = "Avatar Successfully Deleted.";

                            //TODO: Need to move into background thread ASAP!
                            if (ProviderManager.Instance.IsAutoReplicationEnabled)
                            {
                                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                                {
                                    if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                        result = DeleteAvatarByUsernameForProvider(userName, result, SaveMode.AutoReplication, softDelete, type.Value);
                                }

                                if (result.WarningCount > 0)
                                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with username ", userName, " was successfully deleted for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                                else
                                    LoggingManager.Log("Avatar Successfully Deleted/Replicated", LogType.Info, ref result, true, false);
                            }
                        }

                        ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"The avatar with username {userName} failed to load in DeleteAvatarByUsername method in AvatarManagaer. Reason: {avatarResult.Message}.", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with userName ", userName, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                OASISResult<IAvatar> avatarResult = await LoadAvatarByEmailAsync(email, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.DeletedDate == DateTime.MinValue)
                    {
                        result = await DeleteAvatarByEmailForProviderAsync(email, result, SaveMode.FirstSaveAttempt, softDelete, providerType);
                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                        if ((!result.Result || result.IsError) && ProviderManager.Instance.IsAutoFailOverEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                            {
                                if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    result = await DeleteAvatarByEmailForProviderAsync(email, result, SaveMode.AutoFailOver, softDelete, type.Value);

                                    if (!result.IsError && result.Result)
                                    {
                                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                        break;
                                    }
                                }
                            }
                        }

                        if (result.IsError || !result.Result)
                            OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with email ", email, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                        else
                        {
                            result.IsSaved = true;

                            if (result.WarningCount > 0)
                                OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with email ", email, " was successfully deleted for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                            else
                                result.Message = "Avatar Successfully Deleted.";

                            //TODO: Need to move into background thread ASAP!
                            if (ProviderManager.Instance.IsAutoReplicationEnabled)
                            {
                                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                                {
                                    if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                        result = await DeleteAvatarByEmailForProviderAsync(email, result, SaveMode.AutoReplication, softDelete, type.Value);
                                }

                                if (result.WarningCount > 0)
                                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with email ", email, " was successfully deleted for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                                else
                                    LoggingManager.Log("Avatar Successfully Deleted/Replicated", LogType.Info, ref result, true, false);
                            }
                        }

                        await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"The avatar with email {email} failed to load in DeleteAvatarByEmailAsync method in AvatarManagaer. Reason: {avatarResult.Message}.", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with email ", email, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }

        public OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                OASISResult<IAvatar> avatarResult = LoadAvatarByEmail(email, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.DeletedDate == DateTime.MinValue)
                    {
                        result = DeleteAvatarByEmailForProvider(email, result, SaveMode.FirstSaveAttempt, softDelete, providerType);
                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                        if ((!result.Result || result.IsError) && ProviderManager.Instance.IsAutoFailOverEnabled)
                        {
                            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                            {
                                if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                {
                                    result = DeleteAvatarByEmailForProvider(email, result, SaveMode.AutoFailOver, softDelete, type.Value);

                                    if (!result.IsError && result.Result)
                                    {
                                        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                        break;
                                    }
                                }
                            }
                        }

                        if (result.IsError || !result.Result)
                            OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar with email ", email, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                        else
                        {
                            result.IsSaved = true;

                            if (result.WarningCount > 0)
                                OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with email ", email, " was successfully deleted for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                            else
                                result.Message = "Avatar Successfully Deleted.";

                            //TODO: Need to move into background thread ASAP!
                            if (ProviderManager.Instance.IsAutoReplicationEnabled)
                            {
                                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                                {
                                    if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                        result = DeleteAvatarByEmailForProvider(email, result, SaveMode.AutoReplication, softDelete, type.Value);
                                }

                                if (result.WarningCount > 0)
                                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with email ", email, " was successfully deleted for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                                else
                                    LoggingManager.Log("Avatar Successfully Deleted/Replicated", LogType.Info, ref result, true, false);
                            }
                        }

                        ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"The avatar with email {email} failed to load in DeleteAvatarByEmail method in AvatarManagaer. Reason: {avatarResult.Message}.", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar with email ", email, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }

        /*
        public async Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await DeleteAvatarDetailForProviderAsync(id, result, SaveMode.FirstSaveAttempt, softDelete, providerType);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if ((!result.Result || result.IsError) && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await DeleteAvatarDetailForProviderAsync(id, result, SaveMode.AutoFailOver, softDelete, type.Value);

                            if (!result.IsError && result.Result)
                            {
                                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.IsError || !result.Result)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar detail with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with id ", id, " was successfully deleted for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Deleted.";

                    //TODO: Need to move into background thread ASAP!
                    if (ProviderManager.Instance.IsAutoReplicationEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                        {
                            if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                result = await DeleteAvatarDetailForProviderAsync(id, result, SaveMode.AutoReplication, softDelete, type.Value);
                        }

                        if (result.WarningCount > 0)
                            OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with id ", id, " was successfully deleted for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                        else
                            LoggingManager.Log("Avatar Detail Successfully Deleted/Replicated", LogType.Info, ref result, true, false);
                    }
                }

                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar detail with id ", id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string userName, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await DeleteAvatarDetailByUsernameForProviderAsync(userName, result, SaveMode.FirstSaveAttempt, softDelete, providerType);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if ((!result.Result || result.IsError) && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await DeleteAvatarDetailByUsernameForProviderAsync(userName, result, SaveMode.AutoReplication, softDelete, type.Value);

                            if (!result.IsError && result.Result)
                            {
                                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.IsError || !result.Result)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar detail with username ", userName, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with username ", userName, " was successfully deleted for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Deleted.";

                    //TODO: Need to move into background thread ASAP!
                    if (ProviderManager.Instance.IsAutoReplicationEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                        {
                            if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                result = await DeleteAvatarDetailByUsernameForProviderAsync(userName, result, SaveMode.AutoReplication, softDelete, type.Value);
                        }

                        if (result.WarningCount > 0)
                            OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with username ", userName, " was successfully deleted for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                        else
                            LoggingManager.Log("Avatar Detail Successfully Deleted/Replicated", LogType.Info, ref result, true, false);
                    }
                }

                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar detail with userName ", userName, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await DeleteAvatarDetailByEmailForProviderAsync(email, result, SaveMode.FirstSaveAttempt, softDelete, providerType);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if ((!result.Result || result.IsError) && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await DeleteAvatarDetailByEmailForProviderAsync(email, result, SaveMode.AutoFailOver, softDelete, type.Value);

                            if (!result.IsError && result.Result)
                            {
                                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.IsError || !result.Result)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar detail with email ", email, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with email ", email, " was successfully deleted for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Deleted.";

                    //TODO: Need to move into background thread ASAP!
                    if (ProviderManager.Instance.IsAutoReplicationEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                        {
                            if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                result = await DeleteAvatarDetailByEmailForProviderAsync(email, result, SaveMode.AutoReplication, softDelete, type.Value);
                        }

                        if (result.WarningCount > 0)
                            OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with email ", email, " was successfully deleted for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                        else
                            LoggingManager.Log("Avatar Detail Successfully Deleted/Replicated", LogType.Info, ref result, true, false);
                    }
                }

                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar detail with email ", email, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }

        public OASISResult<bool> DeleteAvatarDetailByEmail(string email, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = DeleteAvatarDetailByEmailForProvider(email, result, SaveMode.FirstSaveAttempt, softDelete, providerType);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if ((!result.Result || result.IsError) && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = DeleteAvatarDetailByEmailForProvider(email, result, SaveMode.AutoFailOver, softDelete, type.Value);

                            if (!result.IsError && result.Result)
                            {
                                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                                break;
                            }
                        }
                    }
                }

                if (result.IsError || !result.Result)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to delete the avatar detail with email ", email, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsSaved = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with email ", email, " was successfully deleted for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to delete for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Deleted.";

                    //TODO: Need to move into background thread ASAP!
                    if (ProviderManager.Instance.IsAutoReplicationEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                        {
                            if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                                result = DeleteAvatarDetailByEmailForProvider(email, result, SaveMode.AutoReplication, softDelete, type.Value);
                        }

                        if (result.WarningCount > 0)
                            OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with email ", email, " was successfully deleted for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                        else
                            LoggingManager.Log("Avatar Detail Successfully Deleted/Replicated", LogType.Info, ref result, true, false);
                    }
                }

                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured deleting the avatar detail with email ", email, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = false;
            }

            return result;
        }*/
    }
}
