using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = LoadHolonsForParentForProviderType(id, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForParentForProviderType(id, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with id ", id, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = LoadChildHolonsRecursiveForParentHolon(result, $"id with {id}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false,  int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = LoadHolonsForParentForProviderType(id, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForParentForProviderType(id, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with id ", id, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                MapMetaData(result);

                if (loadChildren && !loadChildrenFromProvider)
                    result = LoadChildHolonsRecursiveForParentHolon(result, $"id with {id}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = await LoadHolonsForParentForProviderTypeAsync(id, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForParentForProviderTypeAsync(id, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with id ", id, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"id with {id}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = await LoadHolonsForParentForProviderTypeAsync(id, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForParentForProviderTypeAsync(id, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with id ", id, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
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
                    result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"id with {id}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = LoadHolonsForParentForProviderType(providerKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForParentForProviderType(providerKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with providerKey ", providerKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = LoadChildHolonsRecursiveForParentHolon(result, $"providerKey with {providerKey}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = LoadHolonsForParentForProviderType(providerKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForParentForProviderType(providerKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with providerKey ", providerKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
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
                    result = LoadChildHolonsRecursiveForParentHolon(result, $"providerKey with {providerKey}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        //TODO: Need to implement this proper way of calling an OASIS method across the entire OASIS...
        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = await LoadHolonsForParentForProviderTypeAsync(providerKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForParentForProviderTypeAsync(providerKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with providerKey ", providerKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
            {
                // Store the original holon for change tracking in STAR/COSMIC.
                foreach (IHolon holon in result.Result)
                    holon.Original = holon;

                if (loadChildren && !loadChildrenFromProvider)
                    result = await LoadChildHolonsRecursiveForParentHolonAsync(result, providerKey, subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = await LoadHolonsForParentForProviderTypeAsync(providerKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

            if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForParentForProviderTypeAsync(providerKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with providerKey ", providerKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
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
                    result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"providerKey with {providerKey}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        //public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
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

        //public OASISResult<IEnumerable<T>> LoadHolonsForParentByCustomKey<T>(string customKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
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
        //public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
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

        //public async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentByCustomKeyAsync<T>(string customKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
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

        //public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByMetaData(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

        //    result = LoadHolonsForParentForProviderTypeByMetaData(metaKey, metaValue, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = LoadHolonsForParentForProviderTypeByMetaData(metaKey, metaValue, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        foreach (IHolon holon in result.Result)
        //            holon.Original = holon;

        //        if (loadChildren && !loadChildrenFromProvider)
        //            result = LoadChildHolonsRecursiveForParentHolon(result, $"metaKey with {metaKey} and metaValue {metaValue}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public OASISResult<IEnumerable<T>> LoadHolonsForParentByMetaData<T>(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

        //    result = LoadHolonsForParentForProviderTypeByMetaData(metaKey, metaValue, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = LoadHolonsForParentForProviderTypeByMetaData(metaKey, metaValue, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
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
        //            result = LoadChildHolonsRecursiveForParentHolon(result, $"metaKey with {metaKey} and metaValue {metaValue}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        ////TODO: Need to implement this proper way of calling an OASIS method across the entire OASIS...
        //public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByMetaDataAsync(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default)
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

        //    result = await LoadHolonsForParentForProviderTypeByMetaDataAsync(metaKey, metaValue, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = await LoadHolonsForParentForProviderTypeByMetaDataAsync(metaKey, metaValue, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
        //        result.Message = errorMessage;
        //        LoggingManager.Log(errorMessage, LogType.Error);
        //    }
        //    else
        //    {
        //        // Store the original holon for change tracking in STAR/COSMIC.
        //        foreach (IHolon holon in result.Result)
        //            holon.Original = holon;

        //        if (loadChildren && !loadChildrenFromProvider)
        //            result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"metaKey with {metaKey} and metaValue {metaValue}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}

        //public async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentByMetaDataAsync<T>(string metaKey, string metaValue, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int currentChildDepth = 0, HolonType subChildHolonType = HolonType.All, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        //{
        //    ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
        //    OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

        //    result = await LoadHolonsForParentForProviderTypeByMetaDataAsync(metaKey, metaValue, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //    if (result.Result == null && ProviderManager.Instance.IsAutoFailOverEnabled)
        //    {
        //        foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
        //        {
        //            if (type.Value != providerType && type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
        //            {
        //                result = await LoadHolonsForParentForProviderTypeByMetaDataAsync(metaKey, metaValue, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, result);

        //                if (result.Result != null)
        //                    break;
        //            }
        //        }
        //    }

        //    if (result.Result == null)
        //    {
        //        result.IsError = true;
        //        string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with metaKey ", metaKey, " and metaValue ", metaValue, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString());
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
        //            result = await LoadChildHolonsRecursiveForParentHolonAsync(result, $"metaKey with {metaKey} and metaValue {metaValue}", subChildHolonType, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, currentChildDepth, providerType);
        //    }

        //    SwitchBackToCurrentProvider(currentProviderType, ref result);
        //    return result;
        //}
    }
} 