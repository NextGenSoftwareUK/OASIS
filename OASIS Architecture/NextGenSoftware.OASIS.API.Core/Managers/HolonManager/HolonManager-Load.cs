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
    public partial class HolonManager : OASISManager
    {
        public OASISResult<T> LoadHolon<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
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

                DecryptHolonMetaData(result.Result);

                if (result.Result.MetaData != null)
                    result.Result = (T)MapMetaData<T>(result.Result);

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<T>> holonsResult = LoadHolonsForParent<T>(id, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

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

        public OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
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

                DecryptHolonMetaData(result.Result);

                if (loadChildren && !loadChildrenFromProvider)
                {
                    OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsForParent(id, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

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

        public async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
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
                        Operation = "LoadHolon",
                        HolonId = id,
                        PreferredProvider = providerType
                    };

                var hdResult = await hyperDrive.RouteRequestAsync<IHolon>(request, LoadBalancingStrategy.Auto);
                if (hdResult != null && !hdResult.IsError && hdResult.Result != null)
                        return hdResult;
                }
            }
            catch (Exception hyperDriveEx)
            {
                LoggingManager.Log($"HyperDrive v2 routing failed for LoadHolon(id={id}), falling back to legacy provider path. Reason: {hyperDriveEx.Message}", LogType.Warning);
            }
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
                    OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsForParentAsync(id, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

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

        public async Task<OASISResult<T>> LoadHolonAsync<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
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
                    if (string.IsNullOrEmpty(result.Result.AllChildIdListCache))
                    {
                        //TODO: Need to add LoadHolonsForIds methods to IOASISStorage interface & providers which takes the AllChildIdList as a param..


                        //List<string> childIds = new List<string>();
                        //childIds = result.Result.AllChildIdList.Split(",").ToList();
                        //Guid childId = Guid.Empty;

                        //foreach (string guid in childIds)
                        //{
                        //    if (Guid.TryParse(guid, out childId))
                        //    {

                        //    }
                        //}
                    }
                    //else
                    //{
                        OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsForParentAsync<T>(id, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

                        if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                            result.Result.Children = [.. holonsResult.Result];
                        else
                        {
                            if (result.IsWarning)
                                OASISErrorHandling.HandleError(ref result, $"The holon with id {id} failed to load and one or more of it's children failed to load. Reason: {holonsResult.Message}");
                            else
                                OASISErrorHandling.HandleWarning(ref result, $"The holon with id {id} loaded fine but one or more of it's children failed to load. Reason: {holonsResult.Message}");
                        }
                   //}
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
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
                    OASISResult<IEnumerable<IHolon>> holonsResult = LoadHolonsForParent(providerKey, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

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

        public OASISResult<T> LoadHolon<T>(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
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
                    OASISResult<IEnumerable<T>> holonsResult = LoadHolonsForParent<T>(providerKey, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

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

        public async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
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
                    OASISResult<IEnumerable<IHolon>> holonsResult = await LoadHolonsForParentAsync(providerKey, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

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

        public async Task<OASISResult<T>> LoadHolonAsync<T>(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
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
                    OASISResult<IEnumerable<T>> holonsResult = await LoadHolonsForParentAsync<T>(providerKey, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, 0, childHolonType, version, providerType);

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

    }
}
