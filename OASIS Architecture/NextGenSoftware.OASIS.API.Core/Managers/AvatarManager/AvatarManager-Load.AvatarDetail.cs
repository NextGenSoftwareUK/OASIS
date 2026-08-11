using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        public OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarDetailForProvider(id, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarDetailForProvider(id, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with id ", id, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with id ", id, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with id ", id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarDetailForProviderAsync(id, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarDetailForProviderAsync(id, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with id ", id, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with id ", id, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with id ", id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarDetailByEmailForProviderAsync(email, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarDetailByEmailForProviderAsync(email, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with email ", email, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with email ", email, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with email ", email, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarDetailByEmailForProvider(email, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarDetailByEmailForProvider(email, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with email ", email, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with email ", email, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with email ", email, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarDetailByUsernameForProviderAsync(username, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarDetailByEmailForProviderAsync(username, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with username ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with username ", username, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with username ", username, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarDetailByUsernameForProvider(username, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarDetailByEmailForProvider(username, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar detail with username ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar detail with username ", username, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Detail Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar detail with username ", username, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }
    }
}
