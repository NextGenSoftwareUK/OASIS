using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class HolonManager : OASISManager
    {
        private static HolonManager _instance = null;

        //public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        public static HolonManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new HolonManager(ProviderManager.CurrentStorageProvider);

                return _instance;
            }
        }

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public HolonManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        /// <summary>
        /// Send's a given holon from one provider to another. 
        /// This method is only really needed if auto-replication is disabled or there is a use case for sending from one provider to another.
        /// By default this will NOT auto-replicate to any other provider (set autoReplicate to true if you wish it to). This param overrides the global auto-replication setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="sourceProviderType"></param>
        /// <param name="destinationProviderType"></param>
        /// <param name="autoReplicate"></param>
        /// <returns></returns>
        public OASISResult<T> SendHolon<T>(Guid id, ProviderType sourceProviderType, ProviderType destinationProviderType, bool autoReplicate = false) where T : IHolon, new()
        {
            // TODO: Finish Implementing ASAP...
            // Needs to load the holon from the source provider and then save to the destination provider.


            return new OASISResult<T>();
        }

        public OASISResult<T> LoadHolon<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = LoadHolonForProviderType(id, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderType(id, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            else if (result.Result.MetaData != null)
                result.Result = (T)MapMetaData<T>(result.Result);

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            result.Result.Original = result.Result;
            return result;
        }

        public OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = LoadHolonForProviderType(id, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderType(id, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            result.Result.Original = result.Result;
            return result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = await LoadHolonForProviderTypeAsync(id, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeAsync(id, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            result.Result.Original = result.Result;
            return result;
        }

        public async Task<OASISResult<T>> LoadHolonAsync<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = await LoadHolonForProviderTypeAsync(id, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeAsync(id, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with id ", id, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else if (result.Result.MetaData != null)
                result.Result = (T)MapMetaData<T>(result.Result);

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            result.Result.Original = result.Result;
            return result;
        }

        public OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = LoadHolonForProviderType(providerKey, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderType(providerKey, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with providerKey ", providerKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            result.Result.Original = result.Result;
            return result;
        }

        public OASISResult<T> LoadHolon<T>(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = LoadHolonForProviderType(providerKey, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonForProviderType(providerKey, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with providerKey ", providerKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else if (result.Result.MetaData != null)
                result.Result = (T)MapMetaData<T>(result.Result);

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            result.Result.Original = result.Result;
            return result;
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = await LoadHolonForProviderTypeAsync(providerKey, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeAsync(providerKey, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with providerKey ", providerKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            result.Result.Original = result.Result;
            return result;
        }

        public async Task<OASISResult<T>> LoadHolonAsync<T>(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>();

            result = await LoadHolonForProviderTypeAsync(providerKey, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonForProviderTypeAsync(providerKey, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load the holon with providerKey ", providerKey, ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else if (result.Result.MetaData != null)
                result.Result = (T)MapMetaData<T>(result.Result);

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            result.Result.Original = result.Result;
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = LoadHolonsForParentForProviderType(id, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForParentForProviderType(id, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with id ", id, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        public OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = LoadHolonsForParentForProviderType(id, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForParentForProviderType(id, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with id ", id, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
                MapMetaData(result);

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            //TODO: NEED TO ADD LOADCHILDREN PARAM ASAP.
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = await LoadHolonsForParentForProviderTypeAsync(id, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForParentForProviderTypeAsync(id, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with id ", id, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = await LoadHolonsForParentForProviderTypeAsync(id, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForParentForProviderTypeAsync(id, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with id ", id, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
                MapMetaData(result);

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = LoadHolonsForParentForProviderType(providerKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForParentForProviderType(providerKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with providerKey ", providerKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        public OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = LoadHolonsForParentForProviderType(providerKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = LoadHolonsForParentForProviderType(providerKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with providerKey ", providerKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
                MapMetaData(result);

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        //TODO: Need to implement this proper way of calling an OASIS method across the entire OASIS...
        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = await LoadHolonsForParentForProviderTypeAsync(providerKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForParentForProviderTypeAsync(providerKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with providerKey ", providerKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = await LoadHolonsForParentForProviderTypeAsync(providerKey, holonType, providerType, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadHolonsForParentForProviderTypeAsync(providerKey, holonType, type.Value, loadChildren, recursive, maxChildDepth, continueOnError, version, result);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load holons for parent with providerKey ", providerKey, ", and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
                MapMetaData(result);

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = LoadAllHolonsForProviderType(holonType, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = LoadAllHolonsForProviderType(holonType, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        public OASISResult<IEnumerable<T>> LoadAllHolons<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = LoadAllHolonsForProviderType(holonType, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = LoadAllHolonsForProviderType(holonType, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
                MapMetaData(result);

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            result = await LoadAllHolonsForProviderTypeAsync(holonType, providerType, result);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadAllHolonsForProviderTypeAsync(holonType, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadAllHolonsAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();

            result = await LoadAllHolonsForProviderTypeAsync(holonType, providerType, result, loadChildren, recursive, maxChildDepth, continueOnError, version);

            if (result.Result == null && ProviderManager.IsAutoFailOverEnabled)
            {
                foreach (EnumValue<ProviderType> type in ProviderManager.GetProviderAutoFailOverList())
                {
                    if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                    {
                        result = await LoadAllHolonsForProviderTypeAsync(holonType, type.Value, result, loadChildren, recursive, maxChildDepth, continueOnError, version);

                        if (result.Result != null)
                            break;
                    }
                }
            }

            if (result.Result == null)
            {
                result.IsError = true;
                string errorMessage = string.Concat("All registered OASIS Providers in the AutoFailOverList failed to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), ". Please view the logs for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
            }
            else
                MapMetaData(result);

            SwitchBackToCurrentProvider(currentProviderType, ref result);

            // Store the original holon for change tracking in STAR/COSMIC.
            foreach (IHolon holon in result.Result)
                holon.Original = holon;

            return result;
        }

        
        public OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = SaveHolonForProviderType(PrepareHolonForSaving(holon, false), providerType, result, saveChildren, recursive, maxChildDepth, continueOnError);

            if ((result.IsError || result.Result == null) && ProviderManager.IsAutoFailOverEnabled)
            {
                ErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = SaveHolonForListOfProviders(holon, result, providerType, ProviderManager.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);
            
            else if (ProviderManager.IsAutoReplicationEnabled)
            { 
                result = SaveHolonForListOfProviders(holon, result, providerType, ProviderManager.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError);

                if (result.InnerMessages.Count > 0)
                    HandleSaveHolonErrorForAutoReplicateList(ref result);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            return result;
        }

        public OASISResult<T> SaveHolon<T>(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>((T)holon);
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();

            holonSaveResult = SaveHolonForProviderType(PrepareHolonForSaving(holon, true), providerType, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

            if ((holonSaveResult.IsError || holonSaveResult.Result == null) && ProviderManager.IsAutoFailOverEnabled)
            {
                ErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = SaveHolonForListOfProviders(holon, result, providerType, ProviderManager.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);
            else
            {
                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonSaveResult.Result, result.Result);

                if (ProviderManager.IsAutoReplicationEnabled)
                { 
                    result = SaveHolonForListOfProviders(holon, result, providerType, ProviderManager.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            return result;
        }

        
        //TODO: Need to implement this format to ALL other Holon/Avatar Manager methods with OASISResult, etc.
        public async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) 
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            result = await SaveHolonForProviderTypeAsync(PrepareHolonForSaving(holon, false), providerType, result, saveChildren, recursive, maxChildDepth, continueOnError);

            if ((result.IsError || result.Result == null) && ProviderManager.IsAutoFailOverEnabled)
            {
                ErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = await SaveHolonForListOfProvidersAsync(holon, result, providerType, ProviderManager.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);

            else if (ProviderManager.IsAutoReplicationEnabled)
            { 
                result = await SaveHolonForListOfProvidersAsync(holon, result, providerType, ProviderManager.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError);

                if (result.InnerMessages.Count > 0)
                    HandleSaveHolonErrorForAutoReplicateList(ref result);
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            return result;
        }


        //TODO: Need to implement this format to ALL other Holon/Avatar Manager methods with OASISResult, etc.
        public async Task<OASISResult<T>> SaveHolonAsync<T>(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<T> result = new OASISResult<T>((T)holon);
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();

            holonSaveResult = await SaveHolonForProviderTypeAsync(PrepareHolonForSaving(holon, true), providerType, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

            if ((holonSaveResult.IsError || holonSaveResult.Result == null) && ProviderManager.IsAutoFailOverEnabled)
            {
                ErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
                result.InnerMessages.Add(holonSaveResult.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = await SaveHolonForListOfProvidersAsync(holon, result, providerType, ProviderManager.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonErrorForAutoFailOverList(ref result, holon);
            else
            {
                result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonSaveResult.Result, result.Result);

                if (ProviderManager.IsAutoReplicationEnabled)
                { 
                    result = await SaveHolonForListOfProvidersAsync(holon, result, providerType, ProviderManager.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            result.IsSaved = result.Result != null && result.Result.Id != Guid.Empty;

            if (result.Result != null)
                result.Result.IsChanged = !result.IsSaved;

            return result;
        }

        
        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (holons.Count() == 0)
            {
                result.Message = "No holons found to save.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            result = SaveHolonsForProviderType(PrepareHolonsForSaving(holons, false), providerType, result, saveChildren, recursive, maxChildDepth, continueOnError);

            if ((result.IsError || result.Result == null) && ProviderManager.IsAutoFailOverEnabled)
            {
                ErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = SaveHolonsForListOfProviders(holons, result, providerType, ProviderManager.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonsErrorForAutoFailOverList(ref result);
            else
            {
                //Should already be false but just in case...
                foreach (IHolon holon in result.Result)
                    holon.IsChanged = false;

                if (ProviderManager.IsAutoReplicationEnabled)
                { 
                    result = SaveHolonsForListOfProviders(holons, result, providerType, ProviderManager.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonsErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        
        public OASISResult<IEnumerable<T>> SaveHolons<T>(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<IHolon>> holonSaveResult = new OASISResult<IEnumerable<IHolon>>();
            List<T> originalHolons = new List<T>();

            if (holons.Count() == 0)
            {
                result.Message = "No holons found to save.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            foreach (IHolon holon in holons)
                originalHolons.Add((T)holon);

            holonSaveResult = SaveHolonsForProviderType(PrepareHolonsForSaving(holons, true), providerType, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

            if ((holonSaveResult.IsError || holonSaveResult.Result == null) && ProviderManager.IsAutoFailOverEnabled)
            {
                ErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
                result.InnerMessages.Add(holonSaveResult.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = SaveHolonsForListOfProviders(holons, result, providerType, ProviderManager.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonsErrorForAutoFailOverList(ref result);
            else
            {
                List<IHolon> savedHolons = holonSaveResult.Result.ToList();

                for (int i=0; i < savedHolons.Count; i++)
                {
                    savedHolons[i].IsChanged = false;  //Should already be false but just in case...
                    savedHolons[i].IsNewHolon = false;

                    //Update the base holon properties that have now been updated (id, createddate, modifieddata, etc)
                    originalHolons[i] = Mapper<IHolon, T>.MapBaseHolonProperties(savedHolons[i], originalHolons[i]);
                }

                if (ProviderManager.IsAutoReplicationEnabled)
                { 
                    result = SaveHolonsForListOfProviders(holons, result, providerType, ProviderManager.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonsErrorForAutoReplicateList(ref result);
                }
            }

            result.Result = originalHolons;
            SwitchBackToCurrentProvider(currentProviderType, ref result);

            return result;
        }

        
        //TODO: Need to implement this format to ALL other Holon/Avatar Manager methods with OASISResult, etc.
        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (holons.Count() == 0)
            {
                result.Message = "No holons found to save.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            result = await SaveHolonsForProviderTypeAsync(PrepareHolonsForSaving(holons, false), providerType, result, saveChildren, recursive, maxChildDepth, continueOnError);

            if ((result.IsError || result.Result == null) && ProviderManager.IsAutoFailOverEnabled)
            {
                ErrorHandling.HandleError(ref result, result.Message);
                result.InnerMessages.Add(result.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = await SaveHolonsForListOfProvidersAsync(holons, result, providerType, ProviderManager.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonsErrorForAutoFailOverList(ref result);
            else
            {
                //Should already be false but just in case...
                foreach (IHolon holon in result.Result)
                    holon.IsChanged = false;

                if (ProviderManager.IsAutoReplicationEnabled)
                { 
                    result = await SaveHolonsForListOfProvidersAsync(holons, result, providerType, ProviderManager.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonsErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        //TODO: Need to implement this format to ALL other Holon/Avatar Manager methods with OASISResult, etc.
        public async Task<OASISResult<IEnumerable<T>>> SaveHolonsAsync<T>(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            ProviderType currentProviderType = ProviderManager.CurrentStorageProviderType.Value;
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            OASISResult<IEnumerable<IHolon>> holonSaveResult = new OASISResult<IEnumerable<IHolon>>();
            List<T> originalHolons = new List<T>();

            if (holons.Count() == 0)
            {
                result.Message = "No holons found to save.";
                result.IsWarning = true;
                result.IsSaved = false;
                return result;
            }

            foreach (IHolon holon in holons)
                originalHolons.Add((T)holon);

            holonSaveResult = await SaveHolonsForProviderTypeAsync(PrepareHolonsForSaving(holons, true), providerType, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

            if ((holonSaveResult.IsError || holonSaveResult.Result == null) && ProviderManager.IsAutoFailOverEnabled)
            {
                ErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
                result.InnerMessages.Add(holonSaveResult.Message);
                result.IsWarning = true;
                result.IsError = false;

                result = await SaveHolonsForListOfProvidersAsync(holons, result, providerType, ProviderManager.GetProviderAutoFailOverList(), "auto-failover", false, saveChildren, recursive, maxChildDepth, continueOnError);
            }

            if (result.InnerMessages.Count > 0)
                HandleSaveHolonsErrorForAutoFailOverList(ref result);
            else
            {
                List<IHolon> savedHolons = holonSaveResult.Result.ToList();

                for (int i = 0; i < savedHolons.Count; i++)
                {
                    savedHolons[i].IsChanged = false;  //Should already be false but just in case...
                    savedHolons[i].IsNewHolon = false;

                    //Update the base holon properties that have now been updated (id, createddate, modifieddata, etc)
                    originalHolons[i] = Mapper<IHolon, T>.MapBaseHolonProperties(savedHolons[i], originalHolons[i]);
                }

                if (ProviderManager.IsAutoReplicationEnabled)
                {
                    result = await SaveHolonsForListOfProvidersAsync(holons, result, providerType, ProviderManager.GetProvidersThatAreAutoReplicating(), "auto-replicate", true, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (result.InnerMessages.Count > 0)
                        HandleSaveHolonsErrorForAutoReplicateList(ref result);
                }
            }

            SwitchBackToCurrentProvider(currentProviderType, ref result);
            return result;
        }

        public OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    result = providerResult.Result.DeleteHolon(id, softDelete);

                    if (!result.IsError && result.Result && ProviderManager.IsAutoReplicationEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                        {
                            if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            {
                                try
                                {
                                    OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                                    if (autoReplicateProviderResult.IsError)
                                    {
                                        result.IsError = true;
                                        result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                    }
                                    else
                                    {
                                        result = autoReplicateProviderResult.Result.DeleteHolon(id, softDelete);

                                        if (result.IsError)
                                            result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value)));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                    result.IsError = true;
                                    result.InnerMessages.Add(errorMessage);
                                    LoggingManager.Log(errorMessage, LogType.Error);
                                    result.Exception = ex;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolon attempting to auto-replicate the deletion of the holon with id: ", id, ", softDelete = ", softDelete);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    result = await providerResult.Result.DeleteHolonAsync(id, softDelete);

                    if (!result.IsError && result.Result && ProviderManager.IsAutoReplicationEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                        {
                            if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            {
                                try
                                {
                                    OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                                    if (autoReplicateProviderResult.IsError)
                                    {
                                        result.IsError = true;
                                        result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                    }
                                    else
                                    {
                                        result = await autoReplicateProviderResult.Result.DeleteHolonAsync(id, softDelete);

                                        if (result.IsError)
                                            result.InnerMessages.Add(string.Concat("An error occured in DeleteHolonAsync method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value)));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    string errorMessage = string.Concat("An unknown error occured in DeleteHolonAsync method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                    result.IsError = true;
                                    result.InnerMessages.Add(errorMessage);
                                    LoggingManager.Log(errorMessage, LogType.Error);
                                    result.Exception = ex;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolonAsync method. id: ", id, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolonAsync attempting to auto-replicate the deletion of the holon with id: ", id, ", softDelete = ", softDelete);
            }

            return result;
        }

        public OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    result = providerResult.Result.DeleteHolon(providerKey, softDelete);

                    if (!result.IsError && result.Result && ProviderManager.IsAutoReplicationEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                        {
                            if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            {
                                try
                                {
                                    OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                                    if (autoReplicateProviderResult.IsError)
                                    {
                                        result.IsError = true;
                                        result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                    }
                                    else
                                    {
                                        result = autoReplicateProviderResult.Result.DeleteHolon(providerKey, softDelete);

                                        if (result.IsError)
                                            result.InnerMessages.Add(string.Concat("An error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value)));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                    result.IsError = true;
                                    result.InnerMessages.Add(errorMessage);
                                    LoggingManager.Log(errorMessage, LogType.Error);
                                    result.Exception = ex;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolon method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolon attempting to auto-replicate the deletion of the holon with providerKey: ", providerKey, ", softDelete = ", softDelete);
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    result.IsError = true;
                    result.Message = providerResult.Message;
                }
                else
                {
                    result = await providerResult.Result.DeleteHolonAsync(providerKey, softDelete);

                    if (!result.IsError && result.Result && ProviderManager.IsAutoReplicationEnabled)
                    {
                        foreach (EnumValue<ProviderType> type in ProviderManager.GetProvidersThatAreAutoReplicating())
                        {
                            if (type.Value != providerType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                            {
                                try
                                {
                                    OASISResult<IOASISStorageProvider> autoReplicateProviderResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                                    if (autoReplicateProviderResult.IsError)
                                    {
                                        result.IsError = true;
                                        result.InnerMessages.Add(autoReplicateProviderResult.Message);
                                    }
                                    else
                                    {
                                        result = await autoReplicateProviderResult.Result.DeleteHolonAsync(providerKey, softDelete);

                                        if (result.IsError)
                                            result.InnerMessages.Add(string.Concat("An error occured in DeleteHolonAsync method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value)));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    string errorMessage = string.Concat("An unknown error occured in DeleteHolonAsync method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), type.Value), " Error details: ", ex.ToString());
                                    result.IsError = true;
                                    result.InnerMessages.Add(errorMessage);
                                    LoggingManager.Log(errorMessage, LogType.Error);
                                    result.Exception = ex;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An unknown error occured in DeleteHolonAsync method. providerKey: ", providerKey, ", softDelete = ", softDelete, ", providerType = ", Enum.GetName(typeof(ProviderType), providerType), " Error details: ", ex.ToString());
                result.IsError = true;
                result.Message = errorMessage;
                LoggingManager.Log(errorMessage, LogType.Error);
                result.Exception = ex;
            }

            if (result.InnerMessages.Count > 0)
            {
                result.IsError = true;
                result.Message = string.Concat("More than one error occured in DeleteHolonAsync attempting to auto-replicate the deletion of the holon with providerKey: ", providerKey, ", softDelete = ", softDelete);
            }

            return result;
        }

        private IHolon PrepareHolonForSaving(IHolon holon, bool extractMetaData)
        {
            // TODO: I think it's best to include audit stuff here so the providers do not need to worry about it?
            // Providers could always override this behaviour if they choose...

            if (holon.Id == Guid.Empty || holon.CreatedDate == DateTime.MinValue)
            {
                if (holon.Id == Guid.Empty)
                    holon.Id = Guid.NewGuid();

                holon.IsNewHolon = true;
            }
            else if (holon.CreatedDate != DateTime.MinValue)
                holon.IsNewHolon = false;

            //if (holon.Id != Guid.Empty)
            if (!holon.IsNewHolon)
            {
                holon.ModifiedDate = DateTime.Now;

                if (AvatarManager.LoggedInAvatar != null)
                    holon.ModifiedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                holon.Version++;
                holon.PreviousVersionId = holon.VersionId;
                holon.VersionId = Guid.NewGuid();
            }
            else
            {
                holon.IsActive = true;
                holon.CreatedDate = DateTime.Now;

                if (AvatarManager.LoggedInAvatar != null)
                    holon.CreatedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                holon.Version = 1;
                holon.VersionId = Guid.NewGuid();
            }

            // Retreive any custom properties and store in the holon metadata dictionary.
            // TODO: Would ideally like to find a better way to do this so we can avoid reflection if possible because of the potential overhead!
            // Need to do some perfomrnace tests with reflection turned on/off (so with this code enabled/disabled) to see what the overhead is exactly...

            // We only want to extract the meta data for sub-classes of Holon that are calling the Generic overloads.
            if (holon.GetType() != typeof(Holon) && extractMetaData)
            {
                PropertyInfo[] props = holon.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo propertyInfo in props)
                {
                    foreach (CustomAttributeData data in propertyInfo.CustomAttributes)
                    {
                        if (data.AttributeType == (typeof(CustomOASISProperty)))
                        {
                            holon.MetaData[propertyInfo.Name] = propertyInfo.GetValue(holon).ToString();
                            break;
                        }
                    }
                }
            }

            return holon;
        }

        private IEnumerable<IHolon> PrepareHolonsForSaving(IEnumerable<IHolon> holons, bool extractMetaData)
        {
            List<IHolon> holonsToReturn = new List<IHolon>();

            foreach (IHolon holon in holons)
                holonsToReturn.Add(PrepareHolonForSaving(holon, extractMetaData));

            return holonsToReturn;
        }

        private void LogError(IHolon holon, ProviderType providerType, string errorMessage)
        {
            LoggingManager.Log(string.Concat("An error occured attempting to save the ", LoggingHelper.GetHolonInfoForLogging(holon), " using the ", Enum.GetName(providerType), " provider. Error Details: ", errorMessage), LogType.Error);
        }

        private OASISResult<IHolon> LoadHolonForProviderType(Guid id, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    result = providerResult.Result.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for id ", id, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<T> LoadHolonForProviderType<T>(Guid id, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for id ", id, " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T));
                    OASISResult<IHolon> holonResult = providerResult.Result.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, version);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonResult.Message}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

                if (result != null)
                {
                    result.Result = default(T);
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> LoadHolonForProviderTypeAsync(Guid id, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    result = await providerResult.Result.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for id ", id, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<T>> LoadHolonForProviderTypeAsync<T>(Guid id, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for id ", id, " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IHolon> holonResult = await providerResult.Result.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonResult.Message}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

                if (result != null)
                {
                    result.Result = default(T);
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IHolon> LoadHolonForProviderType(string providerKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    result = providerResult.Result.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for providerKey ", providerKey, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<T> LoadHolonForProviderType<T>(string providerKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for providerKey ", providerKey, " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IHolon> holonResult = providerResult.Result.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonResult.Message}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

                if (result != null)
                {
                    result.Result = default(T);
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> LoadHolonForProviderTypeAsync(string providerKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IHolon> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    result = await providerResult.Result.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holon for providerKey ", providerKey, " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<T>> LoadHolonForProviderTypeAsync<T>(string providerKey, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<T> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holon for providerKey ", providerKey, " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IHolon> holonResult = await providerResult.Result.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonResult.Message}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

                if (result != null)
                {
                    result.Result = default(T);
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> LoadHolonsForParentForProviderType(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    result = providerResult.Result.LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> LoadHolonsForParentForProviderType<T>(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?

                    OASISResult<IEnumerable<IHolon>> holonsResult = providerResult.Result.LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonsResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonsResult.Message}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentForProviderTypeAsync(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    result = await providerResult.Result.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentForProviderTypeAsync<T>(Guid id, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IEnumerable<IHolon>> holonsResult = await providerResult.Result.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonsResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonsResult.Message}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> LoadHolonsForParentForProviderType(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    result = providerResult.Result.LoadHolonsForParent(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> LoadHolonsForParentForProviderType<T>(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load the holons for parent with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IEnumerable<IHolon>> holonResult = providerResult.Result.LoadHolonsForParent(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonResult.Message}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentForProviderTypeAsync(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<IHolon>> result = null)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    result = await providerResult.Result.LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentForProviderTypeAsync<T>(string providerKey, HolonType holonType, ProviderType providerType, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, OASISResult<IEnumerable<T>> result = null) where T : IHolon, new()
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IEnumerable<IHolon>> loadHolonsForParentResult = await providerResult.Result.LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

                    if (!loadHolonsForParentResult.IsError && loadHolonsForParentResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(loadHolonsForParentResult.Result);
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = loadHolonsForParentResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load the holons for parent with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> LoadAllHolonsForProviderType(HolonType holonType, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    result = providerResult.Result.LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> LoadAllHolonsForProviderType<T>(HolonType holonType, ProviderType providerType, OASISResult<IEnumerable<T>> result, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?

                    OASISResult<IEnumerable<IHolon>> holonsResult = providerResult.Result.LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonsResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonsResult.Message}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsForProviderTypeAsync(HolonType holonType, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    result = await providerResult.Result.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);
                    result.IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> LoadAllHolonsForProviderTypeAsync<T>(HolonType holonType, ProviderType providerType, OASISResult<IEnumerable<T>> result, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new()
        {
            string errorMessage = string.Concat("An error occured attempting to load all holons for holonType ", Enum.GetName(typeof(HolonType), holonType), " using the ", Enum.GetName(providerType), " provider.");

            try
            {
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }
                }
                else if (result != null)
                {
                    T convertedHolon = (T)Activator.CreateInstance(typeof(T)); //TODO: Need to find faster alternative to relfection... maybe JSON?
                    OASISResult<IEnumerable<IHolon>> holonsResult = await providerResult.Result.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, version);

                    if (holonsResult != null && !holonsResult.IsError && holonsResult.Result != null)
                    {
                        result.Result = Mapper<IHolon, T>.MapBaseHolonProperties(holonsResult.Result);
                        result.IsSaved = true;
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} Reason: {holonsResult.Message}");
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"{errorMessage} Reason: {ex}";

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        //TODO: Why do we pass in result?! Need to look into this tomorrow! ;-) lol
        private OASISResult<IHolon> SaveHolonForProviderType(IHolon holon, ProviderType providerType, OASISResult<IHolon> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    OASISResult<IHolon> saveHolonResult = providerResult.Result.SaveHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (!saveHolonResult.IsError && saveHolonResult != null)
                    {
                        result.Result = saveHolonResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, $"An error occured attempting to save the {LoggingHelper.GetHolonInfoForLogging(holon)} in the SaveHolonAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex.ToString()}");
                }
                else
                    LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private async Task<OASISResult<IHolon>> SaveHolonForProviderTypeAsync(IHolon holon, ProviderType providerType, OASISResult<IHolon> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) //TODO: Dont think this should be an optional param?!
        {
            try
            {
                HasHolonChanged(holon, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    OASISResult<IHolon> saveHolonResult = await providerResult.Result.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (!saveHolonResult.IsError && saveHolonResult != null)
                    {
                        result.Result = saveHolonResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, $"An error occured attempting to save the {LoggingHelper.GetHolonInfoForLogging(holon)} in the SaveHolonAsync method for the {Enum.GetName(typeof(ProviderType), providerType)} provider. Reason: {ex.ToString()}");
                }
                else
                    LogError(holon, providerType, ex.ToString());
            }

            return result;
        }

        private OASISResult<IEnumerable<IHolon>> SaveHolonsForProviderType(IEnumerable<IHolon> holons, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = providerResult.Result.SaveHolons(holons, saveChildren, recursive, maxChildDepth, 0, continueOnError);

                    if (!saveHolonsResult.IsError && saveHolonsResult != null)
                    {
                        result.Result = saveHolonsResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonsResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = string.Concat("An error occured attempting to save the holons in the SaveHolons method using the ", Enum.GetName(providerType), " provider. Error Details: ", ex.ToString());

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsForProviderTypeAsync(IEnumerable<IHolon> holons, ProviderType providerType, OASISResult<IEnumerable<IHolon>> result, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true)
        {
            try
            {
                HasAnyHolonsChanged(holons, ref result);

                if (!result.HasAnyHolonsChanged)
                    return result;

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(providerType);

                if (providerResult.IsError)
                {
                    LoggingManager.Log(providerResult.Message, LogType.Error);

                    if (result != null)
                    {
                        result.IsError = true;
                        result.Message = providerResult.Message;
                    }

                    //TODO: In future will return these extra error messages in the OASISResult.
                }
                else if (result != null)
                {
                    OASISResult<IEnumerable<IHolon>> saveHolonsResult = await providerResult.Result.SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, 0, continueOnError);

                    if (!saveHolonsResult.IsError && saveHolonsResult != null)
                    {
                        result.Result = saveHolonsResult.Result;
                        result.IsSaved = true;
                    }
                    else
                    {
                        result.IsError = true;
                        result.Message = saveHolonsResult.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"An error occured attempting to save the holons in the SaveHolonsAsync method for the {Enum.GetName(providerType)} provider. Reason: {ex.ToString()}";

                if (result != null)
                {
                    result.Result = null;
                    ErrorHandling.HandleError(ref result, errorMessage);
                }
                else
                    LoggingManager.Log(errorMessage, LogType.Error);
            }

            return result;
        }

        private OASISResult<IEnumerable<T>> SaveHolonsForListOfProviders<T>(IEnumerable<IHolon> holons, OASISResult<IEnumerable<T>> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon
        {
            OASISResult<IEnumerable<IHolon>> holonSaveResult = new OASISResult<IEnumerable<IHolon>>();
            ProviderType originalCurrentProvider = ProviderManager.CurrentStorageProviderType.Value;

            HasAnyHolonsChanged(holons, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = SaveHolonsForProviderType(holons, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private async Task<OASISResult<IEnumerable<T>>> SaveHolonsForListOfProvidersAsync<T>(IEnumerable<IHolon> holons, OASISResult<IEnumerable<T>> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon
        {
            OASISResult<IEnumerable<IHolon>> holonSaveResult = new OASISResult<IEnumerable<IHolon>>();
            ProviderType originalCurrentProvider = ProviderManager.CurrentStorageProviderType.Value;

            HasAnyHolonsChanged(holons, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = await SaveHolonsForProviderTypeAsync(holons, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private OASISResult<T> SaveHolonForListOfProviders<T>(IHolon holon, OASISResult<T> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon
        {
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();
            ProviderType originalCurrentProvider = ProviderManager.CurrentStorageProviderType.Value;

            HasHolonChanged(holon, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                if (type.Value != originalCurrentProvider)
                { 
                    holonSaveResult = SaveHolonForProviderType(holon, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private async Task<OASISResult<T>> SaveHolonForListOfProvidersAsync<T>(IHolon holon, OASISResult<T> result, ProviderType currentProviderType, List<EnumValue<ProviderType>> providers, string listName, bool continueOSuccess, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true) where T : IHolon
        {
            OASISResult<IHolon> holonSaveResult = new OASISResult<IHolon>();
            ProviderType originalCurrentProvider = ProviderManager.CurrentStorageProviderType.Value;

            HasHolonChanged(holon, ref result);

            if (!result.HasAnyHolonsChanged)
                return result;

            foreach (EnumValue<ProviderType> type in providers)
            {
                //if (type.Value != currentProviderType && type.Value != ProviderManager.CurrentStorageProviderType.Value)
                if (type.Value != originalCurrentProvider)
                {
                    holonSaveResult = await SaveHolonForProviderTypeAsync(holon, type.Value, holonSaveResult, saveChildren, recursive, maxChildDepth, continueOnError);

                    if (holonSaveResult.IsError || holonSaveResult.Result == null)
                        HandleSaveHolonForListOfProviderError(result, holonSaveResult, listName, type.Name);

                    else if (!continueOSuccess)
                        break;
                }
            }

            return result;
        }

        private OASISResult<T> HandleSaveHolonForListOfProviderError<T>(OASISResult<T> result, OASISResult<IHolon> holonSaveResult, string listName, string providerName) where T : IHolon
        {
            holonSaveResult.Message = GetSaveHolonForListOfProvidersErrorMessage(listName, providerName, holonSaveResult.Message);
            ErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
            result.InnerMessages.Add(holonSaveResult.Message);
            result.IsWarning = true;
            result.IsError = false;
            return result;
        }

        private OASISResult<IEnumerable<T>> HandleSaveHolonForListOfProviderError<T>(OASISResult<IEnumerable<T>> result, OASISResult<IEnumerable<IHolon>> holonSaveResult, string listName, string providerName) where T : IHolon
        {
            holonSaveResult.Message = GetSaveHolonForListOfProvidersErrorMessage(listName, providerName, holonSaveResult.Message);
            ErrorHandling.HandleError(ref holonSaveResult, holonSaveResult.Message);
            result.InnerMessages.Add(holonSaveResult.Message);
            result.IsWarning = true;
            result.IsError = false;
            return result;
        }

        private string GetSaveHolonForListOfProvidersErrorMessage(string listName, string providerName, string holoSaveResultErrorMessage)
        {
            return $"Error attempting to save in {listName} list for provider {providerName}. Reason: {holoSaveResultErrorMessage}";
        }

        private void SwitchBackToCurrentProvider<T>(ProviderType currentProviderType, ref OASISResult<T> result)
        {
            OASISResult<IOASISStorageProvider> providerResult = ProviderManager.SetAndActivateCurrentStorageProvider(currentProviderType);

            if (providerResult.IsError)
            {
                result.IsWarning = true; //TODO: Not sure if this should be an error or a warning? Because there was no error saving the holons but an error switching back to the current provider.                
                //result.InnerMessages.Add(string.Concat("The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message));
                result.Message = string.Concat(result.Message, ". The holons saved but there was an error switching the default provider back to ", Enum.GetName(typeof(ProviderType), currentProviderType), " provider. Error Details: ", providerResult.Message);
            }
        }

        private void MapMetaData<T>(OASISResult<IEnumerable<T>> result) where T : IHolon
        {
            List<T> holons = result.Result.ToList();
            for (int i = 0; i < holons.Count(); i++)
            {
                if (holons[i].MetaData != null)
                    holons[i] = (T)MapMetaData<T>(holons[i]);
            }
        }

        private IHolon MapMetaData<T>(IHolon holon)
        {
            foreach (string key in holon.MetaData.Keys)
            {
                PropertyInfo propInfo = typeof(T).GetProperty(key);

                if (propInfo.PropertyType == typeof(Guid))
                    propInfo.SetValue(holon, new Guid(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(bool))
                    propInfo.SetValue(holon, Convert.ToBoolean(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(DateTime))
                    propInfo.SetValue(holon, Convert.ToDateTime(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(int))
                    propInfo.SetValue(holon, Convert.ToInt32(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(long))
                    propInfo.SetValue(holon, Convert.ToInt64(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(float))
                    propInfo.SetValue(holon, Convert.ToDouble(holon.MetaData[key])); //TODO: Check if this is right?! :)

                else if (propInfo.PropertyType == typeof(double))
                    propInfo.SetValue(holon, Convert.ToDouble(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(decimal))
                    propInfo.SetValue(holon, Convert.ToDecimal(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(UInt16))
                    propInfo.SetValue(holon, Convert.ToUInt16(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(UInt32))
                    propInfo.SetValue(holon, Convert.ToUInt32(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(UInt64))
                    propInfo.SetValue(holon, Convert.ToUInt64(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(Single))
                    propInfo.SetValue(holon, Convert.ToSingle(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(char))
                    propInfo.SetValue(holon, Convert.ToChar(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(byte))
                    propInfo.SetValue(holon, Convert.ToByte(holon.MetaData[key]));

                else if (propInfo.PropertyType == typeof(sbyte))
                    propInfo.SetValue(holon, Convert.ToSByte(holon.MetaData[key]));

                else
                    propInfo.SetValue(holon, holon.MetaData[key]);

                //TODO: Add any other missing types...
            }

            return holon;
        }

        //private string BuildInnerMessageError(List<string> innerMessages)
        //{
        //    string result = "";
        //    foreach (string innerMessage in innerMessages)
        //        result = string.Concat(result, innerMessage, "\n\n");

        //    return result;
        //}

        private OASISResult<T> HasHolonChanged<T>(IHolon holon, ref OASISResult<T> result)
        {
            //TODO: TEMP! REMOVE ONCE FINISH IMPLEMENTING HASHOLONCHANGED METHOD BELOW...
            result.HasAnyHolonsChanged = true;
            return result;

            if (!holon.HasHolonChanged())
            {
                result.Message = "No changes need saving";
                result.HasAnyHolonsChanged = false;
            }
            else
                result.HasAnyHolonsChanged = true;

            return result;
        }

        private OASISResult<T> HasAnyHolonsChanged<T>(IEnumerable<IHolon> holons, ref OASISResult<T> result)
        {
            //TODO: TEMP! REMOVE ONCE FINISH IMPLEMENTING HASHOLONCHANGED METHOD BELOW...
            result.HasAnyHolonsChanged = true;
            return result;

            foreach (IHolon holon in holons)
            {
                if (holon.HasHolonChanged())
                {
                    result.HasAnyHolonsChanged = true;
                    break;
                }
            }

            if (!result.HasAnyHolonsChanged)
                result.Message = "No changes need saving";

            return result;
        }

        private string BuildSaveHolonAutoFailOverErrorMessage(List<string> innerMessages, IHolon holon = null)
        {
            return string.Concat("All registered OASIS Providers in the AutoFailOver List failed to save ", holon != null ? LoggingHelper.GetHolonInfoForLogging(holon) : "", ". Reason: ", OASISResultHelper.BuildInnerMessageError(innerMessages), "Please view the logs and InnerMessages property for more information. Providers in the list are: ", ProviderManager.GetProviderAutoFailOverListAsString());
        }

        private string BuildSaveHolonAutoReplicateErrorMessage(List<string> innerMessages, IHolon holon = null)
        {
            return string.Concat("One or more registered OASIS Providers in the AutoReplicate List failed to save ", holon != null ? LoggingHelper.GetHolonInfoForLogging(holon) : "", ". Reason: ", OASISResultHelper.BuildInnerMessageError(innerMessages), "Please view the logs and InnerMessages property for more information. Providers in the list are: ", ProviderManager.GetProvidersThatAreAutoReplicatingAsString());
        }

        private void HandleSaveHolonsErrorForAutoFailOverList<T>(ref OASISResult<IEnumerable<T>> result, IHolon holon = null) where T : IHolon
        {
            ErrorHandling.HandleError(ref result, BuildSaveHolonAutoFailOverErrorMessage(result.InnerMessages, holon));
        }

        private void HandleSaveHolonErrorForAutoFailOverList<T>(ref OASISResult<T> result, IHolon holon = null) where T : IHolon
        {
            ErrorHandling.HandleError(ref result, BuildSaveHolonAutoFailOverErrorMessage(result.InnerMessages, holon));
        }

        private void HandleSaveHolonsErrorForAutoReplicateList<T>(ref OASISResult<IEnumerable<T>> result, IHolon holon = null) where T : IHolon
        {
            ErrorHandling.HandleWarning(ref result, BuildSaveHolonAutoReplicateErrorMessage(result.InnerMessages, holon));
        }

        private void HandleSaveHolonErrorForAutoReplicateList<T>(ref OASISResult<T> result, IHolon holon = null) where T : IHolon
        {
            ErrorHandling.HandleWarning(ref result, BuildSaveHolonAutoReplicateErrorMessage(result.InnerMessages, holon));
        }
    }
} 