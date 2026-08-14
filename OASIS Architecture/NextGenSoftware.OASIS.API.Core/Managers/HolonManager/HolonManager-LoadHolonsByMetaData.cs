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
        public OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = LoadHolonsForProviderTypeByMetaData(metaKey, metaValue, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForProviderTypeByMetaData(metaKey, metaValue, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = LoadChildHolonsRecursiveForParentHolon(result, $"metaKey with {metaKey} and metaValue {metaValue}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadHolonsByMetaData<T>(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = LoadHolonsForProviderTypeByMetaData(metaKey, metaValue, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForProviderTypeByMetaData(metaKey, metaValue, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                MapMetaData(result);

                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = LoadChildHolonsRecursiveForParentHolon(result, $"metaKey with {metaKey} and metaValue {metaValue}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        //TODO: Need to implement this proper way of calling an OASIS method across the entire OASIS...
        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = await LoadHolonsForProviderTypeByMetaDataAsync(metaKey, metaValue, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForProviderTypeByMetaDataAsync(metaKey, metaValue, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"metaKey with {metaKey} and metaValue {metaValue}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadHolonsByMetaDataAsync<T>(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = await LoadHolonsForProviderTypeByMetaDataAsync(metaKey, metaValue, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForProviderTypeByMetaDataAsync(metaKey, metaValue, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                MapMetaData(result);

                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"metaKeyValuePairs", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = LoadHolonsForProviderTypeByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForProviderTypeByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons with metaKeyValuePairs and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = LoadChildHolonsRecursiveForParentHolon(result, $"metaKeyValuePairs", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadHolonsByMetaData<T>(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = LoadHolonsForProviderTypeByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForProviderTypeByMetaData(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons with metaKeyValuePairs and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                MapMetaData(result);

                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = LoadChildHolonsRecursiveForParentHolon(result, $"metaKeyValuePairs", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        //TODO: Need to implement this proper way of calling an OASIS method across the entire OASIS...
        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = await LoadHolonsForProviderTypeByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForProviderTypeByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for metaKeyValuePairs and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"metaKeyValuePairs", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadHolonsByMetaDataAsync<T>(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = await LoadHolonsForProviderTypeByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForProviderTypeByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons with metaKeyValuePairs and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                MapMetaData(result);

                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"metaKeyValuePairs", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }
            
            //TODO: Temp, need to investigate more later! ;-)
            if (currentProviderType != ProviderType.LocalFileOASIS)
                SwitchBackToCurrentProvider(currentProviderType, ref result);
            
            return result;
        }
    }
}
