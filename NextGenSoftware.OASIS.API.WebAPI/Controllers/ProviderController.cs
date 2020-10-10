using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/provider")]
    public class ProviderController : OASISControllerBase
    {
        OASISSettings _settings;

        public ProviderController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        [Authorize]
        [HttpGet("GetAllRegisteredProviders")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredProviders()
        {
            return Ok(ProviderManager.GetAllProviders());
        }

        [Authorize]
        [HttpGet("GetAllRegisteredProvidersForCategory/{category}")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredProvidersForCategory(ProviderCategory category)
        {
            return Ok(ProviderManager.GetProvidersOfCategory(category));
        }

        [Authorize]
        [HttpGet("GetAllRegisteredStorageProviders")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredStorageProviders()
        {
            return Ok(ProviderManager.GetStorageProviders());
        }

        [Authorize]
        [HttpGet("GetAllRegisteredNetworkProviders")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredNetworkProviders()
        {
            return Ok(ProviderManager.GetNetworkProviders());
        }

        [Authorize]
        [HttpGet("GetAllRegisteredRendererProviders")]
        public ActionResult<IEnumerable<IOASISProvider>> GetAllRegisteredRendererProviders()
        {
            return Ok(ProviderManager.GetRendererProviders());
        }

        [Authorize]
        [HttpGet("RegisterProvider/{provider}")]
        public ActionResult<bool> RegisterProvider(IOASISProvider provider)
        {
            return Ok(ProviderManager.RegisterProvider(provider));
        }

        [Authorize]
        [HttpGet("RegisterProviders/{providers}")]
        public ActionResult<bool> RegisterProviders(List<IOASISProvider> providers)
        {
            return Ok(ProviderManager.RegisterProviders(providers));
        }

        [Authorize]
        [HttpGet("UnRegisterProvider/{provider}")]
        public ActionResult<bool> UnRegisterProvider(IOASISProvider provider)
        {
            return Ok(ProviderManager.UnRegisterProvider(provider));
        }

        [Authorize]
        [HttpGet("UnRegisterProvider/{providerType}")]
        public ActionResult<bool> UnRegisterProvider(ProviderType providerType)
        {
            return Ok(ProviderManager.UnRegisterProvider(providerType));
        }

        [Authorize]
        [HttpGet("UnRegisterProviders/{providerTypes}")]
        public ActionResult<bool> UnRegisterProviders(List<ProviderType> providerTypes)
        {
            return Ok(ProviderManager.UnRegisterProviders(providerTypes));
        }

        [Authorize]
        [HttpGet("UnRegisterProviders/{providers}")]
        public ActionResult<bool> UnRegisterProviders(List<IOASISProvider> providers)
        {
            return Ok(ProviderManager.UnRegisterProviders(providers));
        }

        [Authorize]
        [HttpGet("GetProvider/{providerType}")]
        public ActionResult<IOASISProvider> GetProvider(ProviderType providerType)
        {
            return Ok(ProviderManager.GetProvider(providerType));
        }

        [Authorize]
        [HttpGet("IsProviderRegistered/{providerType}")]
        public ActionResult<bool> IsProviderRegistered(ProviderType providerType)
        {
            return Ok(ProviderManager.IsProviderRegistered(providerType));
        }

        [Authorize]
        [HttpGet("SetAndActivateCurrentStorageProvider/{providerType}/{setGlobally}")]
        public ActionResult<IOASISStorage> SetAndActivateCurrentStorageProvider(ProviderType providerType, bool setGlobally)
        {
            return Ok(GetAndActivateProvider(providerType, setGlobally));
        }

        [Authorize]
        [HttpGet("ActivateProvider/{providerType}")]
        public ActionResult<bool> ActivateProvider(ProviderType providerType)
        {
            return Ok(ProviderManager.ActivateProvider(providerType));
        }

        [Authorize]
        [HttpGet("DeActivateProvider/{providerType}")]
        public ActionResult<bool> DeActivateProvider(ProviderType providerType)
        {
            return Ok(ProviderManager.DeActivateProvider(providerType));
        }

        [Authorize]
        [HttpGet("SetDefaultProviders/{providers}")]
        public ActionResult<bool> SetDefaultProviders(string[] providers)
        {
            ProviderManager.DefaultProviderTypes = providers;
            return Ok(true);
        }

        [Authorize]
        [HttpGet("SetProviderConfig/{providerType}/{connectionString}")]
        public ActionResult<bool> SetProviderConfig(ProviderType providerType, string connectionString)
        {
            switch (providerType)
            {
                case ProviderType.MongoDBOASIS:
                    {
                        _settings.StorageProviders.MongoDBOASIS.ConnectionString = connectionString;

                        ProviderManager.DeActivateProvider(ProviderType.MongoDBOASIS);
                        ProviderManager.UnRegisterProvider(ProviderType.MongoDBOASIS);
                    }
                    break;
            }

            return Ok(true);
        }
    }
}
