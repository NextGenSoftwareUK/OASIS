using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Linq;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager
    {
        //public OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    result = LoadHolonForProviderTypeByCustomKey(customKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = LoadHolonForProviderTypeByCustomKey(customKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with customKey ", customKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        result.Result.Original = result.Result;

        //        if (loadChildren && !loadChildrenFromProvider)
        //        {
        //            OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsForParent(customKey, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

        //            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
        //                result.Result.Children = holonsResult.Result.ToList();
        //            else
        //            {
        //                if (result.IsWarning)
        //                    OASISErrorHandling.HandleError(ref result, $"The holon with customKey {customKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //                else
        //                    OASISErrorHandling.HandleWarning(ref result, $"The holon with customKey {customKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //            }
        //        }
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public OASISResult<T> LoadHolonByCustomKey<T>(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<T> result = new OASISResult<T>();

        //    result = LoadHolonForProviderTypeByCustomKey(customKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = LoadHolonForProviderTypeByCustomKey(customKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with customKey ", customKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        result.Result.Original = result.Result;

        //        if (result.Result.MetaData != null)
        //            result.Result = (T)MapMetaData<T>(result.Result);

        //        if (loadChildren && !loadChildrenFromProvider)
        //        {
        //            OASISResult<IEnumerable<T>> holonsResult = LoadHolonsForParent<T>(customKey, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

        //            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
        //                result.Result.Children = [.. holonsResult.Result];
        //            else
        //            {
        //                if (result.IsWarning)
        //                    OASISErrorHandling.HandleError(ref result, $"The holon with customKey {customKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //                else
        //                    OASISErrorHandling.HandleWarning(ref result, $"The holon with customKey {customKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //            }
        //        }
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    result = await LoadHolonForProviderTypeByCustomKeyAsync(customKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = await LoadHolonForProviderTypeByCustomKeyAsync(customKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with customKey ", customKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        result.Result.Original = result.Result;

        //        if (loadChildren && !loadChildrenFromProvider)
        //        {
        //            OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsForParentAsync(customKey, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

        //            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
        //                result.Result.Children = holonsResult.Result.ToList();
        //            else
        //            {
        //                if (result.IsWarning)
        //                    OASISErrorHandling.HandleError(ref result, $"The holon with customKey {customKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //                else
        //                    OASISErrorHandling.HandleWarning(ref result, $"The holon with customKey {customKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //            }
        //        }
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public async Task<OASISResult<T>> LoadHolonByCustomKeyAsync<T>(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<T> result = new OASISResult<T>();

        //    result = await LoadHolonForProviderTypeByCustomKeyAsync(customKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = await LoadHolonForProviderTypeByCustomKeyAsync(customKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with customKey ", customKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        result.Result.Original = result.Result;

        //        if (result.Result.MetaData != null)
        //            result.Result = (T)MapMetaData<T>(result.Result);

        //        if (loadChildren && !loadChildrenFromProvider)
        //        {
        //            OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsForParentAsync<T>(customKey, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

        //            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
        //                result.Result.Children = [.. holonsResult.Result];
        //            else
        //            {
        //                if (result.IsWarning)
        //                    OASISErrorHandling.HandleError(ref result, $"The holon with customKey {customKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //                else
        //                    OASISErrorHandling.HandleWarning(ref result, $"The holon with customKey {customKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //            }
        //        }
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    result = LoadHolonForProviderTypeByMetaData(metaKey, metaValue, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = LoadHolonForProviderTypeByMetaData(metaKey, metaValue, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with metaKey ", metaKey, " and metaValue ", metaValue, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        result.Result.Original = result.Result;

        //        if (loadChildren && !loadChildrenFromProvider)
        //        {
        //            OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsByMetaData(metaKey, metaValue, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

        //            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
        //                result.Result.Children = holonsResult.Result.ToList();
        //            else
        //            {
        //                if (result.IsWarning)
        //                    OASISErrorHandling.HandleError(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //                else
        //                    OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //            }
        //        }
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public OASISResult<T> LoadHolonByMetaData<T>(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<T> result = new OASISResult<T>();

        //    result = LoadHolonForProviderTypeByMetaData(metaKey, metaValue, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = LoadHolonForProviderTypeByMetaData(metaKey, metaValue, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with metaKey ", metaKey, " and metaValue ", metaValue, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        result.Result.Original = result.Result;

        //        if (result.Result.MetaData != null)
        //            result.Result = (T)MapMetaData<T>(result.Result);

        //        if (loadChildren && !loadChildrenFromProvider)
        //        {
        //            OASISResult<IEnumerable<T>> holonsResult = LoadHolonsByMetaData<T>(metaKey, metaValue, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

        //            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
        //                result.Result.Children = [.. holonsResult.Result];
        //            else
        //            {
        //                if (result.IsWarning)
        //                    OASISErrorHandling.HandleError(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //                else
        //                    OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //            }
        //        }
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IHolon> result = new OASISResult<IHolon>();

        //    result = await LoadHolonForProviderTypeByMetaDataAsync(metaKey, metaValue, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = await LoadHolonForProviderTypeByMetaDataAsync(metaKey, metaValue, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with metaKey ", metaKey, " and metaValue ", metaValue, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        result.Result.Original = result.Result;

        //        if (loadChildren && !loadChildrenFromProvider)
        //        {
        //            OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsByMetaDataAsync(metaKey, metaValue, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

        //            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
        //                result.Result.Children = holonsResult.Result.ToList();
        //            else
        //            {
        //                if (result.IsWarning)
        //                    OASISErrorHandling.HandleError(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //                else
        //                    OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //            }
        //        }
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public async Task<OASISResult<T>> LoadHolonByMetaDataAsync<T>(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<T> result = new OASISResult<T>();

        //    result = await LoadHolonForProviderTypeByMetaDataAsync(metaKey, metaValue, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = await LoadHolonForProviderTypeByMetaDataAsync(metaKey, metaValue, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with metaKey ", metaKey, " and metaValue ", metaValue, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        result.Result.Original = result.Result;

        //        if (result.Result.MetaData != null)
        //            result.Result = (T)MapMetaData<T>(result.Result);

        //        OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsByMetaDataAsync<T>(metaKey, metaValue, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

        //        if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
        //            result.Result.Children = [.. holonsResult.Result];
        //        else
        //        {
        //            if (result.IsWarning)
        //                OASISErrorHandling.HandleError(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //            else
        //                OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
        //        }
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public OASISResult<IEnumerable<IHolon>> LoadHolonsByCustomKey(string customKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

        //    result = LoadHolonsForParentForProviderType(customKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = LoadHolonsForParentForProviderType(customKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with customKey ", customKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        foreach (IHolon holon in result.Result)
        //            holon.Original = holon;

        //        if (loadChildren && !loadChildrenFromProvider)
        //            result = LoadChildHolonsRecursiveForParentHolon(result, $"customKey with {customKey}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public OASISResult<IEnumerable<T>> LoadHolonsByCustomKey<T>(string customKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

        //    result = LoadHolonsForParentForProviderType(customKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = LoadHolonsForParentForProviderType(customKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with customKey ", customKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        MapMetaData(result);

        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        foreach (IHolon holon in result.Result)
        //            holon.Original = holon;

        //        if (loadChildren && !loadChildrenFromProvider)
        //            result = LoadChildHolonsRecursiveForParentHolon(result, $"customKey with {customKey}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        ////TODO: Need to implement this proper way of calling an OASIS method across the entire OASIS...
        //public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByCustomKeyAsync(string customKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

        //    result = await LoadHolonsForParentForProviderTypeAsync(customKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = await LoadHolonsForParentForProviderTypeAsync(customKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with customKey ", customKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        foreach (IHolon holon in result.Result)
        //            holon.Original = holon;

        //        if (loadChildren && !loadChildrenFromProvider)
        //            result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"customKey with {customKey}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public async Task<OASISResult<IEnumerable<T>>> LoadHolonsByCustomKeyAsync<T>(string customKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

        //    result = await LoadHolonsForParentForProviderTypeAsync(customKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = await LoadHolonsForParentForProviderTypeAsync(customKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with customKey ", customKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        MapMetaData(result);

        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        foreach (IHolon holon in result.Result)
        //            holon.Original = holon;

        //        if (loadChildren && !loadChildrenFromProvider)
        //            result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"customKey with {customKey}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        public OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsByMetaData(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                //if (holonsResult.Result.Count() > 1)
                //    OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded but more than one holon was found. Returning the first one.");

                result.Result = holonsResult.Result.FirstOrDefault();
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadHolonByMetaData. Reason: The holon with metaKey {metaKey} and metaValue {metaValue} failed to load. Reason: {holonsResult.Message}");

            if (result.Result == null)
            {
                result.Message = "No holon found";
                result.IsWarning = true;
            }
            else
                result.IsLoaded = true;

            return result;
        }

        public OASISResult<T> LoadHolonByMetaData<T>(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<IEnumerable<T>> holonsResult = LoadHolonsByMetaData<T>(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                //if (holonsResult.Result.Count() > 1)
                //    OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded but more than one holon was found. Returning the first one.");

                result.Result = holonsResult.Result.FirstOrDefault();
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadHolonByMetaData. Reason: The holon with metaKey {metaKey} and metaValue {metaValue} failed to load. Reason: {holonsResult.Message}");

            if (result.Result == null)
            {
                result.Message = "No holon found";
                result.IsWarning = true;
            }
            else
                result.IsLoaded = true;

            return result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsByMetaDataAsync(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                //if (holonsResult.Result.Count() > 1)
                //    OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded but more than one holon was found. Returning the first one.");

                result.Result = holonsResult.Result.FirstOrDefault();
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadHolonByMetaDataAsync. Reason: The holon with metaKey {metaKey} and metaValue {metaValue} failed to load. Reason: {holonsResult.Message}");

            if (result.Result == null)
            {
                result.Message = "No holon found";
                result.IsWarning = true;
            }
            else
                result.IsLoaded = true;

            return result;
        }

        public async Task<OASISResult<T>> LoadHolonByMetaDataAsync<T>(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsByMetaDataAsync<T>(metaKey, metaValue, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                //if (holonsResult.Result.Count() > 1)
                //    OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded but more than one holon was found. Returning the first one.");

                result.Result = holonsResult.Result.FirstOrDefault();
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadHolonByMetaDataAsync. Reason: The holon with metaKey {metaKey} and metaValue {metaValue} failed to load. Reason: {holonsResult.Message}");

            if (result.Result == null)
            {
                result.Message = "No holon found";
                result.IsWarning = true;
            }
            else
                result.IsLoaded = true;

            return result;
        }

        public OASISResult<IHolon> LoadHolonByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                //if (holonsResult.Result.Count() > 1)
                //    OASISErrorHandling.HandleWarning(ref result, $"The holon loaded but more than one holon was found. Returning the first one.");

                result.Result = holonsResult.Result.FirstOrDefault();
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadHolonByMetaData. Reason: The holon failed to load. Reason: {holonsResult.Message}");

            if (result.Result == null)
            {
                result.Message = "No holon found";
                result.IsWarning = true;
            }
            else
                result.IsLoaded = true;

            return result;
        }

        public OASISResult<T> LoadHolonByMetaData<T>(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<IEnumerable<T>> holonsResult = LoadHolonsByMetaData<T>(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                //if (holonsResult.Result.Count() > 1)
                //    OASISErrorHandling.HandleWarning(ref result, $"The holon loaded but more than one holon was found. Returning the first one.");

                result.Result = holonsResult.Result.FirstOrDefault();
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadHolonByMetaData. Reason: The holon failed to load. Reason: {holonsResult.Message}");

            if (result.Result == null)
            {
                result.Message = "No holon found";
                result.IsWarning = true;
            }
            else
                result.IsLoaded = true;

            return result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                //if (holonsResult.Result.Count() > 1)
                //    OASISErrorHandling.HandleWarning(ref result, $"The holon loaded but more than one holon was found. Returning the first one.");

                result.Result = holonsResult.Result.FirstOrDefault();
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadHolonByMetaDataAsync. Reason: The holon failed to load. Reason: {holonsResult.Message}");

            if (result.Result == null)
            {
                result.Message = "No holon found";
                result.IsWarning = true;
            }
            else
                result.IsLoaded = true;

            return result;
        }

        public async Task<OASISResult<T>> LoadHolonByMetaDataAsync<T>(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsByMetaDataAsync<T>(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

            if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
            {
                //if (holonsResult.Result.Count() > 1)
                //    OASISErrorHandling.HandleWarning(ref result, $"The holon loaded but more than one holon was found. Returning the first one.");

                result.Result = holonsResult.Result.FirstOrDefault();
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadHolonByMetaDataAsync. Reason: The holon failed to load. Reason: {holonsResult.Message}");

            if (result.Result == null)
            {
                result.Message = "No holon found";
                result.IsWarning = true;
            }
            else
                result.IsLoaded = true;

            return result;
        }

    }
}
