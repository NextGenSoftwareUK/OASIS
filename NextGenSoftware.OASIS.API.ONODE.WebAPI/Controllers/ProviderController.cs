using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System;

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
        public ActionResult<IOASISStorage> GetCurrentStorageProvider()
        {
            return Ok(ProviderManager.CurrentStorageProvider);
        }

        /// <summary>
        /// Get's the current active storage provider type.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetCurrentStorageProviderType")]
        public ActionResult<EnumValue<ProviderType>> GetCurrentStorageProviderType()
        {
            return Ok(ProviderManager.CurrentStorageProviderType);
        }

        /// <summary>
        /// Get all registered providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredProviders")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredProviders()
        {
            return Ok(ProviderManager.GetAllRegisteredProviders());
        }

        /// <summary>
        /// Get all registered provider types.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredProviderTypes")]
        public ActionResult<IEnumerable<EnumValue<ProviderType>>> GetAllRegisteredProviderTypes()
        {
            return Ok(ProviderManager.GetAllRegisteredProviderTypes());
        }

        /// <summary>
        /// Get all registered storage providers for a given category.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredProvidersForCategory/{category}")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredProvidersForCategory(ProviderCategory category)
        {
            return Ok(ProviderManager.GetProvidersOfCategory(category));
        }

        /// <summary>
        /// Get all registered storage providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredStorageProviders")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredStorageProviders()
        {
            return Ok(ProviderManager.GetStorageProviders());
        }

        /// <summary>
        /// Get all registered network providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredNetworkProviders")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredNetworkProviders()
        {
            return Ok(ProviderManager.GetNetworkProviders());
        }

        /// <summary>
        /// Get all registered renderer providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredRendererProviders")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredRendererProviders()
        {
            return Ok(ProviderManager.GetRendererProviders());
        }

        /// <summary>
        /// Get's the provider for the given providerType. (The provider must already be registered).
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetRegisteredProvider/{providerType}")]
        public ActionResult<IOASISProvider> GetRegisteredProvider(ProviderType providerType)
        {
            return Ok(ProviderManager.GetProvider(providerType));
        }

        /// <summary>
        /// Return true if the given provider has been registered, false if it has not.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("IsProviderRegistered/{providerType}")]
        public ActionResult<bool> IsProviderRegistered(ProviderType providerType)
        {
            return Ok(ProviderManager.IsProviderRegistered(providerType));
        }

        /// <summary>
        /// Get's the list of providers that are auto-replicating. See SetAutoReplicate methods below for more info.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetProvidersThatAreAutoReplicating")]
        public ActionResult<EnumValue<ProviderType>[]> GetProvidersThatAreAutoReplicating()
        {
            return Ok(ProviderManager.GetProvidersThatAreAutoReplicating());
        }

        /// <summary>
        /// Get's the list of providers that have auto-failover enabled. See SetAutoFailOver methods below for more info.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetProvidersThatHaveAutoFailOverEnabled")]
        public ActionResult<EnumValue<ProviderType>[]> GetProvidersThatHaveAutoFailOverEnabled()
        {
            return Ok(ProviderManager.GetProviderAutoFailOverList());
        }

        /// <summary>
        /// Get's the list of providers that have auto-load balance enabled. See SetAutoLoadBalance methods below for more info.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetProvidersThatHaveAutoLoadBalanceEnabled")]
        public ActionResult<EnumValue<ProviderType>[]> GetProvidersThatHaveAutoLoadBalanceEnabled()
        {
            return Ok(ProviderManager.GetProviderAutoLoadBalanceList());
        }

        /// <summary>
        /// Register the given provider.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterProvider/{provider}")]
        public ActionResult<bool> RegisterProvider(IOASISProvider provider)
        {
            return Ok(ProviderManager.RegisterProvider(provider));
        }

        /// <summary>
        /// Register the given provider type.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterProviderType/{providerType}")]
        public ActionResult<bool> RegisterProviderType(ProviderType providerType)
        {
            return Ok(OASISDNAManager.RegisterProvider(providerType) != null);
        }

        /// <summary>
        /// Register the given providers.
        /// </summary>
        /// <param name="providers"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterProviders/{providers}")]
        public ActionResult<bool> RegisterProviders(List<IOASISProvider> providers)
        {
            return Ok(ProviderManager.RegisterProviders(providers));
        }

        /// <summary>
        /// Register the given provider types.
        /// </summary>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("RegisterProviderTypes/{providerTypes}")]
        public ActionResult<IOASISStorage[]> RegisterProviderTypes(string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<IOASISStorage> providers = new List<IOASISStorage>();

            foreach (string type in types)
                OASISDNAManager.RegisterProvider((ProviderType)Enum.Parse(typeof(ProviderType), type));

            return Ok(providers);
        }

        /// <summary>
        /// Unregister the given provider.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UnRegisterProvider/{provider}")]
        public ActionResult<bool> UnRegisterProvider(IOASISProvider provider)
        {
            return Ok(ProviderManager.UnRegisterProvider(provider));
        }

        /// <summary>
        /// Unregister the given provider.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UnRegisterProviderType/{providerType}")]
        public ActionResult<bool> UnRegisterProviderType(ProviderType providerType)
        {
            return Ok(ProviderManager.UnRegisterProvider(providerType));
        }

        /// <summary>
        /// Unregisters the list of providers passed in.
        /// </summary>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UnRegisterProviderTypes/{providerTypes}")]
        public ActionResult<bool> UnRegisterProviderTypes(string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));

            return Ok(ProviderManager.UnRegisterProviders(providerTypesList));
        }

        /// <summary>
        /// Unregisters the list of providers passed in.
        /// </summary>
        /// <param name="providers"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UnRegisterProviders/{providers}")]
        public ActionResult<bool> UnRegisterProviders(List<IOASISProvider> providers)
        {
            return Ok(ProviderManager.UnRegisterProviders(providers));
        }

        /// <summary>
        /// Set and activate the current storage provider. If the setGlobally flag is false then this will only apply to the next request made before reverting back to the default provider. If this is set to true then it will permanently switch to the new provider for all future requests.
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="setGlobally"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAndActivateCurrentStorageProvider/{providerType}/{setGlobally}")]
        public ActionResult<bool> SetAndActivateCurrentStorageProvider(ProviderType providerType, bool setGlobally)
        {
            return Ok(GetAndActivateProvider(providerType, setGlobally) != null);
        }

        /// <summary>
        /// Activate the given provider.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("ActivateProvider/{providerType}")]
        public ActionResult<bool> ActivateProvider(ProviderType providerType)
        {
            return Ok(ProviderManager.ActivateProvider(providerType));
        }

        /// <summary>
        /// Deactivate the given provider.
        /// </summary>
        /// <param name="providerType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("DeActivateProvider/{providerType}")]
        public ActionResult<bool> DeActivateProvider(ProviderType providerType)
        {
            return Ok(ProviderManager.DeActivateProvider(providerType));
        }

        ///// <summary>
        ///// Set's the default providers to be used (in priority order).
        ///// </summary>
        ///// <param name="providers"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("SetDefaultProviders/{providers}")]
        //public ActionResult<bool> SetDefaultProviders(string[] providers)
        //{
        //    ProviderManager.DefaultProviderTypes = providers;
        //    return Ok(true);
        //}

        /// <summary>
        /// Enable/disable auto-replication between providers. If this is set to true then the OASIS will automatically replicate all data including the user's avatar to all available providers.
        /// </summary>
        /// <param name="autoReplicate"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoReplicateForAllProviders/{autoReplicate}")]
        public ActionResult<bool> SetAutoReplicateForAllProviders(bool autoReplicate)
        {
            return Ok(ProviderManager.SetAutoReplicateForAllProviders(autoReplicate));
        }

        /// <summary>
        /// Enable/disable auto-replication between providers. If this is set to true then the OASIS will automatically replicate all data including the user's avatar to the list of providers passed in. The OASIS will continue to replicate to the given providers until this method is called again passing in false along with a list of providers to disable auto-replication. NOTE: If a provider is in the list of providers to auto-replicate but is missing from the list when false is passed in, then it will continue to auto-replicate.
        /// </summary>
        /// <param name="autoReplicate"></param>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoReplicateForListOfProviders/{autoReplicate}/{providerTypes}")]
        //public ActionResult<bool> SetAutoReplicateForListOfProviders(bool autoReplicate, ProviderType[] providerTypes)
        public ActionResult<bool> SetAutoReplicateForListOfProviders(bool autoReplicate, string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));

            return Ok(ProviderManager.SetAutoReplicationForProviders(autoReplicate, new List<ProviderType>(providerTypesList)));
        }

        ///// <summary>
        ///// Enable/disable auto-replication for the provider. If this is set to true then the OASIS will automatically replicate all data including the user's avatar to the list of providers passed in. The OASIS will continue to replicate to the given providers until this method is called again passing in false along with a list of providers to disable auto-replication. NOTE: If a provider is in the list of providers to auto-replicate but is missing from the list when false is passed in, then it will continue to auto-replicate.
        ///// </summary>
        ///// <param name="autoReplicate"></param>
        ///// <param name="providerType"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("SetAutoReplicateForProvider/{autoReplicate}/{providerType}")]
        //public ActionResult<bool> SetAutoReplicateForProvider(bool autoReplicate, ProviderType providerType)
        //{
        //    return Ok(ProviderManager.SetAutoReplicateForProviders(autoReplicate, new List<ProviderType>() { providerType }));
        //}


        /// <summary>
        /// Enable/disable auto-failover for all providers. If this is set to true then the OASIS will automatically switch to the next provider if the current one fails. If load balancing is switched on then it will automatically switch to the next fastest provider.
        /// </summary>
        /// <param name="addToFailOverList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoFailOverForAllProviders/{addToFailOverList}")]
        public ActionResult<bool> SetAutoFailOverForAllProviders(bool addToFailOverList)
        {
            return Ok(ProviderManager.SetAutoFailOverForAllProviders(addToFailOverList));
        }

        /// <summary>
        /// Enable/disable auto-failover for providers. If this is set to true then the OASIS will automatically switch to the next provider in the list of providers passed in if the current one fails. If load balancing is switched on then it will automatically switch to the next fastest provider. The OASIS will continue to auto-fail over to the next provider in the list until this method is called again passing in false along with a list of providers to disable auto-failover. NOTE: If a provider is in the list of providers to auto-failover but is missing from the list when false is passed in, then it will continue to auto-failover.
        /// </summary>
        /// <param name="addToFailOverList"></param>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoFailOverForListOfProviders/{addToFailOverList}/{providerTypes}")]
        public ActionResult<bool> SetAutoFailOverForListOfProviders(bool addToFailOverList, string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));

            return Ok(ProviderManager.SetAutoFailOverForProviders(addToFailOverList, new List<ProviderType>(providerTypesList)));
        }

        /// <summary>
        /// Enable/disable auto-load balance for all providers. If this is set to true then the OASIS will automatically switch to the fastest provider within the ONODE's neighbourhood. This effectively load balances the entire internet.
        /// </summary>
        /// <param name="addToLoadBalanceList"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoLoadBalanceForAllProviders/{addToLoadBalanceList}")]
        public ActionResult<bool> SetAutoLoadBalanceForAllProviders(bool addToLoadBalanceList)
        {
            return Ok(ProviderManager.SetAutoLoadBalanceForAllProviders(addToLoadBalanceList));
        }

        /// <summary>
        /// Enable/disable auto-load balance for providers. If this is set to true then the OASIS will automatically switch to the fastest provider within the ONODE's neighbourhood. This effectively load balances the entire internet. The OASIS will continue to auto-load balance over providers in the list until this method is called again passing in false along with a list of providers to disable auto-loadbalance. NOTE: If a provider is in the list of providers to auto-loadbalance but is missing from the list when false is passed in, then it will continue to auto-loadbalance.
        /// </summary>
        /// <param name="addToLoadBalanceList"></param>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoLoadBalanceForListOfProviders/{addToLoadBalanceList}/{providerTypes}")]
        public ActionResult<bool> SetAutoLoadBalanceForListOfProviders(bool addToLoadBalanceList, string providerTypes)
        {
            string[] types = providerTypes.Split(',');
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (string type in types)
                providerTypesList.Add((ProviderType)Enum.Parse(typeof(ProviderType), type));

            return Ok(ProviderManager.SetAutoLoadBalanceForProviders(addToLoadBalanceList, new List<ProviderType>(providerTypesList)));
        }

        ///// <summary>
        ///// Enable/disable auto-failover for the provider. If this is set to true then the OASIS will automatically switch to the next provider in the list of providers passed in. The OASIS will continue to auto-fail over to the next provider in the list until this method is called again passing in false.
        ///// </summary>
        ///// <param name="addToFailOverList"></param>
        ///// <param name="providerType"></param>
        ///// <returns></returns>
        //[Authorize]
        //[HttpPost("SetAutoFailOverForProvider/{addToFailOverList}/{providerType}")]
        //public ActionResult<bool> SetAutoFailOverForProvider(bool addToFailOverList, ProviderType providerType)
        //{
        //    return Ok(ProviderManager.SetAutoFailOverForProviders(addToFailOverList, new List<ProviderType>() { providerType }));
        //}


        /// <summary>
        /// Override a provider's config such as connnectionstring, etc
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        [Authorize(AvatarType.Wizard)]
        [HttpPost("SetProviderConfig/{providerType}/{connectionString}")]
        public ActionResult<bool> SetProviderConfig(ProviderType providerType, string connectionString)
        {
            //TODO: Test this works and then implement for rest of providers...
            switch (providerType)
            {
                case ProviderType.MongoDBOASIS:
                    {
                        OASISDNAManager.OASISDNA.OASIS.StorageProviders.MongoDBOASIS.ConnectionString = connectionString;

                        ProviderManager.DeActivateProvider(ProviderType.MongoDBOASIS);
                        ProviderManager.UnRegisterProvider(ProviderType.MongoDBOASIS);
                    }
                    break;
            }

            return Ok(true);
        }
    }
}
