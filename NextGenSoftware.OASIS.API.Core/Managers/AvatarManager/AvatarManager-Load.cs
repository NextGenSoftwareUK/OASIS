using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        public OASISResult<IAvatar> LoadAvatar(Guid id, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarForProvider(id, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarForProvider(id, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", id, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString(), "."), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", id, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (loadPrivateKeys)
                        result = LoadProviderWallets(result);
                    else
                        result.IsLoaded = true;

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with id ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarForProviderAsync(id, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarForProviderAsync(id, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", id, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", id, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (loadPrivateKeys)
                        result = await LoadProviderWalletsAsync(result);
                    else
                        result.IsLoaded = true;

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with id ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        /*
        public OASISResult<IAvatar> LoadAvatar(string username, string password, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarForProvider(username, password, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarForProvider(username, password, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarForProviderAsync(username, password, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarForProviderAsync(username, password, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }*/

        //TODO: Replicate Auto-Fail over and Auto-Replication code for all Avatar, HolonManager methods etc...
        public OASISResult<IAvatar> LoadAvatar(string username, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarForProvider(username, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarForProvider(username, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (loadPrivateKeys)
                        result = LoadProviderWallets(result);
                    else
                        result.IsLoaded = true;

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarForProviderAsync(username, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarForProviderAsync(username, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                //ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (loadPrivateKeys)
                        result = await LoadProviderWalletsAsync(result);
                    else
                        result.IsLoaded = true;

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatar> LoadAvatarByEmail(string email, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarByEmailForProvider(email, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarByEmailForProvider(email, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with email ", email, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", email, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (loadPrivateKeys)
                        result = LoadProviderWallets(result);
                    else
                        result.IsLoaded = true;

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with email ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarByEmailForProviderAsync(email, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarByEmailForProviderAsync(email, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with email ", email, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", email, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (loadPrivateKeys)
                        result = await LoadProviderWalletsAsync(result);
                    else
                        result.IsLoaded = true;

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with email ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarDetailForProvider(id, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarDetailForProvider(id, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with id ", id, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with id ", id, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with id ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarDetailForProviderAsync(id, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarDetailForProviderAsync(id, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with id ", id, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with id ", id, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with id ", id, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarDetailByEmailForProviderAsync(email, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarDetailByEmailForProviderAsync(email, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with email ", email, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with email ", email, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with email ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarDetailByEmailForProvider(email, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarDetailByEmailForProvider(email, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with email ", email, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with email ", email, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with email ", email, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarDetailByUsernameForProviderAsync(username, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarDetailByEmailForProviderAsync(username, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with username ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with username ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarDetailByUsernameForProvider(username, result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarDetailByEmailForProvider(username, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with username ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with username ", username, " loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with username ", username, " for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAllAvatarsForProvider(result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAllAvatarsForProvider(result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatars. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("All avatars loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatars Successfully Loaded.";

                    if (loadPrivateKeys)
                        result = LoadProviderWalletsForAllAvatars(result);
                    else
                        result.IsLoaded = true;

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatars for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAllAvatarsForProviderAsync(result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAllAvatarsForProviderAsync(result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatars. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("All avatars loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatars Successfully Loaded.";

                    if (loadPrivateKeys)
                        result = await LoadProviderWalletsForAllAvatarsAsync(result);
                    else
                        result.IsLoaded = true;

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatars for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAllAvatarDetailsForProvider(result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = LoadAllAvatarDetailsForProvider(result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatar details. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("All avatar details loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Details Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatar details for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAllAvatarDetailsForProviderAsync(result, providerType, version);
                previousProviderType = ProviderManager.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAllAvatarDetailsForProviderAsync(result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    ErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatar details. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        ErrorHandling.HandleWarning(ref result, string.Concat("All avatar details loaded successfully for the provider ", ProviderManager.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Details Successfully Loaded";
                }

                // Set the current provider back to the original provider.
                ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatar details for provider ", ProviderManager.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }
    }
}