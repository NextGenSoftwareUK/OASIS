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
    public partial class ProviderManager
    {
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

        public List<IOASISBlockchainStorageProvider> GetAllEVMBlockchainProviders()
        {
            List<IOASISBlockchainStorageProvider> blockchainProviders = new List<IOASISBlockchainStorageProvider>();

            foreach (IOASISProvider provider in _registeredProviders)
            {
                foreach (ProviderCategory category in provider.ProviderCategories.Select(x => x.Value).ToList())
                {
                    if (category == ProviderCategory.EVMBlockchain)
                        blockchainProviders.Add((IOASISBlockchainStorageProvider)provider);
                }
            }

            return blockchainProviders;
        }

        public bool IsProviderEVMBlockchain(ProviderType providerType)
        {
            foreach (IOASISBlockchainStorageProvider provider in GetAllEVMBlockchainProviders())
            {
                if (provider.ProviderType.Value == providerType)
                    return true;
            }

            return false;
        }

        public bool IsProviderBlockchain(ProviderType providerType)
        {
            foreach (IOASISBlockchainStorageProvider provider in GetAllBlockchainProviders())
            {
                if (provider.ProviderType.Value == providerType)
                    return true;
            }

            return false;
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

                        else if (OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
                            LoggingManager.Log($"{CurrentStorageProviderType.Name} Provider DeActivated Successfully.", Logging.LogType.Info);
                    }

                    CurrentStorageProviderCategory = provider.ProviderCategory;
                    CurrentStorageProviderType.Value = providerType;
                    CurrentStorageProvider = (IOASISStorageProvider)provider;

                    OASISResult<bool> activateProviderResult = ActivateProvider(CurrentStorageProvider);

                    if ((activateProviderResult != null && (activateProviderResult.IsError || !activateProviderResult.Result)) || activateProviderResult == null)
                        OASISErrorHandling.HandleError(ref result, activateProviderResult != null ? $"Error Occured In ProviderManager.SetAndActivateCurrentStorageProvider Calling ActivateProvider For Provider {CurrentStorageProviderType.Name}. Reason: {activateProviderResult.Message}" : "Unknown error (activateProviderResult was null!)");
                    
                    else if (OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
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

                        else if (OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
                            LoggingManager.Log($"{CurrentStorageProviderType.Name} Provider DeActivated Successfully (Async).", Logging.LogType.Info);
                    }

                    CurrentStorageProviderCategory = provider.ProviderCategory;
                    CurrentStorageProviderType.Value = providerType;
                    CurrentStorageProvider = (IOASISStorageProvider)provider;

                    OASISResult<bool> activateProviderResult = await ActivateProviderAsync(CurrentStorageProvider);

                    if ((activateProviderResult != null && (activateProviderResult.IsError || !activateProviderResult.Result)) || activateProviderResult == null)
                        OASISErrorHandling.HandleError(ref result, activateProviderResult != null ? $"Error Occured In ProviderManager.SetAndActivateCurrentStorageProviderAsync Calling ActivateProviderAsync For Provider {CurrentStorageProviderType.Name}. Reason: {activateProviderResult.Message}" : "Unknown error (activateProviderResult was null!)");

                    else if (OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
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
                    if (OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
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
                    if (OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
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
                    if (OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
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
                    if (OASISDNA.OASIS.StorageProviders.LogSwitchingProviders)
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

    }
}
