using System;
using System.Collections.Generic;
using System.Linq;

using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class ProviderManager
    {
        private static List<IOASISProvider> _registeredProviders = new List<IOASISProvider>();
        private static List<ProviderType> _registeredProviderTypes = null;
        private static bool _setProviderGlobally = false;

        public static ProviderType CurrentStorageProviderType { get; private set; } = ProviderType.Default;

        public static string CurrentStorageProviderName
        {
            get
            {
                return Enum.GetName(CurrentStorageProviderType);
            }
        }

        public static string[] DefaultProviderTypes { get; set; }

        public static IOASISStorage DefaultGlobalStorageProvider { get; set; }

        public static IOASISStorage CurrentStorageProvider { get; private set; } //TODO: Need to work this out because in future there can be more than one provider active at a time.

        public static bool OverrideProviderType { get; set; } = false;

        public static List<ProviderType> ProvidersThatAreAutoReplicating { get; set; }

        //public static List<ProviderType> RegisteredProviderTypes
        //{
        //    get
        //    {
        //        if (_registeredProviderTypes == null)
        //        {
        //            _registeredProviderTypes = new List<ProviderType>();
        //        }
        //    }
        //}

        //TODO: In future the registered providers will be dynamically loaded from MEF by watching a hot folder for compiled provider dlls (and other ways in future...)
        public static bool RegisterProvider(IOASISProvider provider)
        {
            if (!_registeredProviders.Any(x => x.ProviderName == provider.ProviderName))
            {
                _registeredProviders.Add(provider);
                _registeredProviderTypes.Add(provider.ProviderType);
                return true;
            }

            return false;
        }

        public static bool RegisterProviders(List<IOASISProvider> providers)
        {
            bool returnValue = false;

            foreach (IOASISProvider provider in providers)
                returnValue = RegisterProvider(provider);

            return returnValue;
        }

        public static bool UnRegisterProvider(IOASISProvider provider)
        {
            _registeredProviders.Remove(provider);
            _registeredProviderTypes.Remove(provider.ProviderType);
            return true;
        }

        public static bool UnRegisterProvider(ProviderType providerType)
        {
            foreach (IOASISProvider provider in _registeredProviders)
            {
                if (provider.ProviderType == providerType)
                    UnRegisterProvider(provider);
            }    
            
            return true;
        }

        public static bool UnRegisterProviders(List<ProviderType> providerTypes)
        {
            foreach (ProviderType providerType in providerTypes)
                UnRegisterProvider(providerType);

            return true;
        }

        public static bool UnRegisterProviders(List<IOASISProvider> providers)
        {
            foreach (IOASISProvider provider in providers)
                _registeredProviders.Remove(provider);

            return true;
        }

        public static List<IOASISProvider> GetAllRegisteredProviders()
        {
            return _registeredProviders;
        }

        public static List<ProviderType> GetAllRegisteredProviderTypes()
        {
            return _registeredProviderTypes;
        }

        public static List<IOASISProvider> GetProvidersOfCategory(ProviderCategory category)
        {
            return _registeredProviders.Where(x => x.ProviderCategory == category).ToList();
        }

        public static List<ProviderType> GetProviderTypesOfCategory(ProviderCategory category)
        {
            return GetProviderTypes(GetProvidersOfCategory(category));
        }

        public static List<IOASISStorage> GetStorageProviders()
        {
            List<IOASISStorage> storageProviders = new List<IOASISStorage>();

            foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategory == ProviderCategory.Storage || x.ProviderCategory == ProviderCategory.StorageAndNetwork).ToList())
                storageProviders.Add((IOASISStorage)provider);  

            return storageProviders;
        }

        public static List<ProviderType> GetStorageProviderTypes()
        {
            //return GetProviderTypes(GetStorageProviders().Select(x => x.ProviderType);
            return GetStorageProviders().Select(x => x.ProviderType).ToList();
        }

        public static List<IOASISNET> GetNetworkProviders()
        {
            List<IOASISNET> networkProviders = new List<IOASISNET>();

            foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategory == ProviderCategory.Network || x.ProviderCategory == ProviderCategory.StorageAndNetwork).ToList())
                networkProviders.Add((IOASISNET)provider);

            return networkProviders;
        }

        public static List<ProviderType> GetNetworkProviderTypes()
        {
            return GetNetworkProviders().Select(x => x.ProviderType).ToList();

            //List<ProviderType> providerTypes = new List<ProviderType>();

            //foreach (IOASISProvider provider in GetNetworkProviders())
            //    providerTypes.Add(provider.ProviderType);

            //return providerTypes;
        }

        public static List<ProviderType> GetProviderTypes(List<IOASISProvider> providers)
        {
            List<ProviderType> providerTypes = new List<ProviderType>();

            foreach (IOASISProvider provider in providers)
                providerTypes.Add(provider.ProviderType);

            return providerTypes;
        }

        public static List<IOASISRenderer> GetRendererProviders()
        {
            List<IOASISRenderer> rendererProviders = new List<IOASISRenderer>();

            foreach (IOASISProvider provider in _registeredProviders.Where(x => x.ProviderCategory == ProviderCategory.Renderer).ToList())
                rendererProviders.Add((IOASISRenderer)provider);

            return rendererProviders;
        }

        public static IOASISProvider GetProvider(ProviderType type)
        {
            return _registeredProviders.FirstOrDefault(x => x.ProviderType == type);
        }

        public static IOASISStorage GetStorageProvider(ProviderType type)
        {
            return (IOASISStorage)_registeredProviders.FirstOrDefault(x => x.ProviderType == type && x.ProviderCategory == ProviderCategory.Storage);
        }

        public static IOASISNET GetNetworkProvider(ProviderType type)
        {
            return (IOASISNET)_registeredProviders.FirstOrDefault(x => x.ProviderType == type && x.ProviderCategory == ProviderCategory.Network);
        }

        public static IOASISRenderer GetRendererProvider(ProviderType type)
        {
            return (IOASISRenderer)_registeredProviders.FirstOrDefault(x => x.ProviderType == type && x.ProviderCategory == ProviderCategory.Renderer);
        }

        public static bool IsProviderRegistered(IOASISProvider provider)
        {
            return _registeredProviders.Any(x => x.ProviderName == provider.ProviderName);
        }

        public static bool IsProviderRegistered(ProviderType providerType)
        {
            return _registeredProviders.Any(x => x.ProviderType == providerType);
        }

        //public static IOASISSuperStar SetAndActivateCurrentSuperStarProvider(ProviderType providerType)
        //{
        //    SetAndActivateCurrentStorageProvider(providerType);

            
      //  }

        // Called from Managers.
        public static IOASISStorage SetAndActivateCurrentStorageProvider(ProviderType providerType)
        {
            if (providerType == ProviderType.Default)
                return SetAndActivateCurrentStorageProvider();
            else
                return SetAndActivateCurrentStorageProvider(providerType, false);
        }

        //TODO: Called internally (make private ?)
        public static IOASISStorage SetAndActivateCurrentStorageProvider()
        {
            // If a global provider has been set and the REST API call has not overiden the provider (OverrideProviderType) then set to global provider.
            if (DefaultGlobalStorageProvider != null && DefaultGlobalStorageProvider != CurrentStorageProvider && !OverrideProviderType)
                return SetAndActivateCurrentStorageProvider(DefaultGlobalStorageProvider);

            // Otherwise set to default provider (configured in appSettings.json) if the provider has not been overiden in the REST call.
            else if (!OverrideProviderType && DefaultProviderTypes != null && CurrentStorageProviderType != (ProviderType)Enum.Parse(typeof(ProviderType), DefaultProviderTypes[0]))
                return SetAndActivateCurrentStorageProvider(ProviderType.Default, false);

            if (!_setProviderGlobally)
                OverrideProviderType = false;

            return CurrentStorageProvider;
        }

        // Called from ONODE.WebAPI.OASISProviderManager.
        public static IOASISStorage SetAndActivateCurrentStorageProvider(IOASISProvider OASISProvider)
        {
            if (OASISProvider != CurrentStorageProvider)
            {
                if (OASISProvider != null)
                {
                    if (!IsProviderRegistered(OASISProvider))
                        RegisterProvider(OASISProvider);

                    return SetAndActivateCurrentStorageProvider(OASISProvider.ProviderType);
                }
            }

            return CurrentStorageProvider;
        }

        // Called from ONODE.WebAPI.OASISProviderManager.
        //TODO: In future more than one StorageProvider will be active at a time so we need to work out how to handle this...
        public static IOASISStorage SetAndActivateCurrentStorageProvider(ProviderType providerType, bool setGlobally = false)
        {
            _setProviderGlobally = setGlobally;

            //TODO: Need to get this to use the next provider in the list if there is an issue with the first/current provider...
            if (providerType == ProviderType.Default && !OverrideProviderType)
                providerType = (ProviderType)Enum.Parse(typeof(ProviderType), DefaultProviderTypes[0]);

            if (providerType != CurrentStorageProviderType)
            {
                IOASISProvider provider = _registeredProviders.FirstOrDefault(x => x.ProviderType == providerType);

                if (provider == null)
                    throw new InvalidOperationException(string.Concat(Enum.GetName(typeof(ProviderType), providerType), " ProviderType is not registered. Please call RegisterProvider() method to register the provider before calling this method."));

                if (provider != null && (provider.ProviderCategory == ProviderCategory.Storage || provider.ProviderCategory == ProviderCategory.StorageAndNetwork))
                {
                    CurrentStorageProviderType = providerType;
                    CurrentStorageProvider = (IOASISStorage)provider;
                    CurrentStorageProvider.ActivateProvider();

                    if (setGlobally)
                        DefaultGlobalStorageProvider = CurrentStorageProvider;
                }
            }

            return CurrentStorageProvider;
        }

        public static bool ActivateProvider(ProviderType type)
        {
            IOASISProvider provider = _registeredProviders.FirstOrDefault(x => x.ProviderType == type);

            if (provider != null)
            {
                provider.ActivateProvider();
                return true;
            }

            return false;
        }

        public static bool DeActivateProvider(ProviderType type)
        {
            IOASISProvider provider = _registeredProviders.FirstOrDefault(x => x.ProviderType == type);

            if (provider != null)
            {
                provider.DeActivateProvider();
                return true;
            }

            return false;
        }

        public bool SetAutoReplicate(bool autoReplicate, List<ProviderType> providers)
        {
            foreach (ProviderType providerType in providers)
            {
                foreach (ProviderType providerTypeRegistered in ProvidersThatAreAutoReplicating)
                {
                    if (providerType == providerTypeRegistered)
                    {
                        if (!autoReplicate)
                            ProvidersThatAreAutoReplicating.Remove(providerTypeRegistered);

                    }
                }
            }
            
            //ProvidersThatAreAutoReplicating = providers;


            foreach (ProviderType providerType in providers)
            {
                //pro    
            }
        }

        public bool SetAutoReplicate(bool autoReplicate)
        {
            return SetAutoReplicate(autoReplicate, _registeredProviderTypes);
        }
    }
}
