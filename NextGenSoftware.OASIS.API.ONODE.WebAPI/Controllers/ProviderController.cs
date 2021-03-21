using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/provider")]
    public class ProviderController : OASISControllerBase
    {
        OASISDNA _settings;

        public ProviderController(IOptions<OASISDNA> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Get all registered providers.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("GetAllRegisteredProviders")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredProviders()
        {
            return Ok(ProviderManager.GetAllProviders());
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
        public ActionResult<bool> GetProvidersThatAreAutoReplicating()
        {
            //TODO: Finish implementing.
            return Ok(true);
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
        [HttpPost("UnRegisterProvider/{providerType}")]
        public ActionResult<bool> UnRegisterProvider(ProviderType providerType)
        {
            return Ok(ProviderManager.UnRegisterProvider(providerType));
        }

        /// <summary>
        /// Unregisters the list of providers passed in.
        /// </summary>
        /// <param name="providerTypes"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("UnRegisterProviders/{providerTypes}")]
        public ActionResult<bool> UnRegisterProviders(List<ProviderType> providerTypes)
        {
            return Ok(ProviderManager.UnRegisterProviders(providerTypes));
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
        public ActionResult<IOASISStorage> SetAndActivateCurrentStorageProvider(ProviderType providerType, bool setGlobally)
        {
            return Ok(GetAndActivateProvider(providerType, setGlobally));
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

        /// <summary>
        /// Set's the default providers to be used (in priority order).
        /// </summary>
        /// <param name="providers"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetDefaultProviders/{providers}")]
        public ActionResult<bool> SetDefaultProviders(string[] providers)
        {
            ProviderManager.DefaultProviderTypes = providers;
            return Ok(true);
        }

        /// <summary>
        /// Enable/disable fail over between the providers. If this is set to true then if the current provider fails it will switch to the next provider in the default list (if load balancing is switched on then it will automatically switch to the next fastest provider).
        /// </summary>
        /// <param name="failOver"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetFailOver/{failOver}")]
        public ActionResult<bool> SetFailOver(bool failOver)
        {
            //TODO: Finish implementing.
            return Ok(true);
        }

        /// <summary>
        /// Enable/disable load balancing between providers. If this is set to true then the OASIS will automatically switch to the fastest provider within the ONODE's neighbourhood. This effectively load balances the entire internet.
        /// </summary>
        /// <param name="loadBalance"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetLoadBalance/{loadBalance}")]
        public ActionResult<bool> SetLoadBalance(bool loadBalance)
        {
            //TODO: Finish implementing.
            return Ok(true);
        }

        /// <summary>
        /// Enable/disable auto-replication between providers. If this is set to true then the OASIS will automatically replicate all data including the user's avatar to all available providers.
        /// </summary>
        /// <param name="autoReplicate"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoReplicate/{autoReplicate}")]
        public ActionResult<bool> SetAutoReplicate(bool autoReplicate)
        {
            //TODO: Finish implementing.
            return Ok(true);
        }

        /// <summary>
        /// Enable/disable auto-replication between providers. If this is set to true then the OASIS will automatically replicate all data including the user's avatar to the list of providers passed in. The OASIS will continue to replicate to the given providers until this method is called again passing in false along with a list of providers to disable auto-replication. NOTE: If a provider is in the list of providers to auto-replicate but is missing from the list when false is passed in, then it will continue to auto-replicate.
        /// </summary>
        /// <param name="autoReplicate"></param>
        /// <param name="providers"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetAutoReplicate/{autoReplicate}/{providers}")]
        public ActionResult<bool> SetAutoReplicate(bool autoReplicate, string[] providers)
        {
            //TODO: Finish implementing.
            return Ok(true);
        }


        /// <summary>
        /// Override a provider's config such as connnectionstring, etc
        /// </summary>
        /// <param name="providerType"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SetProviderConfig/{providerType}/{connectionString}")]
        public ActionResult<bool> SetProviderConfig(ProviderType providerType, string connectionString)
        {
            //TODO: Test this works and then implement for rest of providers...
            switch (providerType)
            {
                case ProviderType.MongoDBOASIS:
                    {
                        _settings.OASIS.StorageProviders.MongoDBOASIS.ConnectionString = connectionString;

                        ProviderManager.DeActivateProvider(ProviderType.MongoDBOASIS);
                        ProviderManager.UnRegisterProvider(ProviderType.MongoDBOASIS);
                    }
                    break;
            }

            return Ok(true);
        }
    }
}
