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
        public OASISResult<IAvatar> LoadAvatar(Guid id, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        //public OASISResult<IAvatar> LoadAvatar(Guid id, bool loadWallets = true, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarForProvider(id, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarForProvider(id, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", id, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString(), "."), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", id, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
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
                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with id ", id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
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
                        Operation = "LoadAvatar",
                        AvatarId = id,
                        PreferredProvider = providerType
                    };

                    var hdResult = await hyperDrive.RouteRequestAsync<IAvatar>(request, LoadBalancingStrategy.Auto);
                    if (hdResult != null && !hdResult.IsError && hdResult.Result != null)
                        return hdResult;
                }
            }
            catch (Exception hyperDriveEx)
            {
                LoggingManager.Log($"HyperDrive v2 routing failed for LoadAvatar(id={id}), falling back to legacy provider path. Reason: {hyperDriveEx.Message}", LogType.Warning);
            }

            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarForProviderAsync(id, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarForProviderAsync(id, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", id, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", id, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
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
                await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with id ", id, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        /*
        public OASISResult<IAvatar> LoadAvatar(string username, string password, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarForProvider(username, password, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarForProvider(username, password, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarForProviderAsync(username, password, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarForProviderAsync(username, password, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Successfully Loaded.";

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);
                }

                // Set the current provider back to the original provider.
                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }*/

        //TODO: Replicate Auto-Fail over and Auto-Replication code for all Avatar, HolonManager methods etc...
        public OASISResult<IAvatar> LoadAvatar(string username, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarForProvider(username, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarForProvider(username, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
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
                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarForProviderAsync(username, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarForProviderAsync(username, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                //OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar, ", username, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", username, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
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
                await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with username ", username, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IAvatar> LoadAvatarByEmail(string email, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAvatarByEmailForProvider(email, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = LoadAvatarByEmailForProvider(email, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with email ", email, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", email, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
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
                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with email ", email, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAvatarByEmailForProviderAsync(email, result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAvatarByEmailForProviderAsync(email, result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with email ", email, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", email, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
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
                await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with email ", email, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        //public OASISResult<IAvatar> LoadAvatarByJwtToken(string jwtToken, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        //{
        //    OASISResult<IAvatar> result = new OASISResult<IAvatar>();
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    ProviderType previousProviderType = ProviderType.Default;

        //    try
        //    {
        //        result = LoadAvatarByJwtTokenForProvider(jwtToken, result, providerType, version);
        //        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

        //        if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //        {
        //            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //            {
        //                if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //                {
        //                    result = LoadAvatarByJwtTokenForProvider(jwtToken, result, type.Value, version);

        //                    if (!result.IsError && result.Result != null)
        //                        break;
        //                }
        //            }
        //        }

        //        if (result.Result == null)
        //            OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with jwtToken ", jwtToken, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
        //        else
        //        {
        //            if (result.WarningCount > 0)
        //                OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar ", jwtToken, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
        //            else
        //                result.Message = "Avatar Successfully Loaded.";

        //            if (loadPrivateKeys)
        //                result = LoadProviderWallets(result);
        //            else
        //                result.IsLoaded = true;

        //            if (hideAuthDetails)
        //                result.Result = HideAuthDetails(result.Result);
        //        }

        //        // Set the current provider back to the original provider.
        //        ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with jwtToken ", jwtToken, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
        //        result.Result = null;
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IAvatar>> LoadAvatarByJwtTokenAsync(string jwtToken, bool loadPrivateKeys = false, bool hideAuthDetails = true, ProviderType providerType = ProviderType.Default, int version = 0)
        //{
        //    OASISResult<IAvatar> result = new OASISResult<IAvatar>();
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    ProviderType previousProviderType = ProviderType.Default;

        //    try
        //    {
        //        result = await LoadAvatarByJwtTokenForProviderAsync(jwtToken, result, providerType, version);
        //        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

        //        if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //        {
        //            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //            {
        //                if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //                {
        //                    result = await LoadAvatarByJwtTokenForProviderAsync(jwtToken, result, type.Value, version);

        //                    if (!result.IsError && result.Result != null)
        //                        break;
        //                }
        //            }
        //        }

        //        if (result.Result == null)
        //            OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load avatar with jwtToken ", jwtToken, ". Mostly likely reason is the avatar does not exist. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
        //        else
        //        {
        //            if (result.WarningCount > 0)
        //                OASISErrorHandling.HandleWarning(ref result, string.Concat("The jwtToken ", jwtToken, " loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
        //            else
        //                result.Message = "Avatar Successfully Loaded.";

        //            if (loadPrivateKeys)
        //                result = await LoadProviderWalletsAsync(result);
        //            else
        //                result.IsLoaded = true;

        //            if (hideAuthDetails)
        //                result.Result = HideAuthDetails(result.Result);
        //        }

        //        // Set the current provider back to the original provider.
        //        ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading avatar with jwtToken ", jwtToken, " for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
        //        result.Result = null;
        //    }

        //    return result;
        //}
    }
}
