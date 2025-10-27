using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        public OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = LoadAllHolonsForProviderType(holonType, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadAllHolonsForProviderType(holonType, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = LoadChildHolonsRecursiveForAllHolons(result, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);
            }

            if (cache)
                _allHolonsCache = result;

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadAllHolons<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default, bool cache = true) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = LoadAllHolonsForProviderType(holonType, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadAllHolonsForProviderType(holonType, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
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
                    result = LoadChildHolonsRecursiveForAllHolons(result, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = await LoadAllHolonsForProviderTypeAsync(holonType, providerType, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadAllHolonsForProviderTypeAsync(holonType, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = await LoadChildHolonsRecursiveForAllHolonsAsync(result, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadAllHolonsAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, HolonType childHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = await LoadAllHolonsForProviderTypeAsync(holonType, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadAllHolonsForProviderTypeAsync(holonType, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
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
                    result = await LoadChildHolonsRecursiveForAllHolonsAsync(result, childHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, 0, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }
    }
} 