using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// OASIS provider management endpoints for configuring and managing storage, blockchain, and network providers.
    /// Provides access to provider information, switching between providers, and provider configuration.
    /// </summary>
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
        /// <returns>OASIS result containing the current active storage provider.</returns>
        /// <response code="200">Current storage provider retrieved successfully</response>
        /// <response code="401">Unauthorized - Wizard access required</response>
        [Authorize(AvatarType.Wizard)]
        [HttpGet("get-current-storage-provider")]
        [ProducesResponseType(typeof(OASISResult<IOASISStorageProvider>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<IOASISStorageProvider> GetCurrentStorageProvider()
        {
            return new(ProviderManager.Instance.CurrentStorageProvider);
        }

        /// <summary>
        /// Get's the current active storage provider type.
        /// </summary>
        /// <returns>OASIS result containing the current active storage provider type.</returns>
        /// <response code="200">Current storage provider type retrieved successfully</response>
        /// <response code="401">Unauthorized - authentication required</response>
        [Authorize]
        [HttpGet("get-current-storage-provider-type")]
        [ProducesResponseType(typeof(OASISResult<EnumValue<ProviderType>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OASISResult<string>), StatusCodes.Status401Unauthorized)]
        public OASISResult<EnumValue<ProviderType>> GetCurrentStorageProviderType()
        {
            return new(ProviderManager.Instance.CurrentStorageProviderType);
        }

        /// <summary>
        /// Get all registered providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-registered-providers")]
        public OASISResult<IEnumerable<IOASISProvider>> GetAllRegisteredProviders()
        {
            return new(ProviderManager.Instance.GetAllRegisteredProviders());
        }

        /// <summary>
        /// Get all registered provider types.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-registered-provider-types")]
        public OASISResult<IEnumerable<EnumValue<ProviderType>>> GetAllRegisteredProviderTypes()
        {
            return new(ProviderManager.Instance.GetAllRegisteredProviderTypes());
        }

        /// <summary>
        /// Get all registered storage providers for a given category.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-registered-providers-for-category/{category}")]
        public OASISResult<IEnumerable<IOASISProvider>> GetAllRegisteredProvidersForCategory(ProviderCategory category)
        {
            return new(ProviderManager.Instance.GetProvidersOfCategory(category));
        }

        /// <summary>
        /// Get all registered storage providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-registered-storage-providers")]
        public OASISResult<IEnumerable<IOASISProvider>> GetAllRegisteredStorageProviders()
        {
            return new(ProviderManager.Instance.GetStorageProviders());
        }

        /// <summary>
        /// Get all registered network providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-registered-network-providers")]
        public OASISResult<IEnumerable<IOASISProvider>> GetAllRegisteredNetworkProviders()
        {
            return new(ProviderManager.Instance.GetNetworkProviders());
        }

        /// <summary>
        /// Get all registered renderer providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-all-registered-renderer-providers")]
        public OASISResult<IEnumerable<IOASISProvider>> GetAllRegisteredRendererProviders()
        {
            return new(ProviderManager.Instance.GetRendererProviders());
        }

        /// <summary>
        /// Get's the provider for the given providerType. (The provider must already be registered).
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-registered-provider/{providerType}")]
        public OASISResult<IOASISProvider> GetRegisteredProvider(ProviderType providerType)
        {
            return new(ProviderManager.Instance.GetProvider(providerType));
        }

        /// <summary>
        /// Return true if the given provider has been registered, false if it has not.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("is-provider-registered/{providerType}")]
        public OASISResult<bool> IsProviderRegistered(ProviderType providerType)
        {
            return new (ProviderManager.Instance.IsProviderRegistered(providerType));
        }

        /// <summary>
        /// Get's the list of providers that are auto-replicating. See SetAutoReplicate methods below for more info.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-providers-that-are-auto-replicating")]
        public OASISResult<IEnumerable<EnumValue<ProviderType>>> GetProvidersThatAreAutoReplicating()
        {
            return new(ProviderManager.Instance.GetProvidersThatAreAutoReplicating());
        }

        /// <summary>
        /// Get's the list of providers that have auto-failover enabled. See SetAutoFailOver methods below for more info.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-providers-that-have-auto-fail-over-enabled")]
        public OASISResult<IEnumerable<EnumValue<ProviderType>>> GetProvidersThatHaveAutoFailOverEnabled()
        {
            return new(ProviderManager.Instance.GetProviderAutoFailOverList());
        }

        /// <summary>
        /// Get's the list of providers that have auto-load balance enabled. See SetAutoLoadBalance methods below for more info.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("get-providers-that-have-auto-load-balance-enabled")]
        public OASISResult<IEnumerable<EnumValue<ProviderType>>> GetProvidersThatHaveAutoLoadBalanceEnabled()
        {
            return new(ProviderManager.Instance.GetProviderAutoLoadBalanceList());
        }

        /// <summary>
        /// Register the given provider.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("register-provider/{provider}")]
        public OASISResult<bool> RegisterProvider(IOASISProvider provider)
        {
            return new(ProviderManager.Instance.RegisterProvider(provider));
        }

        /// <summary>
        /// Register the given provider type.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("register-provider-type/{providerType}")]
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
        [HttpPost("register-providers/{providers}")]
        public OASISResult<bool> RegisterProviders(List<IOASISProvider> providers)
        {
            return new(ProviderManager.Instance.RegisterProviders(providers));
        }

        /// <summary>
        /// Register the given provider types.
        /// </summary>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("register-provider-types/{providerTypes}")]
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
        [HttpPost("unregister-provider/{provider}")]
        public OASISResult<bool> UnRegisterProvider(IOASISProvider provider)
        {
            return new(ProviderManager.Instance.UnRegisterProvider(provider));
        }

        /// <summary>
        /// Unregister the given provider.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("unregister-provider-type/{providerType}")]
        public OASISResult<bool> UnRegisterProviderType(ProviderType providerType)
        {
            return new(ProviderManager.Instance.UnRegisterProvider(providerType));
        }

        /// <summary>
        /// Unregisters the list of providers passed in.
        /// </summary>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("unregister-provider-types/{providerTypes}")]
        public OASISResult<bool> UnRegisterProviderTypes(string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));

            return new(ProviderManager.Instance.UnRegisterProviders(providerTypesList));
        }

        /// <summary>
        /// Unregisters the list of providers passed in.
        /// </summary>
        /// <param name="providers"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("unregister-providers/{providers}")]
        public OASISResult<bool> UnRegisterProviders(List<IOASISProvider> providers)
        {
            return new(ProviderManager.Instance.UnRegisterProviders(providers));
        }

        /// <summary>
        /// Set and activate the current storage provider. If the setGlobally flag is false then this will only apply to the next request made before reverting back to the default provider. If this is set to true then it will permanently switch to the new provider for all future requests.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("set-and-activate-current-storage-provider/{providerType}/{setGlobally}")]
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
        [HttpPost("activate-provider/{providerType}")]
        public OASISResult<bool> ActivateProvider(ProviderType providerType)
        {
            return ProviderManager.Instance.ActivateProvider(providerType);
        }

        /// <summary>
        /// Deactivate the given provider.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("deactivate-provider/{providerType}")]
        public OASISResult<bool> DeActivateProvider(ProviderType providerType)
        {
            return ProviderManager.Instance.DeActivateProvider(providerType);
        }

        /// <summary>
        /// Enable/disable auto-replication between providers. If this is set to true then the OASIS will automatically replicate all data including the user's avatar to all available providers.
        /// </summary>
        /// <param name="autoReplicate"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("set-auto-replicate-for-all-providers/{autoReplicate}")]
        public OASISResult<bool> SetAutoReplicateForAllProviders(bool autoReplicate)
        {
            return ProviderManager.Instance.SetAutoReplicateForAllProviders(autoReplicate) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoReplicatingList() : new(false);
        }

        /// <summary>
        /// Enable/disable auto-replication between providers. If this is set to true then the OASIS will automatically replicate all data including the user's avatar to the list of providers passed in. The OASIS will continue to replicate to the given providers until this method is called again passing in false along with a list of providers to disable auto-replication. NOTE: If a provider is in the list of providers to auto-replicate but is missing from the list when false is passed in, then it will continue to auto-replicate.
        /// </summary>
        /// <param name="autoReplicate"></param>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("set-auto-replicate-for-list-of-providers/{autoReplicate}/{providerTypes}")]
        public OASISResult<bool> SetAutoReplicateForListOfProviders(bool autoReplicate, string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));
            return ProviderManager.Instance.SetAutoReplicationForProviders(autoReplicate, new List<ProviderType>(providerTypesList)) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoReplicatingList() : new(false);
        }


        /// <summary>
        /// Enable/disable auto-failover for all providers. If this is set to true then the OASIS will automatically switch to the next provider if the current one fails. If load balancing is switched on then it will automatically switch to the next fastest provider.
        /// </summary>
        /// <param name="addToFailOverList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("set-auto-fail-over-for-all-providers/{addToFailOverList}")]
        public OASISResult<bool> SetAutoFailOverForAllProviders(bool addToFailOverList)
        {
            return ProviderManager.Instance.SetAutoFailOverForAllProviders(addToFailOverList) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoFailOverList() : new (false);
        }

        /// <summary>
        /// Enable/disable auto-failover for providers. If this is set to true then the OASIS will automatically switch to the next provider in the list of providers passed in if the current one fails. If load balancing is switched on then it will automatically switch to the next fastest provider. The OASIS will continue to auto-fail over to the next provider in the list until this method is called again passing in false along with a list of providers to disable auto-failover. NOTE: If a provider is in the list of providers to auto-failover but is missing from the list when false is passed in, then it will continue to auto-failover.
        /// </summary>
        /// <param name="addToFailOverList"></param>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("set-auto-fail-over-for-list-of-providers/{addToFailOverList}/{providerTypes}")]
        public OASISResult<bool> SetAutoFailOverForListOfProviders(bool addToFailOverList, string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));
            return ProviderManager.Instance.SetAutoFailOverForProviders(addToFailOverList, new List<ProviderType>(providerTypesList)) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoFailOverList() : new(false);
        }

        /// <summary>
        /// Enable/disable auto-load balance for all providers. If this is set to true then the OASIS will automatically switch to the fastest provider within the ONODE's neighbourhood. This effectively load balances the entire internet.
        /// </summary>
        /// <param name="addToLoadBalanceList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("set-auto-load-balance-for-all-providers/{addToLoadBalanceList}")]
        public OASISResult<bool> SetAutoLoadBalanceForAllProviders(bool addToLoadBalanceList)
        {
            return ProviderManager.Instance.SetAutoLoadBalanceForAllProviders(addToLoadBalanceList) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoLoadBalanceList() : new(false);
        }

        /// <summary>
        /// Enable/disable auto-load balance for providers. If this is set to true then the OASIS will automatically switch to the fastest provider within the ONODE's neighbourhood. This effectively load balances the entire internet. The OASIS will continue to auto-load balance over providers in the list until this method is called again passing in false along with a list of providers to disable auto-loadbalance. NOTE: If a provider is in the list of providers to auto-loadbalance but is missing from the list when false is passed in, then it will continue to auto-loadbalance.
        /// </summary>
        /// <param name="addToLoadBalanceList"></param>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("set-auto-load-balance-for-list-of-providers/{addToLoadBalanceList}/{providerTypes}")]
        public OASISResult<bool> SetAutoLoadBalanceForListOfProviders(bool addToLoadBalanceList, string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));
            
            return ProviderManager.Instance.SetAutoLoadBalanceForProviders(addToLoadBalanceList, new List<ProviderType>(providerTypesList)) ? OASISBootLoader.OASISBootLoader.RegisterProvidersInAutoLoadBalanceList() : new(false);
        }

        /// <summary>
        /// Override a provider's config such as connnectionstring, etc
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("set-provider-config/{providerType}/{connectionString}")]
        public OASISResult<bool> SetProviderConfig(ProviderType providerType, string connectionString)
        {
            //TODO: Test this works and then implement for rest of providers...
            switch (providerType)
            {
                case ProviderType.MongoDBOASIS:
                    {
                        OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.StorageProviders.MongoDBOASIS.ConnectionString = connectionString;

                        ProviderManager.Instance.DeActivateProvider(ProviderType.MongoDBOASIS);
                        ProviderManager.Instance.UnRegisterProvider(ProviderType.MongoDBOASIS);
                    }
                    break;
            }

            return new(true);
        }
    }
}
