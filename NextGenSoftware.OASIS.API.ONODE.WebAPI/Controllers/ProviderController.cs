using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/provider")]
    public class ProviderController : OASISControllerBase
    {
        // OASISDNA _settings;

        //public ProviderController(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
        //{
        //    _settings = OASISSettings.Value;
        //}

        public ProviderController()
        {

        }

        /// <summary>
        /// Get's the current active storage provider.
        /// </summary>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("GetCurrentStorageProvider")]
        public OASISResult<IOASISStorageProvider> GetCurrentStorageProvider()
        {
            return new(ProviderManager.CurrentStorageProvider);
        }

        /// <summary>
        /// Get's the current active storage provider type.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetCurrentStorageProviderType")]
        public OASISResult<EnumValue<ProviderType>> GetCurrentStorageProviderType()
        {
            return new(ProviderManager.CurrentStorageProviderType);
        }

        /// <summary>
        /// Get all registered providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredProviders")]
        public OASISResult<IEnumerable<IOASISProvider>> GetAllRegisteredProviders()
        {
            return new(ProviderManager.GetAllRegisteredProviders());
        }

        /// <summary>
        /// Get all registered provider types.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredProviderTypes")]
        public OASISResult<IEnumerable<EnumValue<ProviderType>>> GetAllRegisteredProviderTypes()
        {
            return new(ProviderManager.GetAllRegisteredProviderTypes());
        }

        /// <summary>
        /// Get all registered storage providers for a given category.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredProvidersForCategory/{category}")]
        public OASISResult<IEnumerable<IOASISProvider>> GetAllRegisteredProvidersForCategory(ProviderCategory category)
        {
            return new(ProviderManager.GetProvidersOfCategory(category));
        }

        /// <summary>
        /// Get all registered storage providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredStorageProviders")]
        public OASISResult<IEnumerable<IOASISProvider>> GetAllRegisteredStorageProviders()
        {
            return new(ProviderManager.GetStorageProviders());
        }

        /// <summary>
        /// Get all registered network providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredNetworkProviders")]
        public OASISResult<IEnumerable<IOASISProvider>> GetAllRegisteredNetworkProviders()
        {
            return new(ProviderManager.GetNetworkProviders());
        }

        /// <summary>
        /// Get all registered renderer providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredRendererProviders")]
        public OASISResult<IEnumerable<IOASISProvider>> GetAllRegisteredRendererProviders()
        {
            return new(ProviderManager.GetRendererProviders());
        }

        /// <summary>
        /// Get's the provider for the given providerType. (The provider must already be registered).
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetRegisteredProvider/{providerType}")]
        public OASISResult<IOASISProvider> GetRegisteredProvider(ProviderType providerType)
        {
            return new(ProviderManager.GetProvider(providerType));
        }

        /// <summary>
        /// Return true if the given provider has been registered, false if it has not.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("IsProviderRegistered/{providerType}")]
        public OASISResult<bool> IsProviderRegistered(ProviderType providerType)
        {
            return new (ProviderManager.IsProviderRegistered(providerType));
        }

        /// <summary>
        /// Get's the list of providers that are auto-replicating. See SetAutoReplicate methods below for more info.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetProvidersThatAreAutoReplicating")]
        public OASISResult<IEnumerable<EnumValue<ProviderType>>> GetProvidersThatAreAutoReplicating()
        {
            return new(ProviderManager.GetProvidersThatAreAutoReplicating());
        }

        /// <summary>
        /// Get's the list of providers that have auto-failover enabled. See SetAutoFailOver methods below for more info.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetProvidersThatHaveAutoFailOverEnabled")]
        public OASISResult<IEnumerable<EnumValue<ProviderType>>> GetProvidersThatHaveAutoFailOverEnabled()
        {
            return new(ProviderManager.GetProviderAutoFailOverList());
        }

        /// <summary>
        /// Get's the list of providers that have auto-load balance enabled. See SetAutoLoadBalance methods below for more info.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetProvidersThatHaveAutoLoadBalanceEnabled")]
        public OASISResult<IEnumerable<EnumValue<ProviderType>>> GetProvidersThatHaveAutoLoadBalanceEnabled()
        {
            return new(ProviderManager.GetProviderAutoLoadBalanceList());
        }

        /// <summary>
        /// Register the given provider.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterProvider/{provider}")]
        public OASISResult<bool> RegisterProvider(IOASISProvider provider)
        {
            return new(ProviderManager.RegisterProvider(provider));
        }

        /// <summary>
        /// Register the given provider type.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterProviderType/{providerType}")]
        public OASISResult<bool> RegisterProviderType(ProviderType providerType)
        {
            return new(OASISBootLoader.OASISBootLoader.RegisterProvider(providerType) != null);
        }

        /// <summary>
        /// Register the given providers.
        /// </summary>
        /// <param name="providers"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterProviders/{providers}")]
        public OASISResult<bool> RegisterProviders(List<IOASISProvider> providers)
        {
            return new(ProviderManager.RegisterProviders(providers));
        }

        /// <summary>
        /// Register the given provider types.
        /// </summary>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterProviderTypes/{providerTypes}")]
        public OASISResult<IEnumerable<IOASISStorageProvider>> RegisterProviderTypes(string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<IOASISStorageProvider> providers = new List<IOASISStorageProvider>();

            foreach (string type in types)
                OASISBootLoader.OASISBootLoader.RegisterProvider((ProviderType)Enum.Parse(typeof(ProviderType), type));

            return new(providers);
        }

        /// <summary>
        /// Unregister the given provider.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UnRegisterProvider/{provider}")]
        public OASISResult<bool> UnRegisterProvider(IOASISProvider provider)
        {
            return new(ProviderManager.UnRegisterProvider(provider));
        }

        /// <summary>
        /// Unregister the given provider.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UnRegisterProviderType/{providerType}")]
        public OASISResult<bool> UnRegisterProviderType(ProviderType providerType)
        {
            return new(ProviderManager.UnRegisterProvider(providerType));
        }

        /// <summary>
        /// Unregisters the list of providers passed in.
        /// </summary>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UnRegisterProviderTypes/{providerTypes}")]
        public OASISResult<bool> UnRegisterProviderTypes(string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));

            return new(ProviderManager.UnRegisterProviders(providerTypesList));
        }

        /// <summary>
        /// Unregisters the list of providers passed in.
        /// </summary>
        /// <param name="providers"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UnRegisterProviders/{providers}")]
        public OASISResult<bool> UnRegisterProviders(List<IOASISProvider> providers)
        {
            return new(ProviderManager.UnRegisterProviders(providers));
        }

        /// <summary>
        /// Set and activate the current storage provider. If the setGlobally flag is false then this will only apply to the next request made before reverting back to the default provider. If this is set to true then it will permanently switch to the new provider for all future requests.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAndActivateCurrentStorageProvider/{providerType}/{setGlobally}")]
        public OASISResult<bool> SetAndActivateCurrentStorageProvider(ProviderType providerType, bool setGlobally)
        {
            return new(GetAndActivateProvider(providerType, setGlobally) != null);
        }

        /// <summary>
        /// Activate the given provider.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("ActivateProvider/{providerType}")]
        public OASISResult<bool> ActivateProvider(ProviderType providerType)
        {
            return ProviderManager.ActivateProvider(providerType);
        }

        /// <summary>
        /// Deactivate the given provider.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DeActivateProvider/{providerType}")]
        public OASISResult<bool> DeActivateProvider(ProviderType providerType)
        {
            return ProviderManager.DeActivateProvider(providerType);
        }

        /// <summary>
        /// Enable/disable auto-replication between providers. If this is set to true then the OASIS will automatically replicate all data including the user's avatar to all available providers.
        /// </summary>
        /// <param name="autoReplicate"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoReplicateForAllProviders/{autoReplicate}")]
        public OASISResult<bool> SetAutoReplicateForAllProviders(bool autoReplicate)
        {
            return ProviderManager.SetAutoReplicateForAllProviders(autoReplicate) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoReplicatingList() : new(false);
        }

        /// <summary>
        /// Enable/disable auto-replication between providers. If this is set to true then the OASIS will automatically replicate all data including the user's avatar to the list of providers passed in. The OASIS will continue to replicate to the given providers until this method is called again passing in false along with a list of providers to disable auto-replication. NOTE: If a provider is in the list of providers to auto-replicate but is missing from the list when false is passed in, then it will continue to auto-replicate.
        /// </summary>
        /// <param name="autoReplicate"></param>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoReplicateForListOfProviders/{autoReplicate}/{providerTypes}")]
        public OASISResult<bool> SetAutoReplicateForListOfProviders(bool autoReplicate, string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));
            return ProviderManager.SetAutoReplicationForProviders(autoReplicate, new List<ProviderType>(providerTypesList)) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoReplicatingList() : new(false);
        }


        /// <summary>
        /// Enable/disable auto-failover for all providers. If this is set to true then the OASIS will automatically switch to the next provider if the current one fails. If load balancing is switched on then it will automatically switch to the next fastest provider.
        /// </summary>
        /// <param name="addToFailOverList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoFailOverForAllProviders/{addToFailOverList}")]
        public OASISResult<bool> SetAutoFailOverForAllProviders(bool addToFailOverList)
        {
            return ProviderManager.SetAutoFailOverForAllProviders(addToFailOverList) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoFailOverList() : new (false);
        }

        /// <summary>
        /// Enable/disable auto-failover for providers. If this is set to true then the OASIS will automatically switch to the next provider in the list of providers passed in if the current one fails. If load balancing is switched on then it will automatically switch to the next fastest provider. The OASIS will continue to auto-fail over to the next provider in the list until this method is called again passing in false along with a list of providers to disable auto-failover. NOTE: If a provider is in the list of providers to auto-failover but is missing from the list when false is passed in, then it will continue to auto-failover.
        /// </summary>
        /// <param name="addToFailOverList"></param>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoFailOverForListOfProviders/{addToFailOverList}/{providerTypes}")]
        public OASISResult<bool> SetAutoFailOverForListOfProviders(bool addToFailOverList, string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));
            return ProviderManager.SetAutoFailOverForProviders(addToFailOverList, new List<ProviderType>(providerTypesList)) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoFailOverList() : new(false);
        }

        /// <summary>
        /// Enable/disable auto-load balance for all providers. If this is set to true then the OASIS will automatically switch to the fastest provider within the ONODE's neighbourhood. This effectively load balances the entire internet.
        /// </summary>
        /// <param name="addToLoadBalanceList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoLoadBalanceForAllProviders/{addToLoadBalanceList}")]
        public OASISResult<bool> SetAutoLoadBalanceForAllProviders(bool addToLoadBalanceList)
        {
            return ProviderManager.SetAutoLoadBalanceForAllProviders(addToLoadBalanceList) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoLoadBalanceList() : new(false);
        }

        /// <summary>
        /// Enable/disable auto-load balance for providers. If this is set to true then the OASIS will automatically switch to the fastest provider within the ONODE's neighbourhood. This effectively load balances the entire internet. The OASIS will continue to auto-load balance over providers in the list until this method is called again passing in false along with a list of providers to disable auto-loadbalance. NOTE: If a provider is in the list of providers to auto-loadbalance but is missing from the list when false is passed in, then it will continue to auto-loadbalance.
        /// </summary>
        /// <param name="addToLoadBalanceList"></param>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoLoadBalanceForListOfProviders/{addToLoadBalanceList}/{providerTypes}")]
        public OASISResult<bool> SetAutoLoadBalanceForListOfProviders(bool addToLoadBalanceList, string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));
            
            return ProviderManager.SetAutoLoadBalanceForProviders(addToLoadBalanceList, new List<ProviderType>(providerTypesList)) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoLoadBalanceList() : new(false);
        }

        /// <summary>
        /// Override a provider's config such as connnectionstring, etc
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("SetProviderConfig/{providerType}/{connectionString}")]
        public OASISResult<bool> SetProviderConfig(ProviderType providerType, string connectionString)
        {
            //TODO: Test this works and then implement for rest of providers...
            switch (providerType)
            {
                case ProviderType.MongoDBOASIS:
                    {
                        OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.MongoDBOASIS.ConnectionString = connectionString;

                        ProviderManager.DeActivateProvider(ProviderType.MongoDBOASIS);
                        ProviderManager.UnRegisterProvider(ProviderType.MongoDBOASIS);
                    }
                    break;
            }

            return new(true);
        }
    }
}
