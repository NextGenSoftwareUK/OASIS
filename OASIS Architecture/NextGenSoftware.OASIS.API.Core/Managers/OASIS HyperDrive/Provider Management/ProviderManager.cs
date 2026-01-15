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
using NextGenSoftware.OASIS.API.Core.Configuration;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    /// <summary>
    /// ⚠️ OBSOLETE WARNING: This ProviderManager class is being refactored for better separation of concerns.
    /// 
    /// NEW ARCHITECTURE:
    /// - ProviderRegistry: Manages provider registration and basic operations
    /// - ProviderSelector: Handles all provider selection algorithms
    /// - ProviderSwitcher: Manages provider switching logic
    /// - ProviderConfigurator: Handles provider configuration and lists
    /// - ProviderManagerNew: Acts as a facade/orchestrator
    /// 
    /// MIGRATION PATH:
    /// 1. Use the new facade methods (ending with "New") for immediate access to new system
    /// 2. Gradually migrate to direct usage of specialized classes
    /// 3. This class will be deprecated once all dependencies are migrated
    /// 
    /// BACKWARD COMPATIBILITY: All existing methods remain functional during transition period.
    /// </summary>
    //[Obsolete("ProviderManager is being refactored. Use ProviderManagerNew or specialized classes (ProviderRegistry, ProviderSelector, ProviderSwitcher, ProviderConfigurator) instead. See class documentation for migration path.")]
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
        public bool SupressConsoleLoggingWhenSwitchingProviders { get; set; } = false;


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

            //TODO: Make work like GetAllBlockchainProviders once ProviderCategories has been set in the providers.
            foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategory.Value == ProviderCategory.Network || x.ProviderCategory.Value == ProviderCategory.StorageAndNetwork || x.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork).ToList())
                networkProviders.Add((IOASISNETProvider)provider);

            return networkProviders;
        }

        public List<IOASISBlockchainStorageProvider> GetAllBlockchainProviders()
        {
            List<IOASISBlockchainStorageProvider> blockchainProviders = new List<IOASISBlockchainStorageProvider>();

            foreach (IOASISProvider provider in _registeredProviders)
            {
                foreach (ProviderCategory category in provider.ProviderCategories.Select(x => x.Value).ToList())
                {
                    if (category == ProviderCategory.Blockchain)
                        blockchainProviders.Add((IOASISBlockchainStorageProvider)provider);
                }
            }

            //foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategories.Contains(new EnumValue<ProviderCategory>(ProviderCategory.Blockchain))))
            //    blockchainProviders.Add((IOASISBlockchainStorageProvider)provider);

            return blockchainProviders;
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

                        else if (!SupressConsoleLoggingWhenSwitchingProviders)
                            LoggingManager.Log($"{CurrentStorageProviderType.Name} Provider DeActivated Successfully.", Logging.LogType.Info);
                    }

                    CurrentStorageProviderCategory = provider.ProviderCategory;
                    CurrentStorageProviderType.Value = providerType;
                    CurrentStorageProvider = (IOASISStorageProvider)provider;

                    OASISResult<bool> activateProviderResult = ActivateProvider(CurrentStorageProvider);

                    if ((activateProviderResult != null && (activateProviderResult.IsError || !activateProviderResult.Result)) || activateProviderResult == null)
                        OASISErrorHandling.HandleError(ref result, activateProviderResult != null ? $"Error Occured In ProviderManager.SetAndActivateCurrentStorageProvider Calling ActivateProvider For Provider {CurrentStorageProviderType.Name}. Reason: {activateProviderResult.Message}" : "Unknown error (activateProviderResult was null!)");
                    
                    else if (!SupressConsoleLoggingWhenSwitchingProviders)
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

                        else if (!SupressConsoleLoggingWhenSwitchingProviders)
                            LoggingManager.Log($"{CurrentStorageProviderType.Name} Provider DeActivated Successfully (Async).", Logging.LogType.Info);
                    }

                    CurrentStorageProviderCategory = provider.ProviderCategory;
                    CurrentStorageProviderType.Value = providerType;
                    CurrentStorageProvider = (IOASISStorageProvider)provider;

                    OASISResult<bool> activateProviderResult = await ActivateProviderAsync(CurrentStorageProvider);

                    if ((activateProviderResult != null && (activateProviderResult.IsError || !activateProviderResult.Result)) || activateProviderResult == null)
                        OASISErrorHandling.HandleError(ref result, activateProviderResult != null ? $"Error Occured In ProviderManager.SetAndActivateCurrentStorageProviderAsync Calling ActivateProviderAsync For Provider {CurrentStorageProviderType.Name}. Reason: {activateProviderResult.Message}" : "Unknown error (activateProviderResult was null!)");

                    else if (!SupressConsoleLoggingWhenSwitchingProviders)
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
                    if (!SupressConsoleLoggingWhenSwitchingProviders)
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
                    if (!SupressConsoleLoggingWhenSwitchingProviders)
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
                    if (!SupressConsoleLoggingWhenSwitchingProviders)
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
                    if (!SupressConsoleLoggingWhenSwitchingProviders)
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

        #region Auto-Load Balancing Methods

        /// <summary>
        /// Gets the list of providers configured for auto-load balancing
        /// </summary>
        // Duplicate removed: method already defined earlier

        /// <summary>
        /// Gets the auto-load balance list as a string for logging/debugging
        /// </summary>
        // Duplicate removed: method already defined earlier

        /// <summary>
        /// Adds providers to the auto-load balance list
        /// </summary>
        public bool AddProvidersToAutoLoadBalanceList(IEnumerable<ProviderType> providers)
        {
            return SetProviderList(true, providers, _providerAutoLoadBalanceList);
        }

        /// <summary>
        /// Removes providers from the auto-load balance list
        /// </summary>
        public bool RemoveProvidersFromAutoLoadBalanceList(IEnumerable<ProviderType> providers)
        {
            return SetProviderList(false, providers, _providerAutoLoadBalanceList);
        }

        /// <summary>
        /// Clears the auto-load balance list
        /// </summary>
        public void ClearAutoLoadBalanceList()
        {
            _providerAutoLoadBalanceList.Clear();
        }

        /// <summary>
        /// Selects the optimal provider for load balancing based on current conditions
        /// </summary>
        public EnumValue<ProviderType> SelectOptimalProviderForLoadBalancing(LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
        {
            if (!IsAutoLoadBalanceEnabled || _providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            // Use configuration-based strategy if Auto is selected
            if (strategy == LoadBalancingStrategy.Auto)
            {
                var config = OASISHyperDriveConfigManager.Instance.GetConfiguration();
                var configured = config?.DefaultStrategy;
                if (!string.IsNullOrWhiteSpace(configured) && Enum.TryParse(configured, true, out LoadBalancingStrategy parsed))
                    strategy = parsed;
                else
                    strategy = LoadBalancingStrategy.Auto;
            }

            return strategy switch
            {
                LoadBalancingStrategy.RoundRobin => SelectRoundRobinProvider(),
                LoadBalancingStrategy.WeightedRoundRobin => SelectWeightedRoundRobinProvider(),
                LoadBalancingStrategy.LeastConnections => SelectLeastConnectionsProvider(),
                LoadBalancingStrategy.Geographic => SelectGeographicProvider(),
                LoadBalancingStrategy.CostBased => SelectCostBasedProvider(),
                LoadBalancingStrategy.Performance => SelectPerformanceBasedProvider(),
                _ => SelectAutoProvider()
            };
        }

        /// <summary>
        /// Performance-based provider selection with lag/ping as primary criteria
        /// </summary>
        private EnumValue<ProviderType> SelectPerformanceBasedProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var config = OASISHyperDriveConfigManager.Instance.GetConfiguration();
            var providerScores = new Dictionary<EnumValue<ProviderType>, double>();

            // Calculate performance scores with lag/ping as primary criteria
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var metrics = performanceMonitor.GetMetrics(provider.Value);
                var geoInfo = performanceMonitor.GetGeographicInfo(provider.Value);
                
                if (metrics == null)
                {
                    providerScores[provider] = 0;
                    continue;
                }

                // Primary criteria: Latency/Ping (50% weight from config)
                var latencyScore = CalculateLatencyScore(metrics, geoInfo);
                
                // Secondary criteria: Performance (30% weight from config)
                var performanceScore = CalculatePerformanceScore(metrics);
                
                // Tertiary criteria: Reliability (20% weight from config)
                var reliabilityScore = CalculateReliabilityScore(metrics);

                // Weighted combination with lag/ping as primary
                var score = (latencyScore * config.LatencyWeight) + 
                           (performanceScore * config.ThroughputWeight) + 
                           (reliabilityScore * config.ReliabilityWeight);
                
                providerScores[provider] = score;
            }

            // Select provider with highest score
            var bestScore = providerScores.Values.Max();
            var bestProviders = providerScores
                .Where(x => x.Value == bestScore)
                .Select(x => x.Key)
                .ToList();

            // If multiple providers have same score, use latency as tie-breaker
            if (bestProviders.Count > 1)
            {
                var lowestLatency = double.MaxValue;
                EnumValue<ProviderType> bestProvider = bestProviders.First();

                foreach (var provider in bestProviders)
                {
                    var metrics = performanceMonitor.GetMetrics(provider.Value);
                    var latency = metrics?.ResponseTimeMs ?? 1000; // Default high latency
                    
                    if (latency < lowestLatency)
                    {
                        lowestLatency = latency;
                        bestProvider = provider;
                    }
                }

                return bestProvider;
            }

            return bestProviders.First();
        }

        /// <summary>
        /// Calculates reliability score
        /// </summary>
        private double CalculateReliabilityScore(ProviderPerformanceMetrics metrics)
        {
            var uptimeScore = metrics.UptimePercentage;
            var errorRateScore = Math.Max(0, 100 - (metrics.ErrorRate * 100));
            
            return (uptimeScore * 0.6) + (errorRateScore * 0.4);
        }

        /// <summary>
        /// Round Robin load balancing - distributes requests evenly
        /// </summary>
        private EnumValue<ProviderType> SelectRoundRobinProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            // Simple round robin implementation
            var index = DateTime.Now.Millisecond % _providerAutoLoadBalanceList.Count;
            return _providerAutoLoadBalanceList[index];
        }

        /// <summary>
        /// Weighted Round Robin - weights providers based on performance
        /// </summary>
        private EnumValue<ProviderType> SelectWeightedRoundRobinProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var providerWeights = new Dictionary<EnumValue<ProviderType>, double>();

            // Calculate weights based on performance metrics
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var metrics = performanceMonitor.GetMetrics(provider.Value);
                var weight = metrics?.OverallScore ?? 50; // Default weight if no metrics
                providerWeights[provider] = Math.Max(1, weight); // Ensure minimum weight of 1
            }

            // Select provider based on weighted random selection
            var totalWeight = providerWeights.Values.Sum();
            var random = new Random().NextDouble() * totalWeight;
            var currentWeight = 0.0;

            foreach (var provider in _providerAutoLoadBalanceList)
            {
                currentWeight += providerWeights[provider];
                if (random <= currentWeight)
                    return provider;
            }

            return _providerAutoLoadBalanceList.First();
        }

        /// <summary>
        /// Least Connections - routes to provider with fewest active connections
        /// </summary>
        private EnumValue<ProviderType> SelectLeastConnectionsProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var providerConnections = new Dictionary<EnumValue<ProviderType>, int>();

            // Get connection counts for all providers
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var connections = performanceMonitor.GetActiveConnections(provider.Value);
                providerConnections[provider] = connections;
            }

            // Select provider with least connections
            var leastConnections = providerConnections.Values.Min();
            var leastConnectedProviders = providerConnections
                .Where(x => x.Value == leastConnections)
                .Select(x => x.Key)
                .ToList();

            // If multiple providers have same connection count, use round robin among them
            if (leastConnectedProviders.Count > 1)
            {
                var index = DateTime.Now.Millisecond % leastConnectedProviders.Count;
                return leastConnectedProviders[index];
            }

            return leastConnectedProviders.First();
        }

        /// <summary>
        /// Geographic routing - routes to nearest provider
        /// </summary>
        private EnumValue<ProviderType> SelectGeographicProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var providerScores = new Dictionary<EnumValue<ProviderType>, double>();

            // Calculate geographic scores for all providers
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var geoInfo = performanceMonitor.GetGeographicInfo(provider.Value);
                var score = geoInfo?.NetworkLatency ?? 50; // Default score if no geo info
                providerScores[provider] = score;
            }

            // Select provider with best geographic score (lowest latency)
            var bestScore = providerScores.Values.Min();
            var bestProviders = providerScores
                .Where(x => x.Value == bestScore)
                .Select(x => x.Key)
                .ToList();

            // If multiple providers have same score, use round robin among them
            if (bestProviders.Count > 1)
            {
                var index = DateTime.Now.Millisecond % bestProviders.Count;
                return bestProviders[index];
            }

            return bestProviders.First();
        }

        /// <summary>
        /// Cost-based routing - routes to most cost-effective provider
        /// </summary>
        private EnumValue<ProviderType> SelectCostBasedProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var providerCosts = new Dictionary<EnumValue<ProviderType>, double>();

            // Calculate cost scores for all providers
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var costAnalysis = performanceMonitor.GetCostAnalysis(provider.Value);
                var metrics = performanceMonitor.GetMetrics(provider.Value);
                
                // Use cost analysis if available, otherwise use metrics cost
                var cost = costAnalysis?.TotalCost ?? metrics?.CostPerOperation ?? 0.01; // Default cost
                providerCosts[provider] = cost;
            }

            // Select provider with lowest cost
            var lowestCost = providerCosts.Values.Min();
            var lowestCostProviders = providerCosts
                .Where(x => x.Value == lowestCost)
                .Select(x => x.Key)
                .ToList();

            // If multiple providers have same cost, use performance metrics to break tie
            if (lowestCostProviders.Count > 1)
            {
                var bestPerformance = double.MaxValue;
                EnumValue<ProviderType> bestProvider = lowestCostProviders.First();

                foreach (var provider in lowestCostProviders)
                {
                    var metrics = performanceMonitor.GetMetrics(provider.Value);
                    var performanceScore = metrics?.ResponseTimeMs ?? 100; // Default response time
                    
                    if (performanceScore < bestPerformance)
                    {
                        bestPerformance = performanceScore;
                        bestProvider = provider;
                    }
                }

                return bestProvider;
            }

            return lowestCostProviders.First();
        }

        /// <summary>
        /// Auto selection - intelligently selects based on multiple factors
        /// </summary>
        private EnumValue<ProviderType> SelectAutoProvider()
        {
            if (_providerAutoLoadBalanceList.Count == 0)
                return CurrentStorageProviderType;

            var performanceMonitor = PerformanceMonitor.Instance;
            var providerScores = new Dictionary<EnumValue<ProviderType>, double>();

            // Calculate comprehensive scores for all providers
            foreach (var provider in _providerAutoLoadBalanceList)
            {
                var metrics = performanceMonitor.GetMetrics(provider.Value);
                var geoInfo = performanceMonitor.GetGeographicInfo(provider.Value);
                var costAnalysis = performanceMonitor.GetCostAnalysis(provider.Value);

                // Calculate weighted score based on multiple factors
                var score = CalculateIntelligentScore(metrics, geoInfo, costAnalysis, provider.Value);
                providerScores[provider] = score;
            }

            // Select provider with highest score
            var bestScore = providerScores.Values.Max();
            var bestProviders = providerScores
                .Where(x => x.Value == bestScore)
                .Select(x => x.Key)
                .ToList();

            // If multiple providers have same score, use latency as tie-breaker
            if (bestProviders.Count > 1)
            {
                var lowestLatency = double.MaxValue;
                EnumValue<ProviderType> bestProvider = bestProviders.First();

                foreach (var provider in bestProviders)
                {
                    var metrics = performanceMonitor.GetMetrics(provider.Value);
                    var latency = metrics?.ResponseTimeMs ?? 1000; // Default high latency
                    
                    if (latency < lowestLatency)
                    {
                        lowestLatency = latency;
                        bestProvider = provider;
                    }
                }

                return bestProvider;
            }

            return bestProviders.First();
        }

        /// <summary>
        /// Calculates intelligent score based on multiple factors with lag/ping as primary criteria
        /// </summary>
        private double CalculateIntelligentScore(ProviderPerformanceMetrics metrics, GeographicInfo geoInfo, CostAnalysis costAnalysis, ProviderType providerType)
        {
            if (metrics == null)
                return 0;

            // Primary criteria: Latency/Ping (40% weight)
            var latencyScore = CalculateLatencyScore(metrics, geoInfo);
            
            // Secondary criteria: Performance (25% weight)
            var performanceScore = CalculatePerformanceScore(metrics);
            
            // Tertiary criteria: Cost (15% weight)
            var costScore = CalculateCostScore(metrics, costAnalysis);
            
            // Quaternary criteria: Geographic (10% weight)
            var geographicScore = CalculateGeographicScore(geoInfo);
            
            // Quinary criteria: Availability (10% weight)
            var availabilityScore = CalculateAvailabilityScore(metrics);

            // Weighted combination with lag/ping as primary
            return (latencyScore * 0.40) + 
                   (performanceScore * 0.25) + 
                   (costScore * 0.15) + 
                   (geographicScore * 0.10) + 
                   (availabilityScore * 0.10);
        }

        /// <summary>
        /// Calculates latency score (primary criteria)
        /// </summary>
        private double CalculateLatencyScore(ProviderPerformanceMetrics metrics, GeographicInfo geoInfo)
        {
            var latency = metrics.ResponseTimeMs;
            var networkLatency = geoInfo?.NetworkLatency ?? latency;
            var totalLatency = Math.Min(latency, networkLatency);

            // Lower latency = higher score (inverted)
            if (totalLatency <= 50) return 100;      // Excellent
            if (totalLatency <= 100) return 90;      // Very Good
            if (totalLatency <= 200) return 80;       // Good
            if (totalLatency <= 500) return 60;       // Fair
            if (totalLatency <= 1000) return 40;      // Poor
            return 20; // Very Poor
        }

        /// <summary>
        /// Calculates performance score
        /// </summary>
        private double CalculatePerformanceScore(ProviderPerformanceMetrics metrics)
        {
            var throughputScore = Math.Min(100, metrics.ThroughputMbps * 10);
            var errorRateScore = Math.Max(0, 100 - (metrics.ErrorRate * 100));
            var uptimeScore = metrics.UptimePercentage;

            return (throughputScore * 0.4) + (errorRateScore * 0.3) + (uptimeScore * 0.3);
        }

        /// <summary>
        /// Calculates cost score
        /// </summary>
        private double CalculateCostScore(ProviderPerformanceMetrics metrics, CostAnalysis costAnalysis)
        {
            var cost = costAnalysis?.TotalCost ?? metrics.CostPerOperation;
            
            // Lower cost = higher score (inverted)
            if (cost <= 0.01) return 100;      // Excellent
            if (cost <= 0.05) return 90;       // Very Good
            if (cost <= 0.10) return 80;       // Good
            if (cost <= 0.25) return 60;        // Fair
            if (cost <= 0.50) return 40;        // Poor
            return 20; // Very Poor
        }

        /// <summary>
        /// Calculates geographic score
        /// </summary>
        private double CalculateGeographicScore(GeographicInfo geoInfo)
        {
            if (geoInfo == null) return 50; // Neutral score

            var latencyScore = Math.Max(0, 100 - (geoInfo.NetworkLatency * 10));
            var hopsScore = Math.Max(0, 100 - (geoInfo.NetworkHops * 5));

            return (latencyScore * 0.7) + (hopsScore * 0.3);
        }

        /// <summary>
        /// Calculates availability score
        /// </summary>
        private double CalculateAvailabilityScore(ProviderPerformanceMetrics metrics)
        {
            return metrics.UptimePercentage;
        }

        #endregion
    }

    // LoadBalancingStrategy moved to NextGenSoftware.OASIS.API.Core.Enums

    #region NEW REFACTORED SYSTEM - Facade Methods for Backward Compatibility
/*
    // New refactored system instances
    private static ProviderManagerNew _newProviderManager;
    private static ProviderManagerNew NewProviderManager
    {
        get
        {
            if (_newProviderManager == null)
                _newProviderManager = ProviderManagerNew.Instance;
            return _newProviderManager;
        }
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderRegistry
    /// Gets all available providers using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderRegistry. Use ProviderRegistry.Instance.GetAvailableProviders() instead.")]
    public List<EnumValue<ProviderType>> GetAvailableProvidersNew()
    {
        return NewProviderManager.GetAvailableProviders();
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderSelector
    /// Selects optimal provider for load balancing using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderSelector. Use ProviderSelector.Instance.SelectOptimalProviderForLoadBalancing() instead.")]
    public EnumValue<ProviderType> SelectOptimalProviderForLoadBalancingNew(LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
    {
        return NewProviderManager.SelectOptimalProviderForLoadBalancing(strategy);
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderSwitcher
    /// Switches storage provider using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderSwitcher. Use ProviderSwitcher.Instance.SwitchStorageProviderAsync() instead.")]
    public async Task<OASISResult<bool>> SwitchStorageProviderAsyncNew(ProviderType newProviderType)
    {
        return await NewProviderManager.SwitchStorageProviderAsync(newProviderType);
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderConfigurator
    /// Adds provider to auto-failover list using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderConfigurator. Use ProviderConfigurator.Instance.AddToAutoFailOverList() instead.")]
    public OASISResult<bool> AddToAutoFailOverListNew(ProviderType providerType)
    {
        return NewProviderManager.AddToAutoFailOverList(providerType);
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderConfigurator
    /// Removes provider from auto-failover list using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderConfigurator. Use ProviderConfigurator.Instance.RemoveFromAutoFailOverList() instead.")]
    public OASISResult<bool> RemoveFromAutoFailOverListNew(ProviderType providerType)
    {
        return NewProviderManager.RemoveFromAutoFailOverList(providerType);
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderConfigurator
    /// Adds provider to auto-replication list using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderConfigurator. Use ProviderConfigurator.Instance.AddToAutoReplicationList() instead.")]
    public OASISResult<bool> AddToAutoReplicationListNew(ProviderType providerType)
    {
        return NewProviderManager.AddToAutoReplicationList(providerType);
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderConfigurator
    /// Removes provider from auto-replication list using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderConfigurator. Use ProviderConfigurator.Instance.RemoveFromAutoReplicationList() instead.")]
    public OASISResult<bool> RemoveFromAutoReplicationListNew(ProviderType providerType)
    {
        return NewProviderManager.RemoveFromAutoReplicationList(providerType);
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderConfigurator
    /// Adds provider to auto-load-balance list using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderConfigurator. Use ProviderConfigurator.Instance.AddToAutoLoadBalanceList() instead.")]
    public OASISResult<bool> AddToAutoLoadBalanceListNew(ProviderType providerType)
    {
        return NewProviderManager.AddToAutoLoadBalanceList(providerType);
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderConfigurator
    /// Removes provider from auto-load-balance list using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderConfigurator. Use ProviderConfigurator.Instance.RemoveFromAutoLoadBalanceList() instead.")]
    public OASISResult<bool> RemoveFromAutoLoadBalanceListNew(ProviderType providerType)
    {
        return NewProviderManager.RemoveFromAutoLoadBalanceList(providerType);
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderConfigurator
    /// Gets provider configuration using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderConfigurator. Use ProviderConfigurator.Instance.GetProviderConfiguration() instead.")]
    public OASISResult<ProviderConfiguration> GetProviderConfigurationNew()
    {
        return NewProviderManager.GetProviderConfiguration();
    }

    /// <summary>
    /// FACADE METHOD: Delegates to new ProviderSwitcher
    /// Gets switching status using the new system
    /// </summary>
    [Obsolete("This method will be moved to ProviderSwitcher. Use ProviderSwitcher.Instance.GetSwitchStatus() instead.")]
    public OASISResult<ProviderSwitchStatus> GetSwitchStatusNew()
    {
        return NewProviderManager.GetSwitchStatus();
    }
    */

    #endregion
}
