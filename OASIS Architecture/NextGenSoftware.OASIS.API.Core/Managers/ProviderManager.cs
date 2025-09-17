using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    //TODO: Need to upgrade all methods to return OASISResult wrapper ASAP! :)
    public class ProviderManager : OASISManager
    {
        private static ProviderManager _instance = null;
        private List<IOASISProvider> _registeredProviders = new List<IOASISProvider>();
        private List<EnumValue<ProviderType>> _registeredProviderTypes = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverList { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverListForCheckIfOASISSystemAccountExists { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverListForAvatarLogin { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverListForCheckIfEmailAlreadyInUse { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoFailOverListForCheckIfUsernameAlreadyInUse { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providersThatAreAutoReplicating { get; set; } = new List<EnumValue<ProviderType>>();
        private List<EnumValue<ProviderType>> _providerAutoLoadBalanceList { get; set; } = new List<EnumValue<ProviderType>>();
        private bool _setProviderGlobally = false;

        public EnumValue<ProviderType> CurrentStorageProviderType { get; private set; } = new EnumValue<ProviderType>(ProviderType.Default);
        public EnumValue<ProviderCategory> CurrentStorageProviderCategory { get; private set; } = new EnumValue<ProviderCategory>(ProviderCategory.None);
        public OASISProviderBootType OASISProviderBootType { get; set; } = OASISProviderBootType.Hot;

        public bool IsAutoReplicationEnabled { get; set; } = true;
        public bool IsAutoLoadBalanceEnabled { get; set; } = true;
        public bool IsAutoFailOverEnabled { get; set; } = true;
        //public bool IsAutoFailOverEnabledForAvatarLogin { get; set; } = true;
        //public bool IsAutoFailOverEnabledForCheckIfEmailAlreadyInUse { get; set; } = true;
        //public bool IsAutoFailOverEnabledForCheckIfUsernameAlreadyInUse { get; set; } = true;

        //public  string CurrentStorageProviderName
        //{
        //    get
        //    {
        //        return Enum.GetName(CurrentStorageProviderType);
        //    }
        //}

        // public  string[] DefaultProviderTypes { get; set; }

        public IOASISStorageProvider DefaultGlobalStorageProvider { get; set; }

        public IOASISStorageProvider CurrentStorageProvider { get; private set; } //TODO: Need to work this out because in future there can be more than one provider active at a time.

        public bool OverrideProviderType { get; set; } = false;


        //public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        public static ProviderManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ProviderManager(null);

                return _instance;
            }
        }

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public ProviderManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        //TODO: In future the registered providers will be dynamically loaded from MEF by watching a hot folder for compiled provider dlls (and other ways in future...)
        public bool RegisterProvider(IOASISProvider provider)
        {
            if (!_registeredProviders.Any(x => x.ProviderType == provider.ProviderType))
            {
                _registeredProviders.Add(provider);
                _registeredProviderTypes.Add(provider.ProviderType);
                return true;
            }

            return false;
        }

        public bool RegisterProviders(List<IOASISProvider> providers)
        {
            bool returnValue = false;

            foreach (IOASISProvider provider in providers)
                returnValue = RegisterProvider(provider);

            return returnValue;
        }

        public bool UnRegisterProvider(IOASISProvider provider)
        {
            DeActivateProvider(provider);

            _registeredProviders.Remove(provider);
            _registeredProviderTypes.Remove(provider.ProviderType);
            return true;
        }

        public bool UnRegisterProvider(ProviderType providerType)
        {
            foreach (IOASISProvider provider in _registeredProviders)
            {
                if (provider.ProviderType.Value == providerType)
                {
                    UnRegisterProvider(provider);
                    break;
                }
            }    
            
            return true;
        }

        public bool UnRegisterProviders(List<ProviderType> providerTypes)
        {
            foreach (ProviderType providerType in providerTypes)
                UnRegisterProvider(providerType);

            return true;
        }

        public bool UnRegisterProviders(List<IOASISProvider> providers)
        {
            foreach (IOASISProvider provider in providers)
                _registeredProviders.Remove(provider);

            return true;
        }

        public ProviderCategory GetProviderCategory(ProviderType providerType)
        {
            foreach (IOASISProvider provider in _registeredProviders)
            {
                if (provider.ProviderType.Value == providerType)
                    return provider.ProviderCategory.Value;
            }

            return ProviderCategory.None;
        }

        public List<IOASISProvider> GetAllRegisteredProviders()
        {
            return _registeredProviders;
        }

        public List<EnumValue<ProviderType>> GetAllRegisteredProviderTypes()
        {
            return _registeredProviderTypes;
        }

        public List<IOASISProvider> GetProvidersOfCategory(ProviderCategory category)
        {
            return _registeredProviders.Where(x => x.ProviderCategory.Value == category).ToList();
        }

        public List<ProviderType> GetProviderTypesOfCategory(ProviderCategory category)
        {
            return GetProviderTypes(GetProvidersOfCategory(category));
        }

        public List<IOASISStorageProvider> GetStorageProviders()
        {
            List<IOASISStorageProvider> storageProviders = new List<IOASISStorageProvider>();

            foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategory.Value == ProviderCategory.Storage || x.ProviderCategory.Value == ProviderCategory.StorageAndNetwork || x.ProviderCategory.Value == ProviderCategory.StorageLocal || x.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork).ToList())
                storageProviders.Add((IOASISStorageProvider)provider);  

            return storageProviders;
        }

        public List<ProviderType> GetStorageProviderTypes()
        {
            //return GetProviderTypes(GetStorageProviders().Select(x => x.ProviderType);
            return GetStorageProviders().Select(x => x.ProviderType.Value).ToList();
        }

        public List<IOASISNETProvider> GetNetworkProviders()
        {
            List<IOASISNETProvider> networkProviders = new List<IOASISNETProvider>();

            foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategory.Value == ProviderCategory.Network || x.ProviderCategory.Value == ProviderCategory.StorageAndNetwork || x.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork).ToList())
                networkProviders.Add((IOASISNETProvider)provider);

            return networkProviders;
        }

        public List<ProviderType> GetNetworkProviderTypes()
        {
            return GetNetworkProviders().Select(x => x.ProviderType.Value).ToList();

            //List<ProviderType> providerTypes = new List<ProviderType>();

            //foreach (IOASISProvider provider in GetNetworkProviders())
            //    providerTypes.Add(provider.ProviderType);

            //return providerTypes;
        }

        public List<ProviderType> GetProviderTypes(List<IOASISProvider> providers)
        {
            List<ProviderType> providerTypes = new List<ProviderType>();

            foreach (IOASISProvider provider in providers)
                providerTypes.Add(provider.ProviderType.Value);

            return providerTypes;
        }

        public List<IOASISRenderer> GetRendererProviders()
        {
            List<IOASISRenderer> rendererProviders = new List<IOASISRenderer>();

            foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategory.Value == ProviderCategory.Renderer).ToList())
                rendererProviders.Add((IOASISRenderer)provider);

            return rendererProviders;
        }

        public IOASISProvider GetProvider(ProviderType type)
        {
            return _registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type);
        }

        public IOASISStorageProvider GetStorageProvider(ProviderType type)
        {
            return (IOASISStorageProvider)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type);
            //return (IOASISStorageProvider)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type && x.ProviderCategory.Value == ProviderCategory.Storage);
        }

        public IOASISNETProvider GetNetworkProvider(ProviderType type)
        {
            return (IOASISNETProvider)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type);
            //return (IOASISNETProvider)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type && x.ProviderCategory.Value == ProviderCategory.Network);
        }

        public IOASISRenderer GetRendererProvider(ProviderType type)
        {
            return (IOASISRenderer)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type);
            //return (IOASISRenderer)_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type && x.ProviderCategory.Value == ProviderCategory.Renderer);
        }

        public bool IsProviderRegistered(IOASISProvider provider)
        {
            return _registeredProviders.Any(x => x.ProviderName == provider.ProviderName);
        }

        public bool IsProviderRegistered(ProviderType providerType)
        {
            return _registeredProviders.Any(x => x.ProviderType.Value == providerType);
        }

        //public  IOASISSuperStar SetAndActivateCurrentSuperStarProvider(ProviderType providerType)
        //{
        //    SetAndActivateCurrentStorageProvider(providerType);

            
      //  }

        // Called from Managers.
        public OASISResult<IOASISStorageProvider> SetAndActivateCurrentStorageProvider(ProviderType providerType)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            if (providerType == ProviderType.Default)
                result = SetAndActivateCurrentStorageProvider();
            else
                result = SetAndActivateCurrentStorageProvider(providerType, false);

            if (result.IsError)
                result.Message = string.Concat("ERROR: The ", Enum.GetName(providerType), " provider may not be registered. Please register it before calling this method. Reason: ", result.Message);

            return result;
        }

        public async Task<OASISResult<IOASISStorageProvider>> SetAndActivateCurrentStorageProviderAsync(ProviderType providerType)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            if (providerType == ProviderType.Default)
                result = await SetAndActivateCurrentStorageProviderAsync();
            else
                result = await SetAndActivateCurrentStorageProviderAsync(providerType, false);

            if (result.IsError)
                result.Message = string.Concat("ERROR: The ", Enum.GetName(providerType), " provider may not be registered. Please register it before calling this method. Reason: ", result.Message);

            return result;
        }

        //TODO: Called internally (make private ?)
        public async Task<OASISResult<IOASISStorageProvider>> SetAndActivateCurrentStorageProviderAsync()
        {
            // If a global provider has been set and the REST API call has not overiden the provider (OverrideProviderType) then set to global provider.
            if (DefaultGlobalStorageProvider != null && DefaultGlobalStorageProvider != CurrentStorageProvider && !OverrideProviderType)
                return await SetAndActivateCurrentStorageProviderAsync(DefaultGlobalStorageProvider);

            // Otherwise set to default provider (configured in appSettings.json) if the provider has not been overiden in the REST call.
            //else if (!OverrideProviderType && DefaultProviderTypes != null && CurrentStorageProviderType.Value != (ProviderType)Enum.Parse(typeof(ProviderType), DefaultProviderTypes[0]))
            else if (!OverrideProviderType && _providerAutoFailOverList.Count > 0 && CurrentStorageProviderType.Value != _providerAutoFailOverList[0].Value) // TODO: Come back to this, not sure we should be setting the first entry every time? Needs thinking and testing through! ;-)
                return await SetAndActivateCurrentStorageProviderAsync(ProviderType.Default, false);

            if (!_setProviderGlobally)
                OverrideProviderType = false;

            return new OASISResult<IOASISStorageProvider>(CurrentStorageProvider);
        }

        //TODO: Called internally (make private ?)
        public OASISResult<IOASISStorageProvider> SetAndActivateCurrentStorageProvider()
        {
            // If a global provider has been set and the REST API call has not overiden the provider (OverrideProviderType) then set to global provider.
            if (DefaultGlobalStorageProvider != null && DefaultGlobalStorageProvider != CurrentStorageProvider && !OverrideProviderType)
                return SetAndActivateCurrentStorageProvider(DefaultGlobalStorageProvider);

            // Otherwise set to default provider (configured in appSettings.json) if the provider has not been overiden in the REST call.
            //else if (!OverrideProviderType && DefaultProviderTypes != null && CurrentStorageProviderType.Value != (ProviderType)Enum.Parse(typeof(ProviderType), DefaultProviderTypes[0]))
            else if (!OverrideProviderType && _providerAutoFailOverList.Count > 0 && CurrentStorageProviderType.Value != _providerAutoFailOverList[0].Value) // TODO: Come back to this, not sure we should be setting the first entry every time? Needs thinking and testing through! ;-)
                return SetAndActivateCurrentStorageProvider(ProviderType.Default, false);

            if (!_setProviderGlobally)
                OverrideProviderType = false;

            return new OASISResult<IOASISStorageProvider>(CurrentStorageProvider);
        }

        // Called from ONODE.WebAPI.OASISProviderManager.
        public OASISResult<IOASISStorageProvider> SetAndActivateCurrentStorageProvider(IOASISProvider OASISProvider)
        {
            if (OASISProvider != CurrentStorageProvider)
            {
                if (OASISProvider != null)
                {
                    if (!IsProviderRegistered(OASISProvider))
                        RegisterProvider(OASISProvider);

                    return SetAndActivateCurrentStorageProvider(OASISProvider.ProviderType.Value);
                }
            }

            return new OASISResult<IOASISStorageProvider>(CurrentStorageProvider);
        }

        public async Task<OASISResult<IOASISStorageProvider>> SetAndActivateCurrentStorageProviderAsync(IOASISProvider OASISProvider)
        {
            if (OASISProvider != CurrentStorageProvider)
            {
                if (OASISProvider != null)
                {
                    if (!IsProviderRegistered(OASISProvider))
                        RegisterProvider(OASISProvider);

                    return await SetAndActivateCurrentStorageProviderAsync(OASISProvider.ProviderType.Value);
                }
            }

            return new OASISResult<IOASISStorageProvider>(CurrentStorageProvider);
        }

        // Called from ONODE.WebAPI.OASISProviderManager.
        //TODO: In future more than one StorageProvider will be active at a time so we need to work out how to handle this...
        public OASISResult<IOASISStorageProvider> SetAndActivateCurrentStorageProvider(ProviderType providerType, bool setGlobally = false)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();
            _setProviderGlobally = setGlobally;

            // This is automatically handled in the Managers (AvatarManager, HolonManager, etc) whenever a provider throws an exception, it will try the next provider in the list... :)
            if (providerType == ProviderType.Default && !OverrideProviderType && _providerAutoFailOverList.Count > 0)
                providerType = _providerAutoFailOverList[0].Value;

            if (providerType != CurrentStorageProviderType.Value)
            {
                IOASISProvider provider = _registeredProviders.FirstOrDefault(x => x.ProviderType.Value == providerType);

                if (provider == null)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(Enum.GetName(typeof(ProviderType), providerType), " ProviderType is not registered. Please call RegisterProvider() method to register the provider before calling this method."));
                    return result;
                }

                if (provider != null && (provider.ProviderCategory.Value == ProviderCategory.Storage 
                    || provider.ProviderCategory.Value == ProviderCategory.StorageAndNetwork 
                    || provider.ProviderCategory.Value == ProviderCategory.StorageLocal 
                    || provider.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork))
                {
                    if (CurrentStorageProvider != null)
                    {
                        OASISResult<bool> deactivateProviderResult = DeActivateProvider(CurrentStorageProvider);

                        //TODO: Think its not an error as long as it can activate a provider below?
                        if ((deactivateProviderResult != null && (deactivateProviderResult.IsError || !deactivateProviderResult.Result)) || deactivateProviderResult == null)
                            OASISErrorHandling.HandleWarning(ref result, deactivateProviderResult != null ? $"Error Occured In ProviderManager.SetAndActivateCurrentStorageProvider Calling DeActivateProvider For Provider {CurrentStorageProviderType.Name}. Reason: {deactivateProviderResult.Message}" : "Unknown error (deactivateProviderResult was null!)");
                        else
                            LoggingManager.Log($"{CurrentStorageProviderType.Name} Provider DeActivated Successfully.", Logging.LogType.Info);
                    }

                    CurrentStorageProviderCategory = provider.ProviderCategory;
                    CurrentStorageProviderType.Value = providerType;
                    CurrentStorageProvider = (IOASISStorageProvider)provider;

                    OASISResult<bool> activateProviderResult = ActivateProvider(CurrentStorageProvider);

                    if ((activateProviderResult != null && (activateProviderResult.IsError || !activateProviderResult.Result)) || activateProviderResult == null)
                        OASISErrorHandling.HandleError(ref result, activateProviderResult != null ? $"Error Occured In ProviderManager.SetAndActivateCurrentStorageProvider Calling ActivateProvider For Provider {CurrentStorageProviderType.Name}. Reason: {activateProviderResult.Message}" : "Unknown error (activateProviderResult was null!)");
                    else
                        LoggingManager.Log($"{CurrentStorageProviderType.Name} Provider Activated Successfully.", Logging.LogType.Info);

                    if (setGlobally)
                        DefaultGlobalStorageProvider = CurrentStorageProvider;
                }
            }

            result.Result = CurrentStorageProvider;
            return result;
        }

        public async Task<OASISResult<IOASISStorageProvider>> SetAndActivateCurrentStorageProviderAsync(ProviderType providerType, bool setGlobally = false)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();
            _setProviderGlobally = setGlobally;

            // This is automatically handled in the Managers (AvatarManager, HolonManager, etc) whenever a provider throws an exception, it will try the next provider in the list... :)
            if (providerType == ProviderType.Default && !OverrideProviderType && _providerAutoFailOverList.Count > 0)
                providerType = _providerAutoFailOverList[0].Value;

            if (providerType != CurrentStorageProviderType.Value)
            {
                IOASISProvider provider = _registeredProviders.FirstOrDefault(x => x.ProviderType.Value == providerType);

                if (provider == null)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(Enum.GetName(typeof(ProviderType), providerType), " ProviderType is not registered. Please call RegisterProvider() method to register the provider before calling this method."));
                    return result;
                }

                if (provider != null && (provider.ProviderCategory.Value == ProviderCategory.Storage
                    || provider.ProviderCategory.Value == ProviderCategory.StorageAndNetwork
                    || provider.ProviderCategory.Value == ProviderCategory.StorageLocal
                    || provider.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork))
                {
                    if (CurrentStorageProvider != null)
                    {
                        OASISResult<bool> deactivateProviderResult = await DeActivateProviderAsync(CurrentStorageProvider);

                        //TODO: Think its not an error as long as it can activate a provider below?
                        if ((deactivateProviderResult != null && (deactivateProviderResult.IsError || !deactivateProviderResult.Result)) || deactivateProviderResult == null)
                            OASISErrorHandling.HandleWarning(ref result, deactivateProviderResult != null ? $"Error Occured In ProviderManager.SetAndActivateCurrentStorageProviderAsync Calling DeActivateProviderAsync For Provider {CurrentStorageProviderType.Name}. Reason: {deactivateProviderResult.Message}" : "Unknown error (deactivateProviderResult was null!)");
                        else
                            LoggingManager.Log($"{CurrentStorageProviderType.Name} Provider DeActivated Successfully (Async).", Logging.LogType.Info);
                    }

                    CurrentStorageProviderCategory = provider.ProviderCategory;
                    CurrentStorageProviderType.Value = providerType;
                    CurrentStorageProvider = (IOASISStorageProvider)provider;

                    OASISResult<bool> activateProviderResult = await ActivateProviderAsync(CurrentStorageProvider);

                    if ((activateProviderResult != null && (activateProviderResult.IsError || !activateProviderResult.Result)) || activateProviderResult == null)
                        OASISErrorHandling.HandleError(ref result, activateProviderResult != null ? $"Error Occured In ProviderManager.SetAndActivateCurrentStorageProviderAsync Calling ActivateProviderAsync For Provider {CurrentStorageProviderType.Name}. Reason: {activateProviderResult.Message}" : "Unknown error (activateProviderResult was null!)");
                    else
                        LoggingManager.Log($"{CurrentStorageProviderType.Name} Provider Activated Successfully (Async).", Logging.LogType.Info);

                    if (setGlobally)
                        DefaultGlobalStorageProvider = CurrentStorageProvider;
                }
            }

            result.Result = CurrentStorageProvider;
            return result;
        }

        public async Task<OASISResult<bool>> ActivateProviderAsync(ProviderType type)
        {
            return await ActivateProviderAsync(_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type));
        }

        public async Task<OASISResult<bool>> ActivateProviderAsync(IOASISProvider provider)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = $"Error Occured Activating Provider {provider.ProviderType.Name} In ProviderManager ActivateProviderAsync. Reason: ";

            if (provider != null)
            {
                try
                {
                    LoggingManager.Log($"Attempting To Activate {provider.ProviderType.Name} Provider (Async)...", Logging.LogType.Info, true);

                    var task = provider.ActivateProviderAsync();

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.ActivateProviderTimeOutSeconds * 1000)) == task)
                        result = task.Result;
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Timeout Occured! ActivateProviderTimeOutSeconds In OASISDNA.json Is Set To {OASISDNA.OASIS.StorageProviders.ActivateProviderTimeOutSeconds}. Try Increasing The Value And Try Again..."));
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Unknown Error Occured: {ex}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}Provider passed in is null!");

            return result;
        }

        public OASISResult<bool> ActivateProvider(ProviderType type)
        {
            return ActivateProvider(_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type));
        }

        public OASISResult<bool> ActivateProvider(IOASISProvider provider)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = $"Error Occured Activating Provider {provider.ProviderType.Name} In ProviderManager ActivateProvider. Reason: ";

            if (provider != null)
            {
                try
                {
                    LoggingManager.Log($"Attempting To Activate {provider.ProviderType.Name} Provider...", Logging.LogType.Info, true);
                    result = Task.Run(() => provider.ActivateProvider()).WaitAsync(TimeSpan.FromSeconds(OASISDNA.OASIS.StorageProviders.ActivateProviderTimeOutSeconds)).Result;
                }
                catch (TimeoutException)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Timeout Occured! ActivateProviderTimeOutSeconds In OASISDNA.json Is Set To {OASISDNA.OASIS.StorageProviders.ActivateProviderTimeOutSeconds}. Try Increasing The Value And Try Again..."));
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Unknown Error Occured: {ex}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}Provider passed in is null!");

            return result;
        }

        public OASISResult<bool> DeActivateProvider(ProviderType type)
        {
            return DeActivateProvider(_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type));
        }

        public OASISResult<bool> DeActivateProvider(IOASISProvider provider)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = $"Error Occured Deactivating Provider {provider.ProviderType.Name} In ProviderManager DeActivateProvider. Reason: ";

            if (provider != null)
            {
                try
                {
                    LoggingManager.Log($"Attempting To Deactivate {provider.ProviderType.Name} Provider...", Logging.LogType.Info, true);
                    result = Task.Run(() => provider.DeActivateProvider()).WaitAsync(TimeSpan.FromSeconds(OASISDNA.OASIS.StorageProviders.DectivateProviderTimeOutSeconds)).Result;
                }
                catch (TimeoutException)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Timeout Occured! DectivateProviderTimeOutSeconds In OASISDNA.json Is Set To {OASISDNA.OASIS.StorageProviders.DectivateProviderTimeOutSeconds}. Try Increasing The Value And Try Again..."));
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Unknown Error Occured: {ex}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}Provider passed in is null!");

            return result;
        }

        public async Task<OASISResult<bool>> DeActivateProviderAsync(ProviderType type)
        {
            return await DeActivateProviderAsync(_registeredProviders.FirstOrDefault(x => x.ProviderType.Value == type));
        }

        public async Task<OASISResult<bool>> DeActivateProviderAsync(IOASISProvider provider)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = $"Error Occured Deactivating Provider {provider.ProviderType.Name} In ProviderManager DeActivateProviderAsync. Reason: ";

            if (provider != null)
            {
                try
                {
                    LoggingManager.Log($"Attempting To Deactivate {provider.ProviderType.Name} Provider (Async)...", Logging.LogType.Info, true);

                    var task = provider.DeActivateProviderAsync();

                    if (await Task.WhenAny(task, Task.Delay(OASISDNA.OASIS.StorageProviders.DectivateProviderTimeOutSeconds * 1000)) == task)
                        result = task.Result;
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Timeout Occured! DectivateProviderTimeOutSeconds In OASISDNA.json Is Set To {OASISDNA.OASIS.StorageProviders.DectivateProviderTimeOutSeconds}. Try Increasing The Value And Try Again..."));
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Unknown Error Occured: {ex}");
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}Provider passed in is null!");

            return result;
        }

        public bool SetAutoReplicationForProviders(bool autoReplicate, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(autoReplicate, providers, _providersThatAreAutoReplicating);
        }

        public OASISResult<bool> SetAutoReplicationForProviders(bool autoReplicate, string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoReplicate", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            result.Result = SetAutoReplicationForProviders(autoReplicate, listResult.Result);
            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoReplicationListForProviders(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoReplicate", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providersThatAreAutoReplicating.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providersThatAreAutoReplicating.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoReplicationListForProviders(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providersThatAreAutoReplicating = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public bool SetAutoReplicateForAllProviders(bool autoReplicate)
        {
            return SetAutoReplicationForProviders(autoReplicate, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public bool SetAutoFailOverForProviders(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverList);
        }

        public bool SetAutoFailOverForProvidersForAvatarLogin(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverListForAvatarLogin);
        }

        public bool SetAutoFailOverForProvidersForCheckIfEmailAlreadyInUse(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverListForCheckIfEmailAlreadyInUse);
        }

        public bool SetAutoFailOverForProvidersForCheckIfUsernameAlreadyInUse(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverListForCheckIfUsernameAlreadyInUse);
        }

        public bool SetAutoFailOverForProvidersForCheckIfOASISSystemAccountExists(bool addToFailOverList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToFailOverList, providers, _providerAutoFailOverListForCheckIfOASISSystemAccountExists);
        }

        public OASISResult<bool> SetAutoFailOverForProviders(bool addToFailOverList, string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOver", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            result.Result = SetAutoFailOverForProviders(addToFailOverList, listResult.Result);
            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProviders(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOver", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providerAutoFailOverList.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providerAutoFailOverList.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForAvatarLogin(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOverForAvatarLogin", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providerAutoFailOverListForAvatarLogin.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providerAutoFailOverListForAvatarLogin.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForCheckIfEmailAlreadyInUse(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOverForCheckIfEmailAlreadyInUse", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providerAutoFailOverListForCheckIfEmailAlreadyInUse.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providerAutoFailOverListForCheckIfEmailAlreadyInUse.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForCheckIfUsernameAlreadyInUse(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoFailOverForCheckIfUsernameAlreadyInUse", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            _providerAutoFailOverListForCheckIfUsernameAlreadyInUse.Clear();
            foreach (ProviderType providerType in listResult.Result)
                _providerAutoFailOverListForCheckIfUsernameAlreadyInUse.Add(new EnumValue<ProviderType>(providerType));

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProviders(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoFailOverList = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForAvatarLogin(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoFailOverListForAvatarLogin = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForCheckIfEmailAlreadyInUse(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoFailOverListForCheckIfEmailAlreadyInUse = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public OASISResult<bool> SetAndReplaceAutoFailOverListForProvidersForCheckIfUsernameAlreadyInUse(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoFailOverListForCheckIfUsernameAlreadyInUse = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public OASISResult<T> ValidateProviderList<T>(string listName, string providerList)
        {
            string[] providers = providerList.Split(',');
            object providerTypeObject = null;

            foreach (string provider in providers)
            {
                if (!Enum.TryParse(typeof(ProviderType), provider.Trim(), out providerTypeObject))
                    return new OASISResult<T>() { Message = $"The ProviderType {provider.Trim()} passed in for the {listName} list is invalid. It must be one of the following types: {EnumHelper.GetEnumValues(typeof(ProviderType), EnumHelperListType.ItemsSeperatedByComma)}.", IsError = true };
            }

            return new OASISResult<T>();
        }

        public OASISResult<IEnumerable<ProviderType>> GetProvidersFromList(string listName, string providerList)
        {
            OASISResult<IEnumerable<ProviderType>> result = new OASISResult<IEnumerable<ProviderType>>();
            List<ProviderType> providerTypes = new List<ProviderType>();
            string[] providers = providerList.Split(",");
            object providerTypeObject = null;
            List<string> invalidProviderTypes = new List<string>();

            foreach (string provider in providers)
            {
                if (Enum.TryParse(typeof(ProviderType), provider.Trim(), out providerTypeObject))
                    providerTypes.Add((ProviderType)providerTypeObject);
                else
                {
                    invalidProviderTypes.Add(provider.Trim());
                    //OASISErrorHandling.HandleWarning(ref result, $"{provider.Trim()} listName} list is invalid.");
                    OASISErrorHandling.HandleWarning(ref result, $"Error in GetProvidersFromList method in ProviderManager, the provider {provider.Trim()} specified in the {listName} list is invalid.");
                }
            }

            if (result.WarningCount > 0)
                result.Message = $"Error in GetProvidersFromList method in ProviderManager. {result.WarningCount} provider type(s) passed in for the {listName} list are invalid:\n\n{OASISResultHelper.BuildInnerMessageError(invalidProviderTypes, ", ", true)}.\n\nThey must be one of the following values: {EnumHelper.GetEnumValues(typeof(ProviderType))}";
            //result.Message = $"Error in GetProvidersFromList method in ProviderManager. {result.WarningCount} provider type(s) passed in for the {listName} are invalid:\n\n{OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}.\n\nThey must be one of the following values: {EnumHelper.GetEnumValues(typeof(ProviderType))}";

            result.Result = providerTypes;
            return result;
        }

        public OASISResult<IEnumerable<EnumValue<ProviderType>>> GetProvidersFromListAsEnumList(string listName, string providerList)
        {
            OASISResult<IEnumerable<EnumValue<ProviderType>>> result = new OASISResult<IEnumerable<EnumValue<ProviderType>>>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList(listName, providerList);

            if (!listResult.IsError && listResult.Result != null)
                result.Result = EnumHelper.ConvertToEnumValueList(listResult.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetProvidersFromListAsEnumList method in ProviderManager. Reason: {listResult.Message}", listResult.DetailedMessage);

            return result;
        }

        public bool SetAutoFailOverForAllProviders(bool addToFailOverList)
        {
            return SetAutoFailOverForProviders(addToFailOverList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public bool SetAutoFailOverForAllProvidersForAvatarLogin(bool addToFailOverList)
        {
            return SetAutoFailOverForProvidersForAvatarLogin(addToFailOverList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public bool SetAutoFailOverForAllProvidersForCheckIfEmailAlreadyInUse(bool addToFailOverList)
        {
            return SetAutoFailOverForProvidersForCheckIfEmailAlreadyInUse(addToFailOverList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public bool SetAutoFailOverForProvidersForCheckIfUsernameAlreadyInUse(bool addToFailOverList)
        {
            return SetAutoFailOverForProvidersForCheckIfUsernameAlreadyInUse(addToFailOverList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public bool SetAutoLoadBalanceForProviders(bool addToLoadBalanceList, IEnumerable<ProviderType> providers)
        {
            return SetProviderList(addToLoadBalanceList, providers, _providerAutoLoadBalanceList);
        }

        public OASISResult<bool> SetAutoLoadBalanceForProviders(bool addToLoadBalanceList, string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoLoadBalance", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            result.Result = SetAutoLoadBalanceForProviders(addToLoadBalanceList, listResult.Result);
            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoLoadBalanceListForProviders(string providerList)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IEnumerable<ProviderType>> listResult = GetProvidersFromList("AutoLoadBalance", providerList);

            result.InnerMessages.AddRange(listResult.InnerMessages);
            result.IsWarning = listResult.IsWarning;
            result.WarningCount += listResult.WarningCount;

            if (!listResult.IsError && listResult.Result != null)
            {
                _providerAutoLoadBalanceList.Clear();
                foreach (ProviderType providerType in listResult.Result)
                    _providerAutoLoadBalanceList.Add(new EnumValue<ProviderType>(providerType));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in SetAndReplaceAutoLoadBalanceListForProviders method in ProviderManager. Reason: {listResult.Result}");

            return result;
        }

        public OASISResult<bool> SetAndReplaceAutoLoadBalanceListForProviders(IEnumerable<EnumValue<ProviderType>> providerList)
        {
            _providerAutoLoadBalanceList = providerList.ToList();
            return new OASISResult<bool>(true);
        }

        public bool SetAutoLoadBalanceForAllProviders(bool addToLoadBalanceList)
        {
            return SetAutoLoadBalanceForProviders(addToLoadBalanceList, _registeredProviderTypes.Select(x => x.Value).ToList());
        }

        public List<EnumValue<ProviderType>> GetProviderAutoLoadBalanceList()
        {
            return _providerAutoLoadBalanceList;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverList()
        {
            return _providerAutoFailOverList;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverListForAvatarLogin()
        {
            return _providerAutoFailOverListForAvatarLogin;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverListForCheckIfEmailAlreadyInUse()
        {
            return _providerAutoFailOverListForCheckIfEmailAlreadyInUse;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverListForCheckIfUsernameAlreadyInUse()
        {
            return _providerAutoFailOverListForCheckIfUsernameAlreadyInUse;
        }

        public List<EnumValue<ProviderType>> GetProviderAutoFailOverListForCheckIfOASISSystemAccountExists()
        {
            return _providerAutoFailOverListForCheckIfOASISSystemAccountExists;
        }
        public string GetProviderAutoFailOverListAsString()
        {
            return GetProviderListAsString(GetProviderAutoFailOverList());
        }

        public string GetProviderAutoFailOverListForAvatarLoginAsString()
        {
            return GetProviderListAsString(GetProviderAutoFailOverListForAvatarLogin());
        }

        public string GetProviderAutoFailOverListForCheckIfEmailAlreadyInUseAsString()
        {
            return GetProviderListAsString(GetProviderAutoFailOverListForCheckIfEmailAlreadyInUse());
        }

        public string GetProviderAutoFailOverListForCheckIfUsernameAlreadyInUseAsString()
        {
            return GetProviderListAsString(GetProviderAutoFailOverListForCheckIfUsernameAlreadyInUse());
        }

        public string GetProvidersThatAreAutoReplicatingAsString()
        {
            return GetProviderListAsString(GetProvidersThatAreAutoReplicating());
        }

        public string GetProviderAutoLoadBalanceListAsString()
        {
            return GetProviderListAsString(GetProviderAutoLoadBalanceList());
        }

        public string GetProviderListAsString(List<ProviderType> providerList)
        {
            return GetProviderListAsString(EnumHelper.ConvertToEnumValueList(providerList).ToList());
        }

        public string GetProviderListAsString(List<EnumValue<ProviderType>> providerList)
        {
            string list = "";

            for (int i = 0; i < providerList.Count(); i++)
            {
                list = string.Concat(list, providerList[i].Name);

                if (i < providerList.Count - 2)
                    list = string.Concat(list, ", ");

                else if (i == providerList.Count() - 2)
                    list = string.Concat(list, " & ");
            }

            return list;
        }

        //public List<EnumValue<ProviderType>> GetProvidersThatAreAutoReplicating()
        //{
        //    return _providersThatAreAutoReplicating;
        //}

        public List<EnumValue<ProviderType>> GetProvidersThatAreAutoReplicating()
        {
            //TODO: Handle OASISResult properly and make all methods return OASISResult ASAP!
            //string providerListCache = GetProviderListAsString(_providersThatAreAutoReplicating);
            //return GetProvidersFromListAsEnumList("AutoReplicate", providerListCache).Result.ToList();

            return _providersThatAreAutoReplicating;
        }

        private bool SetProviderList(bool add, IEnumerable<ProviderType> providers, List<EnumValue<ProviderType>> listToAddTo)
        {
            foreach (ProviderType providerType in providers)
            {
                if (add && !listToAddTo.Any(x => x.Value == providerType))
                    listToAddTo.Add(new EnumValue<ProviderType>(providerType));

                else if (!add)
                {
                    foreach (EnumValue<ProviderType> type in listToAddTo)
                    {
                        if (type.Value == providerType)
                        {
                            listToAddTo.Remove(type);
                            break;
                        }
                    }
                }
            }

            return true;
        }
    }
}
