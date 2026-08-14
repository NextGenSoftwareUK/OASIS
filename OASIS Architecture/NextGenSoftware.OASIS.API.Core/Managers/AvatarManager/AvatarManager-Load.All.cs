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
        public OASISResult<IEnumerable<string>> LoadAllAvatarNames(bool includeUsernames = true, bool includeIds = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<string>> result = new OASISResult<IEnumerable<string>>();
            OASISResult<IEnumerable<IAvatar>> avatarsResult = LoadAllAvatars(false, true, true, providerType, version);

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                result.Result = ProcessAvatarNames(avatarsResult.Result, includeUsernames, includeIds);
                result.IsLoaded = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAllAvatarNames calling LoadAllAvatars. Reason:{avatarsResult.Message}");

            return result;
        }

        public async Task<OASISResult<IEnumerable<string>>> LoadAllAvatarNamesAsync(bool includeUsernames = true, bool includeIds = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<string>> result = new OASISResult<IEnumerable<string>>();
            OASISResult<IEnumerable<IAvatar>> avatarsResult = await LoadAllAvatarsAsync(false, true, true, providerType, version);

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                result.Result = ProcessAvatarNames(avatarsResult.Result, includeUsernames, includeIds);
                result.IsLoaded = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAllAvatarNamesAsync calling LoadAllAvatarsAsync. Reason:{avatarsResult.Message}");

            return result;
        }

        public OASISResult<Dictionary<string,List<string>>> LoadAllAvatarNamesGroupedByName(bool includeUsernames = true, bool includeIds = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<Dictionary<string, List<string>>> result = new OASISResult<Dictionary<string, List<string>>>();
            OASISResult<IEnumerable<IAvatar>> avatarsResult = LoadAllAvatars(false, true, true, providerType, version);

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                result.Result = GroupAvatarNames(avatarsResult.Result, includeUsernames, includeIds);
                result.IsLoaded = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAllAvatarNamesGroupedByName calling LoadAllAvatars. Reason:{avatarsResult.Message}");

            return result;
        }

        public async Task<OASISResult<Dictionary<string, List<string>>>> LoadAllAvatarNamesGroupedByNameAsync(bool includeUsernames = true, bool includeIds = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<Dictionary<string, List<string>>> result = new OASISResult<Dictionary<string, List<string>>>(new Dictionary<string, List<string>>());
            OASISResult<IEnumerable<IAvatar>> avatarsResult = await LoadAllAvatarsAsync(false, true, true, providerType, version);

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                result.Result = GroupAvatarNames(avatarsResult.Result, includeUsernames, includeIds);
                result.IsLoaded = true;
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAllAvatarNamesGroupedByNameAsync calling LoadAllAvatarsAsync. Reason:{avatarsResult.Message}");

            return result;
        }

        //public OASISResult<Dictionary<string, List<string>>> LoadAllAvatarName(bool includeUsernames = true, bool includeIds = true, ProviderType providerType = ProviderType.Default, int version = 0)
        //{
        //    OASISResult<Dictionary<string, List<string>>> result = new OASISResult<Dictionary<string, List<string>>>();
        //    OASISResult<IEnumerable<IAvatar>> avatarsResult = LoadAllAvatars(false, true, true, providerType, version);

        //    if (!avatarsResult.IsError && avatarsResult.Result != null)
        //    {
        //        result.Result = GroupAvatarNames(avatarsResult.Result, includeUsernames, includeIds);
        //        result.IsLoaded = true;
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadAllAvatarNamesGroupedByName calling LoadAllAvatars. Reason:{avatarsResult.Message}");

        //    return result;
        //}


        public OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(bool loadPrivateKeys = false, bool hideAuthDetails = true, bool orderByName = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAllAvatarsForProvider(result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = LoadAllAvatarsForProvider(result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatars. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("All avatars loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatars Successfully Loaded.";

                    if (loadPrivateKeys)
                        result = LoadProviderWalletsForAllAvatars(result);
                    else
                        result.IsLoaded = true;

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);

                    if (orderByName)
                        result.Result = result.Result.OrderBy(x => x.FullName);
                }

                // Set the current provider back to the original provider.
                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatars for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(bool loadPrivateKeys = false, bool hideAuthDetails = true, bool orderByName = true, ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAllAvatarsForProviderAsync(result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAllAvatarsForProviderAsync(result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatars. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("All avatars loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatars Successfully Loaded.";

                    if (loadPrivateKeys)
                        result = await LoadProviderWalletsForAllAvatarsAsync(result);
                    else
                        result.IsLoaded = true;

                    if (hideAuthDetails)
                        result.Result = HideAuthDetails(result.Result);

                    if (orderByName)
                        result.Result = result.Result.OrderBy(x => x.FullName);
                }

                // Set the current provider back to the original provider.
                await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatars for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = LoadAllAvatarDetailsForProvider(result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = LoadAllAvatarDetailsForProvider(result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatar details. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("All avatar details loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Details Successfully Loaded.";
                }

                // Set the current provider back to the original provider.
                ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatar details for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(ProviderType providerType = ProviderType.Default, int version = 0)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            try
            {
                result = await LoadAllAvatarDetailsForProviderAsync(result, providerType, version);
                previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;

                if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                    {
                        if (type.Value != previousProviderType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            result = await LoadAllAvatarDetailsForProviderAsync(result, type.Value, version);

                            if (!result.IsError && result.Result != null)
                                break;
                        }
                    }
                }

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all avatar details. Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                {
                    result.IsLoaded = true;

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("All avatar details loaded successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                    else
                        result.Message = "Avatar Details Successfully Loaded";
                }

                // Set the current provider back to the original provider.
                await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat("Unknown error occured loading all avatar details for provider ", ProviderManager.Instance.CurrentStorageProviderType.Name), string.Concat("Error Message: ", ex.Message), ex);
                result.Result = null;
            }

            return result;
        }

        private Dictionary<string, List<string>> GroupAvatarNames(IEnumerable<IAvatar> avatars, bool includeUsernames = true, bool includeIds = true)
        {
            Dictionary<string, List<string>> groupedAvatars = new Dictionary<string, List<string>>();

            foreach (var avatar in avatars)
            {
                if (!groupedAvatars.ContainsKey(avatar.FullName))
                    groupedAvatars[avatar.FullName] = new List<string>();

                string name = "";

                if (includeIds)
                    name = $"{avatar.Id} ";

                if (includeUsernames)
                    name = $"{name}({avatar.Username})";

                groupedAvatars[avatar.FullName].Add(name.Trim());
            }

            return groupedAvatars;
        }

        private List<string> ProcessAvatarNames(IEnumerable<IAvatar> avatars, bool includeUsernames = true, bool includeIds = true)
        {
            List<string> avatarNames = new List<string>();

            foreach (var avatar in avatars)
            {
                string name = avatar.FullName;

                if (includeIds)
                    name = $"{name} ({avatar.Id})";

                if (includeUsernames)
                    name = $"{name} ({avatar.Username})";

                avatarNames.Add(name.Trim());
            }

            avatarNames = avatarNames.Distinct().ToList();
            return avatarNames;
        }
    }
}
