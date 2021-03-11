using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/data")]
    public class DataController : OASISControllerBase
    {
        OASISSettings _settings;

        public DataController(IOptions<OASISSettings> OASISSettings) : base(OASISSettings)
        {
            _settings = OASISSettings.Value;
        }

        /// <summary>
        /// Load's a holon data object for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("LoadHolon/{id}")]
        public ActionResult<IHolon> LoadHolon(Guid id)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            return Ok();
        }

        /// <summary>
        /// Load's a holon data object for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("LoadHolon/{id}/{providerType}/{setGlobally}")]
        public ActionResult<IHolon> LoadHolon(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }

        /// <summary>
        /// Save's a holon data object.
        /// </summary>
        /// <param name="holon"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SaveHolon/{holon}")]
        public ActionResult<IHolon> SaveHolon(IHolon holon)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            return Ok();
        }

        /// <summary>
        /// Save's a holon data object.
        /// </summary>
        /// <param name="holon"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SaveHolon/{holon}/{providerType}/{setGlobally}")]
        public ActionResult<IHolon> SaveHolon(IHolon holon, ProviderType providerType, bool setGlobally = false)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }

        /// <summary>
        /// Save's a holon data object (meta data) to the given off-chain provider and then links its hash to the on-chain provider.
        /// </summary>
        /// <param name="holon"></param>
        /// <param name="offChainProviderType"></param>
        /// <param name="onChainProviderType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SaveHolonOffChain/{holon}/{offChainProviderType}/{onChainProviderType}")]
        public ActionResult<IHolon> SaveHolon(IHolon holon, ProviderType offChainProviderType, ProviderType onChainProviderType)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            //GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }

        /// <summary>
        /// Delete a holon for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("DeleteHolon/{id}")]
        public ActionResult<IHolon> DeleteHolon(Guid id)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            return Ok();
        }

        /// <summary>
        /// Delete a holon for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("DeleteHolon/{id}/{providerType}/{setGlobally}")]
        public ActionResult<IHolon> DeleteHolon(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            GetAndActivateProvider(providerType, setGlobally);
            return Ok();
        }
    }
}
