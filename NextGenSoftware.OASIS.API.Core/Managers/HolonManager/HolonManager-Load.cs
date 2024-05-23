using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        public OASISResult<T> LoadHolon<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = LoadHolonForProviderType(id, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderType(id, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (result.Result.MetaData != null)
                    result.Result = (T)MapMetaData<T>(result.Result);

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<T>> holonsResult = LoadHolonsForParent<T>(id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = [.. holonsResult.Result];
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with id {id} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with id {id} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = LoadHolonForProviderType(id, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderType(id, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsForParent(id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = holonsResult.Result.ToList();
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with id {id} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with id {id} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = await LoadHolonForProviderTypeAsync(id, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeAsync(id, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString(), string.Concat(".\n\nDetailed Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsForParentAsync(id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = holonsResult.Result.ToList();
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with id {id} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with id {id} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<T>> LoadHolonAsync<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = await LoadHolonForProviderTypeAsync(id, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeAsync(id, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (result.Result.MetaData != null)
                    result.Result = (T)MapMetaData<T>(result.Result);

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsForParentAsync<T>(id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = [.. holonsResult.Result];
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with id {id} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with id {id} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = LoadHolonForProviderType(providerKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderType(providerKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with providerKey ", providerKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsForParent(providerKey, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = holonsResult.Result.ToList();
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with providerKey {providerKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with providerKey {providerKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<T> LoadHolon<T>(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = LoadHolonForProviderType(providerKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderType(providerKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with providerKey ", providerKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (result.Result.MetaData != null)
                    result.Result = (T)MapMetaData<T>(result.Result);

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<T>> holonsResult = LoadHolonsForParent<T>(providerKey, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = [.. holonsResult.Result];
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with providerKey {providerKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with providerKey {providerKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = await LoadHolonForProviderTypeAsync(providerKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeAsync(providerKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with providerKey ", providerKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsForParentAsync(providerKey, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = holonsResult.Result.ToList();
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with providerKey {providerKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with providerKey {providerKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<T>> LoadHolonAsync<T>(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = await LoadHolonForProviderTypeAsync(providerKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeAsync(providerKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with providerKey ", providerKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (result.Result.MetaData != null)
                    result.Result = (T)MapMetaData<T>(result.Result);

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsForParentAsync<T>(providerKey, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = [.. holonsResult.Result];
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with providerKey {providerKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with providerKey {providerKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = LoadHolonForProviderTypeByCustomKey(customKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderTypeByCustomKey(customKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with customKey ", customKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsForParent(customKey, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = holonsResult.Result.ToList();
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with customKey {customKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with customKey {customKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<T> LoadHolonByCustomKey<T>(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = LoadHolonForProviderTypeByCustomKey(customKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderTypeByCustomKey(customKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with customKey ", customKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (result.Result.MetaData != null)
                    result.Result = (T)MapMetaData<T>(result.Result);

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<T>> holonsResult = LoadHolonsForParent<T>(customKey, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = [.. holonsResult.Result];
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with customKey {customKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with customKey {customKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = await LoadHolonForProviderTypeByCustomKeyAsync(customKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeByCustomKeyAsync(customKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with customKey ", customKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsForParentAsync(customKey, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = holonsResult.Result.ToList();
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with customKey {customKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with customKey {customKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<T>> LoadHolonByCustomKeyAsync<T>(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = await LoadHolonForProviderTypeByCustomKeyAsync(customKey, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeByCustomKeyAsync(customKey, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with customKey ", customKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (result.Result.MetaData != null)
                    result.Result = (T)MapMetaData<T>(result.Result);

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsForParentAsync<T>(customKey, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = [.. holonsResult.Result];
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with customKey {customKey} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with customKey {customKey} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = LoadHolonForProviderTypeByMetaData(metaKey, metaValue, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderTypeByMetaData(metaKey, metaValue, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with metaKey ", metaKey, " and metaValue ", metaValue, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsForParentByMetaData(metaKey, metaValue, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = holonsResult.Result.ToList();
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<T> LoadHolonByMetaData<T>(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = LoadHolonForProviderTypeByMetaData(metaKey, metaValue, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderTypeByMetaData(metaKey, metaValue, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with metaKey ", metaKey, " and metaValue ", metaValue, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (result.Result.MetaData != null)
                    result.Result = (T)MapMetaData<T>(result.Result);

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<T>> holonsResult = LoadHolonsForParentByMetaData<T>(metaKey, metaValue, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = [.. holonsResult.Result];
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = await LoadHolonForProviderTypeByMetaDataAsync(metaKey, metaValue, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeByMetaDataAsync(metaKey, metaValue, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with metaKey ", metaKey, " and metaValue ", metaValue, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsForParentByMetaDataAsync(metaKey, metaValue, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                        result.Result.Children = holonsResult.Result.ToList();
                    else
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    }
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<T>> LoadHolonByMetaDataAsync<T>(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, bool loadChildrenFromProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = await LoadHolonForProviderTypeByMetaDataAsync(metaKey, metaValue, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeByMetaDataAsync(metaKey, metaValue, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with metaKey ", metaKey, " and metaValue ", metaValue, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                result.Result.Original = result.Result;

                if (result.Result.MetaData != null)
                    result.Result = (T)MapMetaData<T>(result.Result);

                OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsForParentByMetaDataAsync<T>(metaKey, metaValue, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);

                if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    result.Result.Children = [.. holonsResult.Result];
                else
                {
                    if (result.IsWarning)
                        OASISErrorHandling.HandleError(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                    else
                        OASISErrorHandling.HandleWarning(ref result, $"The holon with metaKey {metaKey} and metaValue {metaValue} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        } 
    }
} 