using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/data")]
    public class DataController : OASISControllerBase
    {
      //  OASISDNA _settings;
        HolonManager _holonManager = null;

        HolonManager HolonManager
        {
            get
            {
                if (_holonManager == null)
                {
                    OASISResult<IOASISStorage> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

                    //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

                    _holonManager = new HolonManager(result.Result);
                }

                return _holonManager;
            }
        }

        //public DataController(IOptions<OASISDNA> OASISSettings) 
        public DataController()
        {
            //_settings = OASISSettings.Value;
        }

        /// <summary>
        /// Load's a holon data object for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("LoadHolon/{id}")]
        public ActionResult<Holon> LoadHolon(Guid id)
        {
            OASISResult<IHolon> result = HolonManager.LoadHolon(id);

            if (result != null && !result.IsError && result.Result != null)
                return Ok(result.Result);
            else
                return Ok(string.Concat("ERROR: An error occured loading the holon. Reason: ", result.Message));
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
        public ActionResult<Holon> LoadHolon(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            GetAndActivateProvider(providerType, setGlobally);
            return LoadHolon(id);
        }

        /// <summary>
        /// Load's all holons
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("LoadAllHolons")]
        public ActionResult<Holon[]> LoadAllHolons()
        {
            OASISResult<IEnumerable<IHolon>> result = HolonManager.LoadAllHolons();

            if (result != null && !result.IsError && result.Result != null)
                return Ok(result.Result);
            else
                return Ok(string.Concat("ERROR: An error occured loading holons. Reason: ", result.Message));

            /*
            List<IHolon> data = (List<IHolon>)HolonManager.LoadAllHolons();
            List<Holon> holons = new List<Holon>();

            if (data == null)
                return Ok("ERROR: No Holons Found.");

            foreach (IHolon holon in data)
                holons.Add((Holon)holon);

            return Ok(holons);*/
        }

        /// <summary>
        /// Load's all holons
        /// </summary>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("LoadAllHolons/{providerType}/{setGlobally}")]
        public ActionResult<Holon[]> LoadAllHolons(ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return LoadAllHolons();
        }

        /// <summary>
        /// Load's all holons for parent
        /// </summary>
        /// <param name="id"></param>
        /// <param name="holonType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("LoadAllHolonsForParent/{id}/{holonType}")]
        public ActionResult<Holon[]> LoadAllHolonsForParent(Guid id, HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = HolonManager.LoadHolonsForParent(id, holonType);

            if (result != null && !result.IsError && result.Result != null)
                return Ok(result.Result);
            else
                return Ok(string.Concat("ERROR: An error occured loading holons. Reason: ", result.Message));

            /*
            List<IHolon> data = (List<IHolon>)HolonManager.LoadHolonsForParent(id, holonType);
            List<Holon> holons = new List<Holon>();

            if (data == null)
                return Ok("ERROR: No Holons Found.");

            foreach (IHolon holon in data)
                holons.Add((Holon)holon);

            return Ok(holons);
            */
        }

        /// <summary>
        /// Load's all holons for parent
        /// </summary>
        /// <param name="id"></param>
        /// <param name="holonType"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("LoadAllHolonsForParent/{id}/{holonType}/{providerType}/{setGlobally}")]
        public ActionResult<Holon[]> LoadAllHolonsForParent(Guid id, HolonType holonType = HolonType.All, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return LoadAllHolonsForParent(id, holonType);
        }

        /// <summary>
        /// Load's all holons for parent
        /// </summary>
        /// <param name="providerKey"></param>
        /// <param name="holonType"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("LoadAllHolonsForParent/{providerKey}/{holonType}")]
        public ActionResult<Holon[]> LoadAllHolonsForParent(string providerKey, HolonType holonType = HolonType.All)
        {
            OASISResult<IEnumerable<IHolon>> result = HolonManager.LoadHolonsForParent(providerKey, holonType);

            if (result != null && !result.IsError && result.Result != null)
                return Ok(result.Result);
            else
                return Ok(string.Concat("ERROR: An error occured loading holons. Reason: ", result.Message));

            /*
            List<IHolon> data = (List<IHolon>)HolonManager.LoadHolonsForParent(providerKey, holonType);
            List<Holon> holons = new List<Holon>();

            if (data == null)
                return Ok("ERROR: No Holons Found.");

            foreach (IHolon holon in data)
                holons.Add((Holon)holon);

            return Ok(holons);*/
        }

        /// <summary>
        /// Load's all holons for parent
        /// </summary>
        /// <param name="providerKey"></param>
        /// <param name="holonType"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("LoadAllHolonsForParent/{providerKey}/{holonType}/{providerType}/{setGlobally}")]
        public ActionResult<Holon[]> LoadAllHolonsForParent(string providerKey, HolonType holonType = HolonType.All, ProviderType providerType = ProviderType.Default, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return LoadAllHolonsForParent(providerKey, holonType);
        }

        /// <summary>
        /// Save's a holon data object.
        /// </summary>
        /// <param name="holon"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SaveHolon")]
        public ActionResult<Holon> SaveHolon(Holon holon)
        {
            OASISResult<IHolon> result = HolonManager.SaveHolon(holon);

            if (result != null && !result.IsError && result.Result != null)
                return Ok(result.Result);
            else
                return Ok(string.Concat("ERROR: An error occured saving the holon. Reason: ", result.Message));
        }

        /// <summary>
        /// Save's a holon data object.
        /// </summary>
        /// <param name="holon"></param>
        /// <param name="providerType">Pass in the provider you wish to use.</param>
        /// <param name="setGlobally"> Set this to false for this provider to be used only for this request or true for it to be used for all future requests too.</param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("SaveHolon/{providerType}/{setGlobally}")]
        public ActionResult<Holon> SaveHolon(Holon holon, ProviderType providerType, bool setGlobally = false)
        {
            GetAndActivateProvider(providerType, setGlobally);
            return SaveHolon(holon);
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
        public ActionResult<Holon> SaveHolon(Holon holon, ProviderType offChainProviderType, ProviderType onChainProviderType)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            //GetAndActivateProvider(providerType, setGlobally);
            return Ok("COMING SOON...");
        }

        /// <summary>
        /// Delete a holon for the given id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("DeleteHolon/{id}")]
        public ActionResult<string> DeleteHolon(Guid id)
        {
            OASISResult<bool> result = HolonManager.DeleteHolon(id);

            if (result != null && !result.IsError && result.Result)
                return Ok("Holon Deleted Successfully");
            
            else if (result != null)
                return Ok(string.Concat("ERROR: An error occured deleting the holon. Reason: ", result.Message));

            else
                return Ok(string.Concat("ERROR: An unknown error occured deleting the holon."));
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
        public ActionResult<string> DeleteHolon(Guid id, ProviderType providerType, bool setGlobally = false)
        {
            // TODO: Finish implementing (will tie into the HDK/ODK/Star project)
            GetAndActivateProvider(providerType, setGlobally);
            return DeleteHolon(id);
        }
    }
}
